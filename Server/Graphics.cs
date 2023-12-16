// +--------------------------------------------------------------------------
//  NightDriver - (c) 2019 Dave Plummer.  All Rights Reserved.
//
//  File:        Graphics.cs
//
//  Description: Given a DotMatrix object this class provides a set of drawing
//               primitives like line, circle, and so on.
//
//  History:     3/25/2019         Davepl      Created
//               6/06/2019         Davepl      Adapted for standalone app
//
//  Notes:       This file provides functionality similar to FastLED on the
//               Arduino platform; although it takes little to no code directly,
//               I am including their license info here since I referred to it
//               constantly!  I am NOT incorporating their license, since its
//               new code, just making sure to give them credit!
//
// Copyright (c) 2013 FastLED - For THIS SOURCE file only:
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// -----------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Linq;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type

namespace NightDriver
{
    // Utilities
    // 
    // Helpers and extension methods of general use throughout

    public static class Utilities
    {
        public static string Left(this string s, int length)
        {
            length = Math.Max(length, 0);

            if (s.Length > length)
            {
                return s.Substring(0, length);
            }
            else
            {
                return s;
            }
        }

        public static double constrain(this double value, double inclusiveMinimum, double inclusiveMaximum)
        {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }

        static Random _random = new Random();
        public static byte RandomByte()
        {
            return (byte)_random.Next(0, 256);
        }

        public static double RandomDouble()
        {
            return _random.NextDouble();
        }

        public static double RandomDouble(double low, double high)
        {
            return _random.NextDouble() * (high - low) + low;
        }

        public static double UnixTime()
        {
            return DateTime.UtcNow.Ticks - 6213559680000000000;
        }

        public static double UnixSeconds()
        {
            double epoch = (DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks) / (double) TimeSpan.TicksPerSecond;

            //ulong seconds = (ulong)epoch;                                       // Whole part of time number (left of the decimal point)
            //ulong uSeconds = (ulong)((epoch - (int)epoch) * 1000000);           // Fractional part of time (right of the decimal point)

             return epoch;
        }

    }

    // CRGB
    //
    // A class that represents 24 bits of color, no alpha.  This class is similar to the CRGB class in FastLED

    public class CRGB
    {
        public byte r;
        public byte g;
        public byte b;

        public CRGB()
        {
            r = 254;                // Default to bright magenta instead of black to catch uninit things sooner visually
            g = 0;
            b = 254;

        }

        public CRGB(byte red, byte green, byte blue)
        {
            r = red;
            g = green;
            b = blue;
        }

        public CRGB(UInt32 input)
        {
            r = (byte)((input >> 16) & 0xFF);
            g = (byte)((input >> 8) & 0xFF);
            b = (byte)((input) & 0xFF);
        }

        public CRGB(CRGB other)
        {
            r = other.r;
            g = other.g;
            b = other.b;
        }

        static Random _random = new Random();

        public static CRGB RandomSaturatedColor
        {
            get
            {
                return HSV2RGB(_random.Next(0, 360), 1.0, 1.0);
            }
        }

        /*
        public CRGB Blend(CRGB with, double ratio = 0.5f)
        {
            Color32 c1 = new Color32(this.r, this.g, this.b, 255);
            Color32 c2 = new Color32(with.r, with.g, with.b, 255);
            Color32 c3 = Color32.Lerp(c1, c2, ratio);
            return new CRGB(c3.r, c3.g, c3.b);
        }
        */

        public static CRGB Black { get { return new CRGB(0, 0, 0); } }
        public static CRGB White { get { return new CRGB(255, 255, 255); } }
        public static CRGB Grey { get { return new CRGB(160, 160, 160); } }

        public static CRGB Red { get { return new CRGB(255, 0, 0); } }
        public static CRGB Maroon { get { return new CRGB(255, 0, 128); } }
        public static CRGB Blue { get { return new CRGB(0, 0, 255); } }
        public static CRGB Cyan { get { return new CRGB(0, 255, 255); } }
        public static CRGB Green { get { return new CRGB(0, 255, 0); } }
        public static CRGB Yellow { get { return new CRGB(255, 255, 0); } }
        public static CRGB Purple { get { return new CRGB(255, 0, 255); } }
        public static CRGB Pink { get { return new CRGB(255, 0, 128); } }
        public static CRGB Orange { get { return new CRGB(255, 128, 0); } }

        public static CRGB Incandescent { get { return new CRGB(255, 214, 170); } }

        public UInt32 ColorValueAsInt()
        {
            return (uint)(r << 16) + (uint)(g << 8) + (uint)b;
        }

        public Color GetColor()
        {
            return Color.FromArgb(r, g, b);
        }

        public CRGB setRGB(byte red, byte green, byte blue)
        {
            r = red;
            g = green;
            b = blue;
            return this;
        }

        public double h
        {
            get
            {
                return System.Drawing.Color.FromArgb(r, g, b).GetHue();
            }
            set
            {
                System.Drawing.Color color = System.Drawing.Color.FromArgb(r, g, b);
                double s = color.GetSaturation();
                //double v = color.GetBrightness();
                CRGB c = HSV2RGB(value * 360.0, s, 1);
                r = c.r;
                g = c.g;
                b = c.b;
            }
        }

        public double RGB2HSV(out double s, out double v)
        {
            System.Drawing.Color color = System.Drawing.Color.FromArgb(r, g, b);
            s = color.GetSaturation();
            v = color.GetBrightness();
            return color.GetHue();
        }

        public static CRGB HSV2RGB(double h, double s = 1.0, double v = 1.0)
        {
            h %= 360;

            double hh, p, q, t, ff;
            long i;
            CRGB outval = new CRGB();

            if (s <= 0.0)                       // No saturaturation, return a greyscale
            {
                outval.r = (byte)(v * 255);
                outval.g = (byte)(v * 255);
                outval.b = (byte)(v * 255);
                return outval;
            }

            hh = h;
            if (hh >= 360.0)
                hh = 0.0;
            hh /= 60.0;
            i = (long)hh;
            ff = hh - i;
            p = v * (1.0 - s);
            q = v * (1.0 - (s * ff));
            t = v * (1.0 - (s * (1.0 - ff)));

            switch (i)
            {
                case 0:
                    outval.r = (byte)(255 * v);
                    outval.g = (byte)(255 * t);
                    outval.b = (byte)(255 * p);
                    break;

                case 1:
                    outval.r = (byte)(255 * q);
                    outval.g = (byte)(255 * v);
                    outval.b = (byte)(255 * p);
                    break;

                case 2:
                    outval.r = (byte)(255 * p);
                    outval.g = (byte)(255 * v);
                    outval.b = (byte)(255 * t);
                    break;

                case 3:
                    outval.r = (byte)(255 * p);
                    outval.g = (byte)(255 * q);
                    outval.b = (byte)(255 * v);
                    break;

                case 4:
                    outval.r = (byte)(255 * t);
                    outval.g = (byte)(255 * p);
                    outval.b = (byte)(255 * v);
                    break;

                case 5:
                default:
                    outval.r = (byte)(255 * v);
                    outval.g = (byte)(255 * p);
                    outval.b = (byte)(255 * q);
                    break;
            }

            return outval;
        }


        private CRGB scaleColorsDownTo(double amount)
        {
            r = (byte)(r * amount);
            g = (byte)(g * amount);
            b = (byte)(b * amount);
            return this;
        }

        private CRGB scaleColorsUpBy(double amount)
        {
            r = (byte)Math.Clamp(r * amount, 0, 255);
            g = (byte)Math.Clamp(g * amount, 0, 255);
            b = (byte)Math.Clamp(b * amount, 0, 255);
            return this;
        }

        public CRGB brightenBy(double amt)
        {
            CRGB copy = new CRGB(this);
            double amountToBrighten = amt;
            copy.scaleColorsUpBy(1.0f + amountToBrighten);
            return copy;
        }

        public CRGB maximizeBrightness()
        {
            CRGB copy = new CRGB(this);
            byte max = Math.Max(copy.r, Math.Max(copy.g, copy.b));
            if (max > 0)
            {
                // What's it going to take to make you into 255?
                double factor = ((double)(255) * 256) / max;

                // Apply that much to all the color components
                copy.r = (byte)((copy.r * factor) / 256.0);
                copy.g = (byte)((copy.g * factor) / 256.0);
                copy.b = (byte)((copy.b * factor) / 256.0);
            }
            return copy;
        }

        public CRGB fadeToBlackBy(double amt)
        {
            CRGB copy = new CRGB(this);
            double amountToFade = Utilities.constrain(amt, 0.0f, 1.0f);
            copy.scaleColorsDownTo(1.0f - amountToFade);
            return copy;
        }

        public CRGB blendWith(CRGB other, double amount = 0.5)
        {
            byte r = (byte)(this.r * amount + other.r * (1.0 - amount));
            byte g = (byte)(this.g * amount + other.g * (1.0 - amount));
            byte b = (byte)(this.b * amount + other.b * (1.0 - amount));
            return new CRGB(r, g, b);
        }

        public static CRGB operator +(CRGB left, CRGB right)
        {
            byte r = (byte)Math.Clamp(left.r + right.r, 0, 255);
            byte g = (byte)Math.Clamp(left.g + right.g, 0, 255);
            byte b = (byte)Math.Clamp(left.b + right.b, 0, 255);
            return new CRGB(r, g, b);
        }

        public static CRGB operator *(CRGB left, double right)
        {
            byte r = (byte)(left.r * right);
            byte g = (byte)(left.g * right);
            byte b = (byte)(left.b * right);
            return new CRGB(r, g, b);
        }

        public static CRGB GetBlackbodyHeatColor(double temp)
        {
            temp = Math.Min(1.0f, temp);
            byte temperature = (byte)(255 * temp);
            byte t192 = (byte)Math.Round((temperature / 255.0f) * 191);

            byte heatramp = (byte)(t192 & 0x3F);
            heatramp <<= 2;

            if (t192 > 0x80)
                return new CRGB(255, 255, heatramp);
            else if (t192 > 0x40)
                return new CRGB(255, heatramp, 0);
            else
                return new CRGB(heatramp, 0, 0);
        }

        // Step through the color array provided, filling in the colors beginning at startPos, and filling them
        // with a mix that runs from start to end in color.

        public static CRGB[] makeGradient(CRGB start, CRGB end, int length = 16)
        {
            CRGB[] array = new CRGB[length];
            double currentFraction = (double)1 / length;
            double currentRemainder = 1.0 - currentFraction;
            double fractionIncrement = (double)1 / length;

            // Because the multiplication operator for color returns a fraction of a color, and because the additioon operator can
            // add them, we can generate the color steps along the way quite easily...

            for (int iPos = 0; iPos < length; iPos++, currentFraction += fractionIncrement, currentRemainder -= fractionIncrement)
                array[iPos] = start * currentFraction + end * currentRemainder;

            return array;
        }

        public static CRGB[] AllBlue => Enumerable.Repeat(CRGB.Blue, 16).ToArray();

        public static CRGB[] BlueStars => new CRGB[]
        {
            new CRGB(0, 0, 32),
            new CRGB(0, 0, 64),
            new CRGB(0, 0, 96),
            new CRGB(0, 0, 112),
            new CRGB(0, 0, 128),
            new CRGB(0, 0, 144),
            new CRGB(0, 0, 160),
            new CRGB(0, 0, 176),
            new CRGB(0, 0, 192),
            new CRGB(0, 0, 208),
            new CRGB(0, 0, 224),
            new CRGB(0, 0, 240),
            new CRGB(0, 0, 255),
            new CRGB(0, 32, 255),
            new CRGB(0, 64, 255),
            new CRGB(0, 96, 255),
            new CRGB(0, 128, 255),
            new CRGB(0, 144, 255),
            new CRGB(0, 176, 255),
            new CRGB(0, 192, 255),
            new CRGB(0, 208, 255),
            new CRGB(0, 224, 255),
            new CRGB(0, 240, 255),
            new CRGB(0, 255, 255),
            new CRGB(32, 255, 255),
            new CRGB(64, 255, 255),
            new CRGB(96, 255, 255),
            new CRGB(128, 255, 255),
            new CRGB(144, 255, 255),
            new CRGB(176, 255, 255),
            new CRGB(192, 255, 255),
            new CRGB(208, 255, 255),
            new CRGB(224, 255, 255),
            new CRGB(240, 255, 255),
            new CRGB(255, 255, 255),

        };

        public static CRGB[] HotStars => new CRGB[16]
        {
            CRGB.GetBlackbodyHeatColor(0.00),
            CRGB.GetBlackbodyHeatColor(0.02),
            CRGB.GetBlackbodyHeatColor(0.05),
            CRGB.GetBlackbodyHeatColor(0.08),
            CRGB.GetBlackbodyHeatColor(0.10),
            CRGB.GetBlackbodyHeatColor(0.12),
            CRGB.GetBlackbodyHeatColor(0.15),
            CRGB.GetBlackbodyHeatColor(0.28),
            CRGB.GetBlackbodyHeatColor(0.20),
            CRGB.GetBlackbodyHeatColor(0.22),
            CRGB.GetBlackbodyHeatColor(0.25),
            CRGB.GetBlackbodyHeatColor(0.27),
            CRGB.GetBlackbodyHeatColor(0.50),
            CRGB.GetBlackbodyHeatColor(0.60),
            CRGB.GetBlackbodyHeatColor(0.70),
            CRGB.GetBlackbodyHeatColor(0.80)
        };


        public static CRGB[] FrostyStars => new CRGB[16]
        {
            new CRGB(0, 0, 96),
            new CRGB(0, 0, 112),
            new CRGB(0, 0, 128),
            new CRGB(0, 0, 144),
            new CRGB(0, 0, 160),
            new CRGB(0, 0, 176),
            new CRGB(0, 0, 192),
            new CRGB(0, 0, 208),
            new CRGB(0, 0, 224),
            new CRGB(0, 0, 240),
            new CRGB(0, 0, 255),
            new CRGB(32, 32, 255),
            new CRGB(64, 64, 255),
            new CRGB(96, 96, 255),
            new CRGB(112, 112, 255),
            new CRGB(196, 196, 255)
        };

        public static CRGB[] ChristmasLights => new CRGB[]
        {
            CRGB.Red,
            CRGB.Green,
            CRGB.Blue,
            CRGB.Orange,
            CRGB.Purple,
        };

        // Vintage GE lamps as sampled by Eric Maffei for Davepl

        public static CRGB[] VintageChristmasLights => new CRGB[]
        {
            new CRGB(238, 51, 39),      // Red
            new CRGB(0, 172, 87),       // Green
            new CRGB(250, 164, 25),     // Yellow
            new CRGB(0, 131, 203)       // Blue
        };

        public static CRGB[] BluePeak => new CRGB[]
        {
            new CRGB(0, 0, 8),
            new CRGB(0, 0, 32),
            new CRGB(0, 0, 64),
            new CRGB(0, 0, 96),
            new CRGB(0, 0, 128),
            new CRGB(0, 0, 160),
            new CRGB(0, 0, 192),
            new CRGB(0, 0, 255),
            new CRGB(0, 0, 192),
            new CRGB(0, 0, 160),
            new CRGB(0, 0, 128),
            new CRGB(0, 0, 96),
            new CRGB(0, 0, 64),
            new CRGB(0, 0, 32),
            new CRGB(0, 0, 8),
            new CRGB(0, 0, 8)
        };

        public static CRGB[] RedPeak => new CRGB[]
        {
            new CRGB(8,   0, 0),
            new CRGB(32,  0, 0),
            new CRGB(64,  0, 0),
            new CRGB(96,  0, 0),
            new CRGB(128, 0, 0),
            new CRGB(160, 0, 0),
            new CRGB(192, 0, 0),
            new CRGB(255, 0, 0),
            new CRGB(192, 0, 0),
            new CRGB(160, 0, 0),
            new CRGB(128, 0, 0),
            new CRGB(96,  0, 0),
            new CRGB(64,  0, 0),
            new CRGB(32,  0, 0),
            new CRGB(8,   0, 0),
            new CRGB(8,   0, 0)
        };

        public static CRGB[] BlueSpectrum => new CRGB[]
        {
            CRGB.Black,
            CRGB.Red,
            CRGB.Black,
            CRGB.Orange,
            CRGB.Black,
            CRGB.Yellow,
            CRGB.Black,
            CRGB.Green,
            CRGB.Black,
            CRGB.Cyan,
            CRGB.Black,
            CRGB.Blue,
            CRGB.Black,
            CRGB.Purple,
            CRGB.Black,
            CRGB.Green
        };

        public static CRGB[] RainbowStripes => new CRGB[]
        {
            CRGB.Black, 
            CRGB.Red,
            CRGB.Black, 
            CRGB.Orange,   
            CRGB.Black, 
            CRGB.Yellow,   
            CRGB.Black, 
            CRGB.Green,
            CRGB.Black, 
            CRGB.Cyan,
            CRGB.Black, 
            CRGB.Blue,
            CRGB.Black, 
            CRGB.Purple,   
            CRGB.Black, 
            CRGB.Green
        };

        public static CRGB[] Rainbow => new CRGB[]
        {
            CRGB.Red,
            CRGB.Orange,
            CRGB.Yellow,
            CRGB.Green,
            CRGB.Cyan,
            CRGB.Blue,
            new CRGB(128, 0, 255),
            CRGB.Purple,
        };

        public static CRGB[] BlueFlame => new CRGB[]
        {
            CRGB.Black,
            new CRGB(0, 0, 16),
            new CRGB(0, 0, 128),
            new CRGB(0, 0, 192),
            new CRGB(0, 0, 255),
            new CRGB(0, 128, 255),
            CRGB.White
        };

        public static CRGB[] Emergency => new CRGB[]
        {
            CRGB.Blue,
            CRGB.Blue,
            CRGB.Black,
            CRGB.Black,
            CRGB.Red,
            CRGB.Red,
            CRGB.Black,
            CRGB.Black,
        };
        public static CRGB[] URegina => new CRGB[]
        {
            new CRGB(0, 0x8F, 0x2e),
            new CRGB(0, 0x8F, 0x2e),
            CRGB.Black,
            CRGB.Black,
            new CRGB(0xff, 0xc8, 0x2e),
            new CRGB(0xff, 0xc8, 0x2e),
            CRGB.Black,
            CRGB.Black,
        };
        public static CRGB[] Rainbow2 => new CRGB[]
        {
            CRGB.Red,
            CRGB.Red,

            CRGB.Red.blendWith(CRGB.Orange),

            CRGB.Orange,

            CRGB.Yellow,
            CRGB.Yellow,

            CRGB.Green,
            CRGB.Green.blendWith(CRGB.Cyan),

            CRGB.Cyan,

            //CRGB.Cyan.blendWith(CRGB.Blue, .75),
            //CRGB.Cyan.blendWith(CRGB.Blue, .25),


            CRGB.Blue,
            CRGB.Blue,

            CRGB.Blue.blendWith(CRGB.Purple, .50),

            CRGB.Purple,
        
        };

        public static CRGB[] Reds => new CRGB[]
        {
            CRGB.Red,
            CRGB.Orange,
            CRGB.Yellow,
            CRGB.Orange,
        };

        public static CRGB[] Football_Regina => new CRGB[]
        {
            new CRGB(0, 0x62, 0x3F),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(180, 180, 180),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(180, 180, 180),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(255, 255, 255),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(180, 180, 180),
            new CRGB(0, 0x62, 0x3F),
            new CRGB(0, 0x62, 0x3F)
        };

        public static CRGB[] Football_Regina2 => new CRGB[]
        {
            new CRGB(0, 98, 63),
            new CRGB(0, 98, 63),
            new CRGB(180, 180, 180),
            new CRGB(0, 98, 63),
            new CRGB(0, 98, 63),
            new CRGB(0, 98, 63),
            new CRGB(0, 98, 63),
            new CRGB(180, 180, 180),
            new CRGB(0, 98, 63),
            new CRGB(0, 98, 63),
            new CRGB(255, 255, 255),
            new CRGB(255, 255, 255),
            new CRGB(255, 255, 255),
            new CRGB(180, 180, 180),
            new CRGB(0, 98, 63),
            new CRGB(0, 98, 63),
        };

        public static CRGB[] Football_Seattle => new CRGB[]
        {
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(0,21,50),
            new CRGB(0,21,50),
            new CRGB(0,21,50),
            new CRGB(0,21,50),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(54,87,140),
            new CRGB(78,167,1),
            new CRGB(78,167,1),
            new CRGB(78,167,1),
            new CRGB(78,167,1),
            new CRGB(78,167,1),
            new CRGB(78,167,1),
            new CRGB(54,87,140),
        };


        public static CRGB[] Football_Seattle_Orig => new CRGB[]
        {
            CRGB.White,
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(54,87,140),
            new CRGB(78,167,1),
            new CRGB(144,145,140),
            new CRGB(3,38,58),
            new CRGB(54,87,140),
            new CRGB(78,167,1),
            new CRGB(144,145,140),
            new CRGB(3,38,58),
            new CRGB(3,38,58),
            new CRGB(54,87,140),
            new CRGB(78,167,1),
            new CRGB(144,145,140),
            new CRGB(3,38,58),
            new CRGB(54,87,140),
            new CRGB(78,167,1),
        };

        public static CRGB[] Football_Seattle_3 => new CRGB[]
        {
            new CRGB(0,21, 50),
            new CRGB(105,190,40),
            new CRGB(129,138,143),
            new CRGB(1, 51, 105)
        };

        public static CRGB[] DarkRainbow => new CRGB[]
        {
            CRGB.Red.fadeToBlackBy(0.8f),
            CRGB.Orange.fadeToBlackBy(0.8f),
            CRGB.Yellow.fadeToBlackBy(0.8f),
            CRGB.Green.fadeToBlackBy(0.8f),
            CRGB.Cyan.fadeToBlackBy(0.8f),
            CRGB.Blue.fadeToBlackBy(0.8f),
            CRGB.Purple.fadeToBlackBy(0.8f),
            CRGB.Pink.fadeToBlackBy(0.8f),
            CRGB.Red.fadeToBlackBy(0.8f),
            CRGB.Orange.fadeToBlackBy(0.8f),
            CRGB.Yellow.fadeToBlackBy(0.8f),
            CRGB.Green.fadeToBlackBy(0.8f),
            CRGB.Cyan.fadeToBlackBy(0.8f),
            CRGB.Blue.fadeToBlackBy(0.8f),
            CRGB.Purple.fadeToBlackBy(0.8f),
            CRGB.Pink.fadeToBlackBy(0.8f)
        };


        public static CRGB[] DavisAndArizona => new CRGB[]
        {
            new CRGB(218, 170, 0),
            CRGB.Black,
            new CRGB(0, 80, 170),
            CRGB.Black,
            new CRGB(218, 170, 0),
            CRGB.Black,
            new CRGB(0, 80, 170),
            CRGB.Black,
            new CRGB(251, 2, 25),
            CRGB.Black,
            new CRGB(0, 0, 255),
            CRGB.Black,
            new CRGB(251, 2, 25),
            CRGB.Black,
            new CRGB(0, 0, 255),
            CRGB.Black,
        };
    }

    public interface ILEDGraphics
    {
        void DrawCircleHelper(uint x0, uint y0, uint r, uint cornername, CRGB color);
        void DrawCircle(uint x0, uint y0, uint r, bool color);
        void DrawCircle(uint x0, uint y0, uint r, CRGB color);
        void DrawRect(uint x, uint y, uint w, uint h, bool color);
        void DrawRect(uint x, uint y, uint w, uint h, CRGB color);
        void DrawFastVLine(uint x, uint y, uint h, CRGB color);
        void DrawFastHLine(uint x, uint y, uint w, CRGB color);
        void DrawRoundRect(uint x, uint y, uint w, uint h, uint r, CRGB color);
        void DrawLine(uint x0, uint y0, uint x1, uint y1, bool color);
        void DrawLine(uint x0, uint y0, uint x1, uint y1, CRGB color);
        void FillSolid(CRGB color);
        void Blur(uint passes = 1, uint start = 0, uint length = 0);
        void FillRainbow(double startHue = 0.0, double deltaHue = 5.0);

        CRGB GetPixel(uint x, uint y = 0);
        void DrawPixel(uint x, uint y, CRGB color);
        void DrawPixels(double fPos, double count, CRGB color);
        void DrawPixel(uint x, CRGB color);
        void BlendPixel(uint x, CRGB color);
        uint DotCount { get; }
        uint Width { get;  }
        uint Height { get;  }
        double PixelsPerMeter { get; }
    };



    abstract public class GraphicsBase : ILEDGraphics
    {

        private static bool IsSet(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        public virtual uint DotCount
        {
            get { return Width * Height ;}
        }

        public virtual double PixelsPerMeter
        {
            get { return 144.0; }
        }

        protected int16_t abs(int v)
        {
            return (int16_t)System.Math.Abs(v);
        }

        protected int16_t abs(int16_t v)
        {
            return System.Math.Abs(v);
        }

        protected void Swap(ref uint v1, ref uint v2)
        {
            uint v = v1;
            v1 = v2;
            v2 = v;
        }

        public void DrawCircleHelper(uint x0, uint y0, uint r, uint cornername, CRGB color)
        {
            int16_t f = (int16_t)(1 - r);
            int16_t ddF_x = 1;
            int16_t ddF_y = (int16_t)(-2 * r);
            int16_t x = 0;
            int16_t y = (int16_t) r;

            while (x < y)
            {
                if (f >= 0)
                {
                    y--;
                    ddF_y += 2;
                    f += ddF_y;
                }
                x++;
                ddF_x += 2;
                f += ddF_x;
                if (IsSet((byte)cornername, 0x4))
                {

                    DrawPixel((uint)(x0 + x), (uint)(y0 + y), color);
                    DrawPixel((uint)(x0 + y), (uint)(y0 + x), color);
                }
                if (IsSet((byte)cornername, 0x2))
                {
                    DrawPixel((uint)(x0 + x), (uint)(y0 - y), color);
                    DrawPixel((uint)(x0 + y), (uint)(y0 - x), color);
                }
                if (IsSet((byte)cornername, 0x8))
                {
                    DrawPixel((uint)(x0 - y), (uint)(y0 + x), color);
                    DrawPixel((uint)(x0 - x), (uint)(y0 + y), color);
                }
                if (IsSet((byte)cornername, 0x1))
                {
                    DrawPixel((uint)(x0 - y), (uint)(y0 - x), color);
                    DrawPixel((uint)(x0 - x), (uint)(y0 - y), color);
                }
            }
        }
            
        public void DrawCircle(uint x0, uint y0, uint r, bool b)
        {
            DrawCircle(x0, y0, r, b ? CRGB.Green : CRGB.Black);
        }

        // Draw a circle outline
        public void DrawCircle(uint x0, uint y0, uint r, CRGB color)
        {
            int16_t f = (int16_t)(1 - r);
            int16_t ddF_x = 1;
            int16_t ddF_y = (int16_t)(-2 * r);
            int16_t x = 0;
            int16_t y = (int16_t) r;

            DrawPixel(x0, (y0 + r), color);
            DrawPixel(x0, (y0 - r), color);
            DrawPixel((x0 + r), y0, color);
            DrawPixel((x0 - r), y0, color);

            while (x < y)
            {
                if (f >= 0)
                {
                    y--;
                    ddF_y += 2;
                    f += ddF_y;
                }
                x++;
                ddF_x += 2;
                f += ddF_x;

                DrawPixel((uint)(x0 + x), (uint)(y0 + y), color);
                DrawPixel((uint)(x0 - x), (uint)(y0 + y), color);
                DrawPixel((uint)(x0 + x), (uint)(y0 - y), color);
                DrawPixel((uint)(x0 - x), (uint)(y0 - y), color);
                DrawPixel((uint)(x0 + y), (uint)(y0 + x), color);
                DrawPixel((uint)(x0 - y), (uint)(y0 + x), color);
                DrawPixel((uint)(x0 + y), (uint)(y0 - x), color);
                DrawPixel((uint)(x0 - y), (uint)(y0 - x), color);
            }
        }

        public void DrawRect(uint x, uint y, uint w, uint h, CRGB color)
        {
            DrawFastHLine(x, y, w, color);
            DrawFastHLine(x, (y + h - 1), w, color);
            DrawFastVLine(x, y, h, color);
            DrawFastVLine((x + w - 1), y, h, color);
        }

        public void DrawRect(uint x, uint y, uint w, uint h, bool b)
        {
            DrawRect(x, y, w, h, b ? CRGB.Green : CRGB.Black);
        }

        public void DrawFastVLine(uint x, uint y, uint h, CRGB color)
        {
            DrawLine(x, y, x, (y + h - 1), color);
        }

        public void DrawFastHLine(uint x, uint y, uint w, CRGB color)
        {
            // Update in subclasses if desired!
            DrawLine(x, y, (x + w - 1), y, color);
        }
        public void DrawRoundRect(uint x, uint y, uint w, uint h, uint r, CRGB color)
        {
            // smarter version
            DrawFastHLine(x + r, y, w - 2 * r, color); // Top
            DrawFastHLine(x + r, y + h - 1, w - 2 * r, color); // Bottom
            DrawFastVLine(x, y + r, h - 2 * r, color); // Left
            DrawFastVLine(x + w - 1, y + r, h - 2 * r, color); // Right
                                                               // draw four corners
            DrawCircleHelper(x + r, y + r, r, 1, color);
            DrawCircleHelper(x + w - r - 1, y + r, r, 2, color);
            DrawCircleHelper(x + w - r - 1, y + h - r - 1, r, 4, color);
            DrawCircleHelper(x + r, y + h - r - 1, r, 8, color);
        }
       
        public void DrawLine(uint x0, uint y0, uint x1, uint y1, bool color)
        {
            this.DrawLine(x0, y0, x1, y1, (CRGB)(color ? new CRGB(0xFFFFFF) : new CRGB(0x000000)));
        }

        // Bresenham's algorithm - thx wikpedia
        public void DrawLine(uint x0, uint y0, uint x1, uint y1, CRGB color)
        {
            bool steep = abs((int)y1 - (int)y0) > abs((int)x1 - (int)x0);
            if (steep)
            {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }

            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }

            int16_t dx, dy;
            dx = (int16_t)(x1 - x0);
            dy = (int16_t)(abs((int)y1 - (int)y0));

            int16_t err = (int16_t)(dx / 2);
            int16_t ystep;

            if (y0 < y1)
            {
                ystep = 1;
            }
            else
            {
                ystep = -1;
            }

            for (; x0 <= x1; x0++)
            {
                if (steep)
                {
                    DrawPixel(y0, x0, color);
                }
                else
                {
                    DrawPixel(x0, y0, color);
                }
                err -= dy;
                if (err < 0)
                {
                    y0 += (uint) ystep;
                    err += dx;
                }
            }
        }

        public void FillSolid(CRGB color)
        {
            for (uint x = 0; x < Width; x++)
                for (uint y = 0; y < Height; y++)
                    DrawPixel(x, y, color);
        }
            
        public void FillRainbow(double startHue = 0.0, double deltaHue = 5.0f)
        {
            double hue = startHue;
            for (uint y = 0; y < Height; y++)
            {
                for (uint x = 0; x < Width; x++)
                {
                    CRGB color = CRGB.HSV2RGB(hue % 360);
                    DrawPixel(x, y, color);
                    hue += deltaHue;
                }
            }
        }

        // Blur
        //
        // Does a quick 5-element kernel Gaussian blur on the LED buffer

        public void Blur(uint passes = 1, uint start = 0, uint length = 0)
        {
            for (uint pass = 0; pass < passes; pass++)
            {
                for (uint pos = 0; pos < DotCount; pos++)
                {
                    CRGB sum = GetPixel(pos - 2) * 0.06136 +
                               GetPixel(pos - 1) * 0.24477 +
                               GetPixel(pos)     * 0.38774 +
                               GetPixel(pos + 1) * 0.24477 +
                               GetPixel(pos + 2) * 0.06136;
                    DrawPixel(pos, sum);
                }
            }
        }

        // Your implementation class must provide these, the actual writing of pixels

        public abstract CRGB GetPixel(uint x, uint y = 0);
        public abstract void DrawPixel(uint x, uint y, CRGB color);
        public abstract void DrawPixels(double fPos, double count, CRGB color);
        public abstract void DrawPixel(uint x, CRGB color);
        public abstract void BlendPixel(uint x, CRGB color);


        public abstract uint Width
        {
            get;
        }

        public abstract uint Height
        {
            get;
        }

        public abstract uint LEDCount
        {
            get;
        }
    }
}