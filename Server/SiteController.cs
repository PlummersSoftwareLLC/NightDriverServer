using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace NightDriver
{
    // ScheduledEffect
    //
    // An LED effect with scheduling

    public class ScheduledEffect
    {
        public const DayOfWeek WeekEnds = DayOfWeek.Saturday | DayOfWeek.Sunday;
        public const DayOfWeek WeekDays = DayOfWeek.Monday | DayOfWeek.Tuesday | DayOfWeek.Wednesday | DayOfWeek.Thursday | DayOfWeek.Friday;
        public const DayOfWeek AllDays = WeekDays | WeekEnds;

        protected LEDEffect _LEDEffect;
        protected DayOfWeek _DaysOfWeeks;
        protected uint _StartHour;
        protected uint _EndHour;
        protected uint _StartMinute;
        protected uint _EndMinute;

        public ScheduledEffect(DayOfWeek daysOfWeek, uint startHour, uint endHour, LEDEffect ledEffect, uint startMinute = 0, uint endMinute = 60)
        {
            _DaysOfWeeks = daysOfWeek;
            _LEDEffect = ledEffect;
            _StartHour = startHour;
            _EndHour = endHour;
        }

        public bool ShouldEffectRunNow
        {
            get
            {
                if (_DaysOfWeeks.HasFlag(DateTime.Now.DayOfWeek))
                    if (DateTime.Now.Hour > _StartHour || DateTime.Now.Hour == _StartHour && DateTime.Now.Minute >= _StartMinute)
                        if (DateTime.Now.Hour < _EndHour || DateTime.Now.Hour == _EndHour && DateTime.Now.Minute <= _EndMinute)
                            return true;

                return false;
            }
        }

        public uint MinutesRunning
        {
            get
            {
                uint c = 0;
                if (DateTime.Now.Hour > _StartHour)
                    c += ((uint)DateTime.Now.Hour - _StartHour) * 60;
                if (DateTime.Now.Minute >= _StartMinute)
                    c += ((uint)DateTime.Now.Minute - _StartMinute);
                return c;
            }
        }

        public LEDEffect LEDEffect
        {
            get
            {
                return _LEDEffect;
            }
        }
    }

    // Location
    //
    // A "location" is a set of one or more LED strip controllers and the effects that will run on them.  It
    // implements the "GraphicsBase" interface so that the effects can draw upon the "site" as a whole,
    // and it is later divied up to the various controllers.  So if you have 4000 LEDs, you might have
    // four strips with 1000 LEDs each, for example.  Combined with a list of effects, they consitute a site.

    public class Location : GraphicsBase
    {
        protected CancellationToken _token;
        protected DateTime StartTime;
        public    System.Threading.Thread _Thread;
        public virtual CRGB[] LEDs { get; }
        public virtual LightStrip[] LightStrips { get; }
        public virtual ScheduledEffect[] LEDEffects { get; }
        
        public const int PIXELS_PER_METER144 = 144;

        public Location()
        {
            StartTime = DateTime.Now;
        }

        public int FramesPerSecond
        {
            get; set;
        } = 22;

        protected int SecondsPerEffect
        {
            get
            {
                return 300;
            }
        }

        public uint SpareTime
        {
            get;
            set;
        } = 1000;

        public static uint MinimumSpareTime => (uint)ConsoleApp.g_AllSites.Min(location => location.SpareTime);

        // If we were certain that every pixel would get touched, and hence created, we wouldn't need to init them, but to
        // be safe, we init them all to a default pixel value (like magenta)

        protected static T[] InitializePixels<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }

        void WorkerDrawAndSendLoop()
        {
            DateTime lastSpareTimeReset = DateTime.UtcNow;
            DateTime timeLastFrame = DateTime.UtcNow -TimeSpan.FromSeconds((FramesPerSecond == 0 ? 0 : 1.0 / FramesPerSecond));

            while (!_token.IsCancellationRequested)
            {
                DateTime timeNext = timeLastFrame + TimeSpan.FromSeconds(1.0 / (FramesPerSecond > 0 ? FramesPerSecond : 30));
                DrawAndEnqueueAll(timeNext);
                timeLastFrame = timeNext;

                TimeSpan delay = timeNext - DateTime.UtcNow;
                if (delay.TotalMilliseconds > 0)
                {
                    Thread.Sleep((int) delay.TotalMilliseconds);
                }
                else
                {
                    ConsoleApp.Stats.WriteLine(this.GetType().Name + " dropped Frame by " + delay.TotalMilliseconds);
                    Thread.Sleep(1);
                }

                double spare = delay.TotalMilliseconds <= 0 ? 0 : delay.TotalMilliseconds;
                SpareTime = Math.Min(SpareTime, (uint) spare);

                ConsoleApp.Stats.SpareMilisecondsPerFrame = (uint)delay.TotalMilliseconds;

                if ((DateTime.UtcNow - lastSpareTimeReset).TotalSeconds > 1)
                {
                    SpareTime = 1000;
                    lastSpareTimeReset = DateTime.UtcNow;
                }
            }
            Debug.WriteLine("Leaving WorkerDrawAndSendLooop");
        }

        public void StartWorkerThread(CancellationToken token)
        {
            foreach (var strip in LightStrips)
                strip.Location = this; 

            _token = token;
            _Thread = new Thread(WorkerDrawAndSendLoop);
            _Thread.IsBackground = true;
            _Thread.Priority = ThreadPriority.BelowNormal;
            _Thread.Start();
        }


        public string CurrentEffectName
        {
            get;
            private set;
        } = "[None]";

        public void DrawAndEnqueueAll(DateTime timestamp)
        {
            DateTime timeStart2 = DateTime.UtcNow;

            var enabledEffects = LEDEffects.Where(effect => effect.ShouldEffectRunNow == true);
            var effectCount = enabledEffects.Count();
            if (effectCount > 0)
            {
                int iEffect = (int)((DateTime.Now - StartTime).TotalSeconds / SecondsPerEffect);
                iEffect %= effectCount;
                var effect = enabledEffects.ElementAt(iEffect);

                // We lock the CRGB buffer so that when the UI follows the same approach, we don't
                // get half-rendered frames in the LEDVisualizer

                lock (LEDs)
                    effect.LEDEffect.DrawFrame(this);
                CurrentEffectName = effect.LEDEffect.GetType().Name;
                if ((DateTime.UtcNow - timeStart2).TotalSeconds > 0.25)
                    ConsoleApp.Stats.WriteLine("MAIN3 DELAY");
            }

            if ((DateTime.UtcNow - timeStart2).TotalSeconds > 0.25)
                ConsoleApp.Stats.WriteLine("MAIN2 DELAY");

            foreach (var controller in LightStrips)
                if (controller.ReadyForData)
                    controller.CompressAndEnqueueData(LEDs, timestamp);
                else
                    controller.Response.Reset();
        }

        public override uint Width
        {
            get { return (uint)LEDs.Length; }
        }

        public override uint Height
        {
            get { return 1; }
        }

        public override uint LEDCount
        {
            get { return Width * Height; }
        }

        protected uint GetPixelIndex(uint x, uint y)
        {
            return (y * Width) + x;
        }

        protected void SetPixel(uint x, uint y, CRGB color)
        {

            LEDs[GetPixelIndex(x, Height - 1 - y)] = color;
        }

        protected void SetPixel(uint x, CRGB color)
        {
            LEDs[x] = color;
        }

        protected CRGB GetPixel(uint x)
        {
            if (x < 0 || x >= Width)
                return CRGB.Black;

            return LEDs[GetPixelIndex(x, 0)];
        }

        public override CRGB GetPixel(uint x, uint y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return CRGB.Black;

            return LEDs[GetPixelIndex(x, y)];
        }

        public override void DrawPixels(double fPos, double count, CRGB color)
        {
            double availFirstPixel = 1 - (fPos - (uint)(fPos));
            double amtFirstPixel = Math.Min(availFirstPixel, count);
            count = Math.Min(count, DotCount-fPos);
            if (fPos >= 0 && fPos < DotCount)
                BlendPixel((uint)fPos, color.fadeToBlackBy(1.0 - amtFirstPixel));

            fPos += amtFirstPixel;
            //fPos %= DotCount;
            count -= amtFirstPixel;

            while (count >= 1.0)
            {
                if (fPos >= 0 && fPos < DotCount)
                {
                    BlendPixel((uint)fPos, color);
                    count -= 1.0;
                }
                fPos += 1.0;
            }

            if (count > 0.0)
            {
                if (fPos >= 0 && fPos < DotCount)
                    BlendPixel((uint)fPos, color.fadeToBlackBy(1.0 - count));
            }
        }

        public override void DrawPixel(uint x, CRGB color)
        {
            SetPixel(x, color);
        }

        public override void DrawPixel(uint x, uint y, CRGB color)
        {
            SetPixel(x, y, color);
        }

        public override void BlendPixel(uint x, CRGB color)
        {
            CRGB c1 = GetPixel(x);
            SetPixel(x, c1 + color);
        }
    };

    // EffectsDatabase
    //
    // A static database of some predefined effects

    public static class EffectsDatabase
    {
        //        public static LEDEffect FireWindow => new BlueFire(5*144, true)
        //        {
        //           _Cooling = 100
        //        };

        public static LEDEffect FireWindow => new FireEffect(100, true, 2, 1, null) 
        {
           _Cooling = 3500,
           _Reversed = true,
           _Sparks = 1,
           _SparkHeight = 3
        };

        public static LEDEffect MarqueeEffect => new PaletteEffect(Palette.Rainbow)
        {
            _Density = Palette.SmoothRainbow.OriginalSize / 144.0 / 2.5 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 15,
            _DotSize = 3,
            _LEDColorPerSecond = 0,
            _LEDScrollSpeed = 10,
            _Brightness = 1.0,
            _Mirrored = true
        };
        public static LEDEffect MarqueeEffect2 => new PaletteEffect(Palette.Rainbow)
        {
            _Density = .5 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 3,
            _DotSize = 1,
            _LEDColorPerSecond = 0,
            _LEDScrollSpeed = 0.5,
            _Brightness = 1.0,
            _Mirrored = true
        };
        public static LEDEffect ColorCycleTube => new PaletteEffect(Palette.Rainbow)
        {
            _Density = 0,
            _EveryNthDot = 1,
            _DotSize = 1,
            _LEDColorPerSecond = 3,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect WhitePointLights => new SimpleColorFillEffect(new CRGB(246, 200, 160));

        public static LEDEffect QuietBlueStars => new StarEffect<ColorStar>
        {
            Blend = true,
            NewStarProbability = 1.0,
            StarPreignitonTime = 2.5,
            StarIgnition = 0.0,
            StarHoldTime = 2.0,
            StarFadeTime = 1.0,
            StarSize = 1,
            MaxSpeed = 0,
            BaseColorSpeed = 0.1,
            ColorSpeed = 0.0,
            RandomStartColor = false,
            RandomStarColorSpeed = false,
        };

        public static LEDEffect QuietColorStars => new StarEffect<PaletteStar>
        {
            Blend = true,
            NewStarProbability = 1.00,
            StarPreignitonTime = 4.0,
            StarIgnition = 0.0,
            StarHoldTime = 1.0,
            StarFadeTime = 3.0,
            StarSize = 1,
            MaxSpeed = 0,
            ColorSpeed = 0,
            Palette = new Palette(CRGB.ChristmasLights),
            RampedColor = false

        };

        public static LEDEffect ClassicTwinkle => new StarEffect<PaletteStar>
        {
            Blend = false,
            NewStarProbability = 1.0,
            StarPreignitonTime = 0,
            StarIgnition = 0.0,
            StarHoldTime = 1.75,
            StarFadeTime = 0.0,
            StarSize = 1,
            MaxSpeed = 0,
            ColorSpeed = 0,
            Palette = new Palette(CRGB.ChristmasLights),
            RampedColor = false,
            RandomStartColor = true
        };

        public static LEDEffect FrostyBlueStars => new StarEffect<PaletteStar>
        {
            Blend = true,
            NewStarProbability = .5,
            StarPreignitonTime = 0.05,
            StarIgnition = 0.1,
            StarHoldTime = 3.0,
            StarFadeTime = 1.0,
            StarSize = 1,
            MaxSpeed = 0,
            ColorSpeed = 1,
            Palette = new Palette(CRGB.makeGradient(new CRGB(0, 0, 64), new CRGB(0, 64, 255)))
        };

        public static LEDEffect TwinkleBlueStars => new StarEffect<PaletteStar>
        {
            Blend = true,
            NewStarProbability = 5,
            StarPreignitonTime = 0.1,
            StarIgnition = 0.05,
            StarHoldTime = 2.0,
            StarFadeTime = 1.0,
            StarSize = 1,
            MaxSpeed = 5,
            ColorSpeed = 0,
            Palette = new Palette(CRGB.BlueStars)
        };

        public static LEDEffect SparseChristmasLights => new StarEffect<PaletteStar>
        {
            Blend = false,
            NewStarProbability = 0.20,
            StarPreignitonTime = 0.00,
            StarIgnition = 0.0,
            StarHoldTime = 5.0,
            StarFadeTime = 0.0,
            StarSize = 1,
            MaxSpeed = 2,
            ColorSpeed = .05,
            Palette = new Palette(CRGB.ChristmasLights)
        };

        public static LEDEffect SparseChristmasLights2 => new StarEffect<AlignedPaletteStar>
        {
            Blend = false,
            NewStarProbability = 0.20,
            StarPreignitonTime = 0.00,
            StarIgnition = 0.0,
            StarHoldTime = 3.0,
            StarFadeTime = 0.0,
            StarSize = 6,
            MaxSpeed = 0,
            ColorSpeed = 0,
            Alignment = 24,
            RampedColor = true,
            Palette = new Palette(CRGB.ChristmasLights)
        };

        public static LEDEffect TwinkleChristmasLights => new StarEffect<AlignedPaletteStar>
        {
            Blend = false,
            NewStarProbability = 2.0,
            StarPreignitonTime = 0.00,
            StarIgnition = 0.0,
            StarHoldTime = 0.5,
            StarFadeTime = 0.0,
            StarSize = 4,
            MaxSpeed = 0,
            ColorSpeed = 0,
            Alignment = 24,
            RampedColor = true,
            Palette = new Palette(CRGB.ChristmasLights)
        };
        public static LEDEffect ChristmasLights => new PaletteEffect(new Palette(CRGB.ChristmasLights) { Blend = false } )
        {
            _Density = 3,
            _EveryNthDot = 18,
            _DotSize = 2,
            _LEDColorPerSecond = 0,
            _LEDScrollSpeed = 30,
            _RampedColor = true,
        };
        public static LEDEffect VintageChristmasLights => new PaletteEffect(new Palette(CRGB.VintageChristmasLights))
        {
            _Density = 1 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 5,
            _DotSize = 2,
            _LEDColorPerSecond = 0,
            _LEDScrollSpeed = 20,
            _RampedColor = true
        };

        public static LEDEffect FastChristmasLights => new PaletteEffect(new Palette(CRGB.ChristmasLights))
        {
            _Density = 1 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 28,
            _DotSize = 10,
            _LEDColorPerSecond = 0,
            _LEDScrollSpeed = 80,
            _RampedColor = true
        };

        public static LEDEffect ChristmasLightsFast => new PaletteEffect(new Palette(CRGB.ChristmasLights))
        {
            _Density = 16 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 32,
            _DotSize = 1,
            _LEDColorPerSecond = 0,
            _LEDScrollSpeed = 1,
            _RampedColor = false
        };


        public static LEDEffect LavaStars => new StarEffect<PaletteStar>
        {
            Blend = true,
            NewStarProbability = 5,
            StarPreignitonTime = 0.05,
            StarIgnition = 1.0,
            StarHoldTime = 1.0,
            StarFadeTime = 1.0,
            StarSize = 10,
            MaxSpeed = 50,
            Palette = new Palette(CRGB.HotStars)
        };

        public static LEDEffect RainbowMiniLites => new PaletteEffect(Palette.SmoothRainbow ) 
        {
            _Density = 1,
            _EveryNthDot = 10,
            _DotSize = 1,
            _LEDColorPerSecond = 20,
            _LEDScrollSpeed = 0
        };

        public static LEDEffect RainbowStrip => new PaletteEffect(Palette.SmoothRainbow)
        {
            _Density = .3,
            _EveryNthDot = 1,
            _DotSize = 1,
            _LEDColorPerSecond = 10,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect MiniLites => new PaletteEffect(new Palette(new CRGB[]
        {
            CRGB.Blue,
            CRGB.Cyan,
            CRGB.Green,
            CRGB.Blue,
            CRGB.Purple,
            CRGB.Pink,
            CRGB.Blue
        }))
        {
            _Density = .1 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 14,
            _DotSize = 1,
            _LEDColorPerSecond = 3,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect RainbowColorLites => new PaletteEffect(Palette.Rainbow)
        {
            _Density = 0.15  * Location.PIXELS_PER_METER144,
            _EveryNthDot = 8,
            _DotSize = 3,
            _LEDColorPerSecond = 5,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect CupboardRainbowSweep => new PaletteEffect(Palette.Rainbow)
        {
            _Density = .5 / 16  * Location.PIXELS_PER_METER144,
            _EveryNthDot = 10,
            _DotSize = 10,
            _LEDColorPerSecond = 1,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect ColorFadeMiniLites => new PaletteEffect(Palette.Rainbow)
        {
            _Density = 0,
            _EveryNthDot = 14,
            _DotSize = 1,
            _LEDColorPerSecond = 1,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect RidersEffect => new PaletteEffect(new Palette(CRGB.Football_Regina))
        {
            _Density = 1 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 2,
            _DotSize = 1,
            _LEDColorPerSecond = 2,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect RidersEffect2 => new PaletteEffect(new Palette(CRGB.Football_Regina2))
        {
            _Density = 1 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 2,
            _DotSize = 1,
            _LEDColorPerSecond = 2,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect Football_Effect_Seattle => new PaletteEffect(new Palette(CRGB.Football_Seattle))
        {
            _Density = 16 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 1,
            _DotSize = 1,
            _LEDColorPerSecond = 5,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect Football_Effect_SeattleA => new PaletteEffect(new Palette(CRGB.Football_Seattle))
        {
            _Density = 1 / CRGB.Football_Seattle.Length *  Location.PIXELS_PER_METER144,
            _EveryNthDot = 1,
            _DotSize = 1,
            _LEDColorPerSecond = 2,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect Football_Effect_Seattle2 => new PaletteEffect(new Palette(CRGB.Football_Seattle))
        {
            _Density = 8 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 10,
            _DotSize = 5,
            _LEDColorPerSecond = 0,
            _LEDScrollSpeed = 20,
        };

        public static LEDEffect C9 => new PaletteEffect(new Palette(CRGB.VintageChristmasLights))
        {
            _Density = 1 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 16,
            _DotSize = 1,
            _LEDColorPerSecond = 0,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect SeawawksTwinkleStarEffect => new StarEffect<PaletteStar>
        {
            Palette = new Palette(CRGB.Football_Seattle),
            Blend = true,
            NewStarProbability = 3,
            StarPreignitonTime = 0.05,
            StarIgnition = .5,
            StarHoldTime = 1.0,
            StarFadeTime = .5,
            StarSize = 2,
            MaxSpeed = 0,
            ColorSpeed = 0,
            RandomStartColor = false,
            BaseColorSpeed = 0.25,
            RandomStarColorSpeed = false
        };

        public static LEDEffect RainbowTwinkleStarEffect => new StarEffect<PaletteStar>
        {
            Palette = Palette.Rainbow,
            Blend = true,
            NewStarProbability = 3,
            StarPreignitonTime = 0.05,
            StarIgnition = .5,
            StarHoldTime = .0,
            StarFadeTime = 1.5,
            StarSize = 1,
            MaxSpeed = 0,
            ColorSpeed = 10,
            RandomStartColor = false,
            BaseColorSpeed = 0.005,
            RandomStarColorSpeed = false
        };

        public static LEDEffect ChristmasTwinkleStarEffect
        {
            get
            {
                return new StarEffect<PaletteStar>
                {
                    Palette = new Palette(CRGB.ChristmasLights),
                    Blend = true,
                    NewStarProbability = 20,
                    StarPreignitonTime = 0.05,
                    StarIgnition = .05,
                    StarHoldTime = 0.5,
                    StarFadeTime = .25,
                    StarSize = 1,
                    MaxSpeed = 2,
                    ColorSpeed = 0,
                    RandomStartColor = true,
                    BaseColorSpeed = 0.0,
                    RandomStarColorSpeed = false
                };
            }
        }

        public static LEDEffect OneDirectionStars
        {
            get
            {
                return new StarEffect<PaletteStar>
                {
                    Palette = new Palette(CRGB.Rainbow),
                    Blend = false,
                    NewStarProbability = 2,
                    StarPreignitonTime = 0.0,
                    StarIgnition = 0,
                    StarHoldTime = 2.0,
                    StarFadeTime = 1.0,
                    StarSize = 1,
                    MaxSpeed = 50,
                    ColorSpeed = 0,
                    Direction = 1,
                    RandomStartColor = true,
                    BaseColorSpeed = 0.0,
                    RandomStarColorSpeed = false
                };
            }
        }

        public static LEDEffect BasicColorTwinkleStarEffect
        {
            get
            {
                return new StarEffect<ColorStar>
                {
                    Blend = true,
                    NewStarProbability = 1,
                    StarPreignitonTime = 0.05,
                    StarIgnition = .5,
                    StarHoldTime = 5.0,
                    StarFadeTime = .5,
                    StarSize = 1,
                    MaxSpeed = 2,
                    ColorSpeed = -2,
                    RandomStartColor = false,
                    BaseColorSpeed = 0.5,
                    RandomStarColorSpeed = false
                };
            }
        }

        public static LEDEffect SubtleColorTwinkleStarEffect
        {
            get
            {
                return new StarEffect<ColorStar>
                {
                    Blend = true,
                    NewStarProbability = 3,
                    StarPreignitonTime = 0.5,
                    StarIgnition = 0,
                    StarHoldTime = 1.0,
                    StarFadeTime = .5,
                    StarSize = 1,
                    MaxSpeed = 2,
                    ColorSpeed = -2,
                    RandomStartColor = false,
                    BaseColorSpeed = 0.2,
                    RandomStarColorSpeed = false
                };
            }
        }

        public static LEDEffect ToyFireTruck
        {
            get
            {
                return new StarEffect<PaletteStar>
                {
                    Palette = new Palette(new CRGB[] { CRGB.Red, CRGB.Red }),
                    Blend = true,
                    NewStarProbability = 25,
                    StarPreignitonTime = 0.05,
                    StarIgnition = .5,
                    StarHoldTime = 0.0,
                    StarFadeTime = .5,
                    StarSize = 1,
                    MaxSpeed = 5,
                    ColorSpeed = -2,
                    RandomStartColor = false,
                    BaseColorSpeed = 50,
                    RandomStarColorSpeed = false,
                    Direction = -1
                };
            }
        }

        public static LEDEffect CharlieBrownTree
        {
            get
            {
                return new StarEffect<ColorStar>
                {
                    Blend = true,
                    NewStarProbability = 5, // 25,
                    StarPreignitonTime = 0.05,
                    StarIgnition = .5,
                    StarHoldTime = 0.0,
                    StarFadeTime = .5,
                    StarSize = 1,
                    MaxSpeed = 20,
                    ColorSpeed = -2,
                    RandomStartColor = false,
                    BaseColorSpeed = 5,
                    RandomStarColorSpeed = false,
                    Direction = 1
                };
            }
        }

        public static LEDEffect Mirror
        {
            get
            {
                return new StarEffect<ColorStar>
                {
                    Blend = true,
                    NewStarProbability = 25,
                    StarPreignitonTime = 0.05,
                    StarIgnition = .5,
                    StarHoldTime = 0.0,
                    StarFadeTime = .5,
                    StarSize = 1,
                    MaxSpeed = 50,
                    ColorSpeed = -2,
                    RandomStartColor = false,
                    BaseColorSpeed = 5,
                    RandomStarColorSpeed = false,
                    Direction = 1
                };
            }
        }

        public static LEDEffect RainbowMarquee => new PaletteEffect(new Palette(CRGB.Rainbow))
        {
            _Density = .05,
            _EveryNthDot = 12,
            _DotSize = 3,
            _LEDColorPerSecond = -10,
            _LEDScrollSpeed = 10,
            _Mirrored = true,

        };

        public static LEDEffect ColorTunnel => new PaletteEffect(new Palette(CRGB.Rainbow))
        {
            _Density = .36 * Location.PIXELS_PER_METER144,
            _EveryNthDot = 3,
            _DotSize = 1,
            _LEDColorPerSecond = 1,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect BigMiniLites => new PaletteEffect(new Palette(CRGB.Rainbow))
        {
            _Density = 4.0 * Location.PIXELS_PER_METER144,
            _LEDColorPerSecond = 1,
            _LEDScrollSpeed = 0,
        };

        public static LEDEffect Mirror3
        {
            get
            {
                return new StarEffect<ColorStar>
                {
                    Blend = true,
                    NewStarProbability = 15, // 25,
                    StarPreignitonTime = 0.05,
                    StarIgnition = .0,
                    StarHoldTime = 0.0,
                    StarFadeTime = .5,
                    StarSize = 1,
                    MaxSpeed = 0,
                    ColorSpeed = 0,
                    RandomStartColor = false,
                    BaseColorSpeed = 10,
                    RandomStarColorSpeed = false,
                };
            }
        }
    }

    // Cabana
    //
    // Location definitio for the lights on the eaves of the Cabana

    public class Cabana : Location
    {
        const bool compressData = true;
        const int CABANA_START = 0;
        const int CABANA_1 = CABANA_START;
        const int CABANA_1_LENGTH = (5 * 144 - 1) + (3 * 144);
        const int CABANA_2 = CABANA_START + CABANA_1_LENGTH;
        const int CABANA_2_LENGTH = 5 * 144 + 55;
        const int CABANA_3 = CABANA_START + CABANA_2_LENGTH + CABANA_1_LENGTH;
        const int CABANA_3_LENGTH = 6 * 144 + 62;
        const int CABANA_4 = CABANA_START + CABANA_3_LENGTH + CABANA_2_LENGTH + CABANA_1_LENGTH;
        const int CABANA_4_LENGTH = 8 * 144 - 23;
        const int CABANA_LENGTH = CABANA_1_LENGTH + CABANA_2_LENGTH + CABANA_3_LENGTH + CABANA_4_LENGTH;

        private CRGB[] _LEDs = InitializePixels<CRGB>(CABANA_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.36", "CBWEST1", compressData, CABANA_1_LENGTH, 1, CABANA_1, false)  { FramesPerBuffer = 500, BatchSize = 10 },      
            new LightStrip("192.168.8.5", "CBEAST1", compressData, CABANA_2_LENGTH, 1, CABANA_2, true)    { FramesPerBuffer = 500, BatchSize = 10 },      
            new LightStrip("192.168.8.37", "CBEAST2", compressData, CABANA_3_LENGTH, 1, CABANA_3, false)  { FramesPerBuffer = 500, BatchSize = 10 },      
            new LightStrip("192.168.8.31", "CBEAST3", compressData, CABANA_4_LENGTH, 1, CABANA_4, false)  { FramesPerBuffer = 500, BatchSize = 10 },      
        };

        public ScheduledEffect[] _GameDayLEDEffects =
        {
            // Intense Effects for Daytime Hours

            new ScheduledEffect(ScheduledEffect.AllDays,  9, 22,  EffectsDatabase.Football_Effect_Seattle),
        };
 

        public ScheduledEffect[] _LEDEffects =
        {
            // Uncomment to test a single effect

            new ScheduledEffect(ScheduledEffect.AllDays,  22, 23, new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.50f), 2)),
            new ScheduledEffect(ScheduledEffect.AllDays,  23, 24, new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.60f), 2)),
            new ScheduledEffect(ScheduledEffect.AllDays,  0,  1,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.70f), 4)),
            new ScheduledEffect(ScheduledEffect.AllDays,  1,  2,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.80f), 4)),
            new ScheduledEffect(ScheduledEffect.AllDays,  2,  3,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.90f), 4)),
            new ScheduledEffect(ScheduledEffect.AllDays,  3,  4,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.80f), 4)),
            new ScheduledEffect(ScheduledEffect.AllDays,  4,  5,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.70f), 3)),
            new ScheduledEffect(ScheduledEffect.AllDays,  5,  6,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.60f), 2)),
            new ScheduledEffect(ScheduledEffect.AllDays,  6,  7,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.50f), 2)),
            new ScheduledEffect(ScheduledEffect.AllDays,  7,  8,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.40f), 2)),


            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.SubtleColorTwinkleStarEffect ),

            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.OneDirectionStars ),
            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.ColorFadeMiniLites ),
            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.ColorCycleTube ),
            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.RainbowMiniLites ),            
            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.QuietColorStars),

            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.QuietColorStars),

            /* Very high data rate - could even be too much to decompress on the ESP32
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new PaletteEffect(Palette.SmoothRainbow)
            {   
                _Density = 0.25,
                _EveryNthDot = 1,
                _DotSize = 1,
                _LEDColorPerSecond = 50,
                _LEDScrollSpeed = 0,
            }),
            */


            // new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.ClassicTwinkle),

            /*
          new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, 
                new PaletteEffect(new Palette(CRGB.ChristmasLights)
                { Blend = false }
                )
            {   
                _Density = 1.25,
                _EveryNthDot = 24,
                _DotSize = 3,
                _LEDColorPerSecond = 0,
                _LEDScrollSpeed = 10,
            }), 
              */


            // Busy Stuff

            // new ScheduledEffect(ScheduledEffect.AllDays,  9, 22, EffectsDFatabase.ColorTunnel ),
            
            // Solid strip around Cabana that rotates color over time
            //
            // new ScheduledEffect(ScheduledEffect.AllDays,  9, 22, EffectsDatabase.RainbowStrip ),
            
            // Nice Christmas Light Palette Effect
            // new ScheduledEffect(ScheduledEffect.AllDays,  9, 22, EffectsDatabase.RainbowMiniLites ),
            
            // Bright solid tube around Cabana
            // new ScheduledEffect(ScheduledEffect.AllDays,  9, 22, EffectsDatabase.ColorCycleTube ),
            
            // Big non-moving 3 pixel lights that slowly cycle color
            // new ScheduledEffect(ScheduledEffect.AllDays,  9, 22, EffectsDatabase.BigMiniLites ),

            // Christmas new ScheduledEffect(ScheduledEffect.AllDays,  9, 21, EffectsDatabase.ChristmasTwinkleStarEffect),
                // Quiet Early AM

            /*
            new ScheduledEffect(ScheduledEffect.AllDays,  22, 23, new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.50f), 2)),
            new ScheduledEffect(ScheduledEffect.AllDays,  23, 24, new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.60f), 2)),
            new ScheduledEffect(ScheduledEffect.AllDays,  0,  1,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.70f), 4)),
            new ScheduledEffect(ScheduledEffect.AllDays,  1,  2,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.80f), 4)),
            new ScheduledEffect(ScheduledEffect.AllDays,  2,  3,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.90f), 4)),
            new ScheduledEffect(ScheduledEffect.AllDays,  3,  4,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.80f), 4)),
            new ScheduledEffect(ScheduledEffect.AllDays,  4,  5,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.70f), 3)),
            new ScheduledEffect(ScheduledEffect.AllDays,  5,  6,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.60f), 2)),
            new ScheduledEffect(ScheduledEffect.AllDays,  6,  7,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.50f), 2)),
            new ScheduledEffect(ScheduledEffect.AllDays,  7,  8,  new SimpleColorFillEffect(CRGB.RandomSaturatedColor.fadeToBlackBy(0.40f), 2)),
            
            // All Day 
            
            // Star Twinkle colors with minor appearance flash
            // new ScheduledEffect(ScheduledEffect.AllDays,  9, 22, EffectsDatabase.BasicColorTwinkleStarEffect),
            
            // Like sparse Christmas lights 
            // new ScheduledEffect(ScheduledEffect.AllDays,  0, 22, EffectsDatabase.SparseChristmasLights),
            
            // Big slow colored marquee dots
            // new ScheduledEffect(ScheduledEffect.AllDays,  0, 22, EffectsDatabase.MarqueeEffect ),
            
            // Serene Color Stars
            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.QuietBlueStars),

            // Small non-moving color lights similar to status effect
            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.ColorFadeMiniLites),
            
            // Slow grow/fade color stars
            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.QuietColorStars),
            
            // Meteor Effect
            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, new MeteorEffect(CABANA_LENGTH, false, CABANA_LENGTH / 144, 5, 0.88, 3, 3)),
            
            */

        };

        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects 
        { 
            get 
            {
                if (false)
                    return _GameDayLEDEffects;
                else
                    return _LEDEffects; 
            } 
        }
        public override CRGB[] LEDs { get { return _LEDs; } }
    };



    // Bench
    //
    // Location definition for the test rig on the workbench

    public class Bench : Location
    {
        const bool compressData = true;
        const int BENCH_START   = 0;
        const int BENCH_LENGTH = 8 * 144;

        private CRGB[] _LEDs = InitializePixels<CRGB>(BENCH_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.70", "BENCH", compressData, BENCH_LENGTH, 1, BENCH_START, true, 0, false) { FramesPerBuffer = 24, BatchSize = 1  }  // 216
            //new LightStrip("192.168.8.163", "BENCH", compressData, BENCH_LENGTH, 1, BENCH_START, true, 0, false) {  }  // 216
        }; 

        public ScheduledEffect[] _LEDEffects = 
        {
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.ClassicTwinkle),
        };

        public override LightStrip[] LightStrips        { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects    { get { return _LEDEffects; } }
        public override CRGB[] LEDs                  { get { return _LEDs; } }
    };

    // ChristmasPresents - Front Door Pillar Left
    //
    // Location definition for the test rig on the workbench

    public class CeilingStrip : Location
    {
        const int START   = 0;
        const int LENGTH = 5*144 + 38;

        private CRGB[] _LEDs = InitializePixels<CRGB>(LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.43", "Ceiling A", true, LENGTH, 1, START, true, 0, false) { FramesPerBuffer = 500, BatchSize = 10 },  // 216
            //new LightStrip("192.168.8.1", "Ceiling B", true, LENGTH, 1, START, true, 0, false) { FramesPerBuffer = 24, BatchSize = 1  }  // 216
        }; 

        public ScheduledEffect[] _LEDEffects = 
        {
//            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new SimpleColorFillEffect(CRGB.White, 1) ),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new FireEffect(LENGTH, true) { _Cooling = 80, _speed = 2 }),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.SubtleColorTwinkleStarEffect ),

            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.ClassicTwinkle ),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.MarqueeEffect ),
       };

        public override LightStrip[] LightStrips        { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects    { get { return _LEDEffects; } }
        public override CRGB[] LEDs                  { get { return _LEDs; } }
    };

    // ChristmasTruck - A little truck I dress up with lights at Christmas
    //
    // Location definition for the little fire truck

    public class ChristmasTruck : Location
    {
        const int START   = 0;
        const int LENGTH = 70;
        private CRGB[] _LEDs = InitializePixels<CRGB>(LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.38", "Presents", true, LENGTH, 1, START, true, 0, false) { FramesPerBuffer = 500, BatchSize = 10  }  // 216
        }; 

        public ScheduledEffect[] _LEDEffects = 
        {
        /*
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, 
                new StarEffect<PaletteStar>
                {
                    Palette = new Palette(CRGB.ChristmasLights),
                    Blend = false,
                    NewStarProbability = 3,
                    StarPreignitonTime = 0.05,
                    StarIgnition = .05,
                    StarHoldTime = 1.0,
                    StarFadeTime = .5,
                    StarSize = 1,
                    MaxSpeed = 2,
                    ColorSpeed = 0,
                    RandomStartColor = true,
                    BaseColorSpeed = 0.0,
                    RandomStarColorSpeed = false
                }   
            ),
        */
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, 
                new PaletteEffect(new Palette(CRGB.ChristmasLights)
                { Blend = false }
                )
            {   
                _Density = 10,
                _EveryNthDot = 6,
                _DotSize = 1,
                _LEDColorPerSecond = 0,
                _LEDScrollSpeed = 5,
            }), 
       };

        public override LightStrip[] LightStrips        { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects    { get { return _LEDEffects; } }
        public override CRGB[] LEDs                  { get { return _LEDs; } }
    };

    // s - Front Door Pillar Left
    //
    // Location definition for the test rig on the workbench

    public class Pillars : Location
    {
        const bool compressData = true;
        const int PILLAR_START   = 0;
        const int PILLAR_LENGTH = 2*300;

        private CRGB[] _LEDs = InitializePixels<CRGB>(PILLAR_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.160", "PillarS", compressData, PILLAR_LENGTH, 1, PILLAR_START, false, 0, false) { FramesPerBuffer = 500, BatchSize = 1  },  // 216
            new LightStrip("192.168.8.64", "PillarN", compressData, PILLAR_LENGTH, 1, PILLAR_START, false, 0, false) { FramesPerBuffer = 500, BatchSize = 1  }  // 216
        }; 

        public ScheduledEffect[] _LEDEffects = 
        {

            //// 

            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, 
                EffectsDatabase.ColorCycleTube ),
                
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, 
                new SimpleColorFillEffect(CRGB.Red, 1)),       

            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, 
                new SimpleColorFillEffect(CRGB.White, 3)),       

            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, 
                new SimpleColorFillEffect(CRGB.Green, 1)),       


//            new ScheduledEffect(ScheduledEffect.AllDays,  9, 21, EffectsDatabase.OneDirectionStars),

        };

        public override LightStrip[] LightStrips        { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects    { get { return _LEDEffects; } }
        public override CRGB[] LEDs                  { get { return _LEDs; } }
    };

    // NorthWall
    //
    // Location definition for the north wall accent lighting

    public class NorthWall : Location
    {
        const bool compressData = true;
        const int WALL_LENGTH = 2 * 144;

        private CRGB[] _LEDs = InitializePixels<CRGB>(WALL_LENGTH * 2);

        private LightStrip[] _StripControllers =
        {
            // There are two pieces to this, each comprising half of it

            new LightStrip("192.168.1.62", "WALLNW", compressData, WALL_LENGTH, 1, 0, false)           {  },
            new LightStrip("192.168.1.63", "WALLNE", compressData, WALL_LENGTH, 1, WALL_LENGTH, false) {  } 
        };

        public ScheduledEffect[] _LEDEffects =
        {
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new SimpleColorFillEffect(CRGB.Blue, 1) )
        };

        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    };

    // Demo
    //
    // Used for filming demos on video camera

    public class Demo : Location
    {
        const bool compressData = true;
        const int DEMO_START = 0;
        const int DEMO_LENGTH = 144;

        private CRGB[] _LEDs = InitializePixels<CRGB>(DEMO_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.75", "DEMO", compressData, DEMO_LENGTH, 1, DEMO_START, false) { FramesPerBuffer = 500, BatchSize = 10  }  // 216
        };

        public ScheduledEffect[] _LEDEffects =
        {
            new ScheduledEffect(ScheduledEffect.AllDays, 21, 22, new FireworksEffect() { NewParticleProbability = 2.0, MaxSpeed = 150 } ),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.OneDirectionStars ),
            new ScheduledEffect(ScheduledEffect.AllDays,  8, 22, EffectsDatabase.QuietColorStars),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.SubtleColorTwinkleStarEffect ),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.SubtleColorTwinkleStarEffect ),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.ColorFadeMiniLites ),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.ColorCycleTube ),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.RainbowMiniLites ),
     
           //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new SimpleColorFillEffect(CRGB.White, 1)), 
            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24,  EffectsDatabase.QuietBlueStars),
            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new FireEffect(DEMO_LENGTH, false) { _Cooling = 120 }  ),
            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new FireworksEffect() { NewParticleProbability = 6.0, MaxSpeed = 20 } ),
            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24,  EffectsDatabase.TwinkleBlueStars  ),
            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new FireEffect(DEMO_LENGTH, false) { _Cooling = 600 } ),

           new ScheduledEffect(ScheduledEffect.AllDays, 0, 24,

                new PaletteEffect(true ? Palette.Rainbow : new Palette( new CRGB[] {  new CRGB(0, 0x80, 0xFF), } ))
                {
                    _Density =.25 * PIXELS_PER_METER144, _LEDScrollSpeed = 0, _LEDColorPerSecond = 12, _DotSize = 1, _EveryNthDot = 1, _Brightness = 1
                }
            ),



        };

        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    };
    public class Tree : Location
    {
        const bool compressData = true;
        const int TREE_START = 0;
        const int TREE_LENGTH = 8*144;

        private CRGB[] _LEDs = InitializePixels<CRGB>(TREE_LENGTH);
        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.33", "TREE", compressData, TREE_LENGTH, 1, TREE_START, false, 0, false) { FramesPerBuffer = 500, BatchSize = 10  },
        };

        public ScheduledEffect[] _LEDEffects =
        {
//            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new SimpleColorFillEffect(CRGB.White, 1)),

//            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new FireEffect(TREE_LENGTH, true, 5) { _SparkHeight = 2, _Cooling = 2500, _SparkProbability = 0.25 } ),

//            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24,
//                new MeteorEffect(TREE_LENGTH, false, 1, 1, 0.75, 0.5, 0.5)),

            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new SimpleColorFillEffect(CRGB.White, 1)),

               new ScheduledEffect(ScheduledEffect.AllDays,  6, 24,
                    new PaletteEffect( new Palette(new CRGB [] { CRGB.Red, CRGB.Green, CRGB.Blue }))
                        { _LEDColorPerSecond = 20,
                        _LEDScrollSpeed = 3,
                        _EveryNthDot = 4,
                        _DotSize = 1,
                        _Mirrored = false,
                        _Brightness = 1.0,
                        _Density = 8}),
                
            new ScheduledEffect(ScheduledEffect.AllDays,  6, 24,
                    new PaletteEffect( new Palette(new CRGB [] { CRGB.Red, CRGB.Green, CRGB.Blue }))
                        { _LEDColorPerSecond = 20,
                        _LEDScrollSpeed = 0,
                        _EveryNthDot = TREE_LENGTH,
                        _DotSize = TREE_LENGTH,
                        _Mirrored = false,
                        _Brightness = 1.0,
                        _Density = 0}),


            new ScheduledEffect(ScheduledEffect.AllDays,  6, 24, new SimpleColorFillEffect(CRGB.Green, 1)),

            new ScheduledEffect(ScheduledEffect.AllDays,  6, 24,
                    new PaletteEffect( new Palette(new CRGB [] { CRGB.Green }))
                        { _LEDColorPerSecond = 20,
                        _LEDScrollSpeed = 3,
                        _EveryNthDot = 4,
                        _DotSize = 1,
                        _Mirrored = false,
                        _Brightness = 1.0,
                        _Density = 0}),

                new ScheduledEffect(ScheduledEffect.AllDays,  6, 24,
                    new PaletteEffect( new Palette(new CRGB [] { CRGB.Red, CRGB.Green, CRGB.Blue }))
                        { _LEDColorPerSecond = 20,
                        _LEDScrollSpeed = 3,
                        _EveryNthDot = 4,
                        _DotSize = 1,
                        _Mirrored = false,
                        _Brightness = 1.0,
                        _Density = 8}),

            // new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.CharlieBrownTree),
            
            /*
            new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new PaletteEffect(new Palette(CRGB.URegina))
            {
                _Density = .25 * PIXELS_PER_METER144,
                _EveryNthDot = 8,
                _DotSize = 4,
                _LEDColorPerSecond = 0,
                _LEDScrollSpeed = 15,
                _Brightness = .125
            })
            */
            
        };

        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    };

    public class Mirror : Location
    {
        const bool compressData = true;
        const int BENCH_START = 0;
        const int BENCH_LENGTH = 93;

        private CRGB[] _LEDs = InitializePixels<CRGB>(BENCH_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.1.53", "MIRROR", compressData, BENCH_LENGTH, 1, BENCH_START, false, 0, false),
        };

        public ScheduledEffect[] _LEDEffects =
        {
            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new FireEffect(93, true) { _Cooling = 2000, _Drift = 1, _SparkHeight = 4, _Sparks = 1 } ),
            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.Football_Effect_Seattle2),
            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.Mirror),
            //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.Mirror3),
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.ColorTunnel),
        };

        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    };
    /*
        public class Truck : Location
        {
            const bool compressData = true;
            const int TRUCK_START = 0;
            const int TRUCK_LENGTH = 270;

            private CRGB[] _LEDs = InitializePixels<CRGB>(TRUCK_LENGTH);

            private LightStrip[] _StripControllers =
            {
                new LightStrip("192.168.0.215", "TREE", compressData, TRUCK_LENGTH, 1, TRUCK_START, false),
            };

            public ScheduledEffect[] _LEDEffects =
            {
                new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, EffectsDatabase.C9)
            };

            public override LightStrip[] LightStrips { get { return _StripControllers; } }
            public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
            public override CRGB[] LEDs { get { return _LEDs; } }
        };
    */
    public class AtomLight : Location
    {
        const bool compressData = true;
        const int ATOM_START = 0;
        const int ATOM_LENGTH = 53;

        private CRGB[] _LEDs = InitializePixels<CRGB>(ATOM_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.0.148", "ATOM0", compressData, ATOM_LENGTH, 1, ATOM_START, false, 1 + 2 + 4 + 8),
            //new LightStrip("192.168.0.251", "ATOM1", compressData, ATOM_LENGTH, 1, ATOM_START, false, 2),
            //new LightStrip("192.168.0.251", "ATOM2", compressData, ATOM_LENGTH, 1, ATOM_START, false, 4),
            //new LightStrip("192.168.0.251", "ATOM3", compressData, ATOM_LENGTH, 1, ATOM_START, false, 8),
        };

        public ScheduledEffect[] _LEDEffects =
        {
            new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, EffectsDatabase.Football_Effect_Seattle),
        };

        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    };

    public class TV : Location
    {
        const bool compressData = true;
        const int TV_START = 0;
        const int TV_LENGTH = 144 * 5;

        private CRGB[] _LEDs = InitializePixels<CRGB>(TV_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.26", "TV",        compressData, TV_LENGTH,         1, TV_START, false) { FramesPerBuffer = 500, BatchSize = 10  }
        };

        public ScheduledEffect[] _LEDEffects =
        {
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new RainbowEffect(0, 20)),
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new RainbowEffect(1, 100)),
            new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new FireEffect(4 * 144, true) { _Cooling = 60 } )
            /*
            new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new PaletteEffect(new Palette( CRGB.Reds, CRGB.Reds.Length))
            {
                _Density = .1 * PIXELS_PER_METER144,
                _EveryNthDot = 10,
                _DotSize = 10,
                _LEDColorPerSecond = 0,
                _LEDScrollSpeed = 10,
                _Blend = true
            })
            */
           //new ScheduledEffect(ScheduledEffect.AllDays,  0, 24, new SimpleColorFillEffect(CRGB.Red, 2))
    };

        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    }

    // ShopCupboards
    //
    // Location definition for the up-lights on top of the shop cupboards

    public class ShopCupboards : Location
    {
        const bool compressData = true;

        const int CUPBOARD_START = 0;
        const int CUPBOARD_1_START = CUPBOARD_START;
        const int CUPBOARD_1_LENGTH = 300 + 200;
        const int CUPBOARD_2_START = CUPBOARD_1_START + CUPBOARD_1_LENGTH;
        const int CUPBOARD_2_LENGTH = 300 + 300;                                   // 90 cut from one 
        const int CUPBOARD_3_START = CUPBOARD_2_START + CUPBOARD_2_LENGTH;
        const int CUPBOARD_3_LENGTH = 144;
        const int CUPBOARD_4_START = CUPBOARD_2_START + CUPBOARD_2_LENGTH + CUPBOARD_3_LENGTH;
        const int CUPBOARD_4_LENGTH = 144;          // Actuall 82, but 
        const int CUPBOARD_LENGTH = CUPBOARD_1_LENGTH + CUPBOARD_2_LENGTH + CUPBOARD_3_LENGTH + CUPBOARD_4_LENGTH;

        private CRGB[] _LEDs = InitializePixels<CRGB>(CUPBOARD_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.12", "CUPBOARD1", compressData, CUPBOARD_1_LENGTH, 1, CUPBOARD_1_START, false) { FramesPerBuffer = 500, BatchSize = 10 },
            new LightStrip("192.168.8.29", "CUPBOARD2", compressData, CUPBOARD_2_LENGTH, 1, CUPBOARD_2_START, false) { FramesPerBuffer = 500, BatchSize = 10 },
            new LightStrip("192.168.8.30", "CUPBOARD3", compressData, CUPBOARD_3_LENGTH, 1, CUPBOARD_3_START, false) { FramesPerBuffer = 500, BatchSize = 10 },  // WHOOPS
            new LightStrip("192.168.8.15", "CUPBOARD4", compressData, CUPBOARD_4_LENGTH, 1, CUPBOARD_4_START, false) { FramesPerBuffer = 500, BatchSize = 10 },
            //new LightStrip("192.168.8.15", "CUPBOARD4", compressData, CUPBOARD_4_LENGTH, 1, CUPBOARD_4_START, false) { FramesPerBuffer = 500, BatchSize = 10 },
        };

        public ScheduledEffect[] _LEDEffects =
        {
                /* Walking marquee for testing
                new ScheduledEffect(ScheduledEffect.AllDays,  0, 24,
                    new PaletteEffect( new Palette(new CRGB [] { CRGB.Red, CRGB.Green, CRGB.Blue }))
                        { _LEDColorPerSecond = 20,
                        _LEDScrollSpeed = 3,
                        _EveryNthDot = 4,
                        _DotSize = 1,
                        _Mirrored = false,
                        _Brightness = 1.0,
                        _Density = 8}),
                */
            // new ScheduledEffect(ScheduledEffect.AllDays,  5, 21, new SimpleColorFillEffect(CRGB.Yellow, 1)),
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new SimpleColorFillEffect(CRGB.Orange, 1))
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new SimpleColorFillEffect(new CRGB(64, 255, 128), 1))
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new PaletteEffect(Palette.Rainbow) { _EveryNthDot = 1, _DotSize = 1, _Density = 0.075/32 * PIXELS_PER_METER144, _LEDColorPerSecond = 0.5 }),
            
            new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new PaletteEffect(Palette.Rainbow) 
            { 
                _EveryNthDot = 1, 
                _DotSize = 1, 
                _Density = 0.025/32 * PIXELS_PER_METER144, 
                _LEDColorPerSecond = 3 
            }),
        };

        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    };

    // ShopSouthWindows
    //
    // Location definition for the lights int the 3-window south shop bay window

    
    public class ShopSouthWindows1 : Location
    {
        const bool compressData = true;

        const int WINDOW_START = 0;
        const int WINDOW_1_START = 0;
        const int WINDOW_1_LENGTH = 100;

        const int WINDOW_LENGTH = WINDOW_1_LENGTH;

        private CRGB[] _LEDs = InitializePixels<CRGB>(WINDOW_LENGTH);
        
        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.8", "WINDOW1", compressData, WINDOW_1_LENGTH, 1, WINDOW_1_START, false) { FramesPerBuffer = 21, BatchSize = 1 } ,
        };

        public ScheduledEffect[] _LEDEffects = 
        {
            new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new SimpleColorFillEffect(new CRGB(255, 112, 0), 1)),
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, EffectsDatabase.FireWindow),
             
        };


        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    }

    public class ShopSouthWindows2 : Location
    {
        const bool compressData = true;

        const int WINDOW_START = 0;
        const int WINDOW_1_START = 0;
        const int WINDOW_1_LENGTH = 100;

        const int WINDOW_LENGTH = WINDOW_1_LENGTH;

        private CRGB[] _LEDs = InitializePixels<CRGB>(WINDOW_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.9", "WINDOW2", compressData, WINDOW_1_LENGTH, 1, WINDOW_1_START, false) { BatchSize = 1 },
        };

        public ScheduledEffect[] _LEDEffects =
        {
            new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new SimpleColorFillEffect(CRGB.Blue, 1)),
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, EffectsDatabase.FireWindow),
            
        };


        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    }

    public class ShopSouthWindows3 : Location
    {
        const bool compressData = true;

        const int WINDOW_START = 0;
        const int WINDOW_1_START = 0;
        const int WINDOW_1_LENGTH = 100;

        const int WINDOW_LENGTH = WINDOW_1_LENGTH;

        private CRGB[] _LEDs = InitializePixels<CRGB>(WINDOW_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.10", "WINDOW3", compressData, WINDOW_1_LENGTH, 1, WINDOW_1_START, false) { BatchSize = 1 },
        };

        public ScheduledEffect[] _LEDEffects =
        {
            new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new SimpleColorFillEffect(CRGB.Green, 1)),
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, EffectsDatabase.FireWindow),
            
        };


        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    }
    
    public class ShopSouthWindows : Location
    {
        const bool compressData = true;

        const int WINDOW_START = 0;
        const int WINDOW_1_START = 0;
        const int WINDOW_1_LENGTH = 5 * 144;
        const int WINDOW_2_START = WINDOW_1_START + WINDOW_1_LENGTH;
        const int WINDOW_2_LENGTH = 5 * 144;                                   // 90 cut from one 
        const int WINDOW_3_START = WINDOW_2_START + WINDOW_2_LENGTH;
        const int WINDOW_3_LENGTH = 5 * 144;

        const int WINDOW_LENGTH = WINDOW_1_LENGTH + WINDOW_2_LENGTH + WINDOW_3_LENGTH;

        private CRGB[] _LEDs = InitializePixels<CRGB>(WINDOW_LENGTH);

        private LightStrip[] _StripControllers =
        {
            new LightStrip("192.168.8.8",  "WINDOW1", compressData, WINDOW_1_LENGTH, 1, WINDOW_1_START, false),
            new LightStrip("192.168.8.9",  "WINDOW2", compressData, WINDOW_2_LENGTH, 1, WINDOW_2_START, false),
            new LightStrip("192.168.8.10", "WINDOW3", compressData, WINDOW_3_LENGTH, 1, WINDOW_3_START, false),
        };

        public ScheduledEffect[] _LEDEffects =
        {
            new ScheduledEffect(ScheduledEffect.AllDays, 0, 24,

                new PaletteEffect(false ? Palette.Rainbow : new Palette( new CRGB[] {  CRGB.Orange, CRGB.Green, CRGB.Blue } ))
                {
                    _Density = (Double)Palette.Rainbow.OriginalSize / WINDOW_LENGTH * PIXELS_PER_METER144, 
                    _LEDScrollSpeed = 0, 
                    _LEDColorPerSecond = false ? 0.05 : 0, 
                    _DotSize = WINDOW_1_LENGTH, 
                    _EveryNthDot = WINDOW_1_LENGTH, 
                    _Brightness = 1,
                }
            ),
            
        };


        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    };
    
    

    // ShopEastWindows
    //
    // Location definition for the lights int the 3-window south shop bay window


    public class ShopEastWindows : Location
    {
        const bool compressData = true;

        const int WINDOW_START = 0;
        const int WINDOW_2_START = 0;
        const int WINDOW_2_LENGTH = 300;

        const int WINDOW_LENGTH = WINDOW_2_LENGTH;

        private CRGB[] _LEDs = InitializePixels<CRGB>(WINDOW_LENGTH);

        private LightStrip[] _StripControllers =
        {           // 192.168.1.18
            new LightStrip("192.168.8.22", "WINDOWEAST", compressData, WINDOW_2_LENGTH, 1, WINDOW_2_START, false),
        };
        private static readonly ScheduledEffect[] _LEDEffects =
        {
            // new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new SimpleColorFillEffect(CRGB.Blue, 3))

            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new FireEffect(WINDOW_2_LENGTH, true)),

            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, EffectsDatabase.Football_Effect_Seattle),
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, EffectsDatabase.ChristmasLights),
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new SimpleColorFillEffect(CRGB.GetBlackbodyHeatColor(0.80), 4))
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new SimpleColorFillEffect(CRGB.Green, 2)),
            //new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new PaletteEffect(Palette.Rainbow)
            //    {
            //        _Density = 0.0025, _LEDScrollSpeed = 5, _LEDColorPerSecond = 5, _DotSize = 1, _EveryNthDot = 1, _Brightness = 0.5
            //    })
            new ScheduledEffect(ScheduledEffect.AllDays, 0, 24, new PaletteEffect(Palette.Rainbow) { _EveryNthDot = 1, _DotSize = 1, _Density = 0.25/32 * PIXELS_PER_METER144, _LEDColorPerSecond = 3, _Brightness = 0.25 }),
        };

        public override LightStrip[] LightStrips { get { return _StripControllers; } }
        public override ScheduledEffect[] LEDEffects { get { return _LEDEffects; } }
        public override CRGB[] LEDs { get { return _LEDs; } }
    };
    
}
