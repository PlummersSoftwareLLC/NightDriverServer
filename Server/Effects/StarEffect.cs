using System;
using System.Collections.Generic;
using NightDriver;


public class StarEffect<StarType> : LEDEffect where StarType : BaseStar
{
    protected Queue<BaseStar> _Stars = new Queue<BaseStar>();

    private Random _random = new Random(DateTime.Now.Millisecond);
    private DateTime _lastBaseColorUpdate = DateTime.UtcNow;

    public Palette    Palette               = new Palette(CRGB.Rainbow);
    public bool       Blend                 = false;
    public double     MaxSpeed              = 2.0f;
    public double     NewStarProbability    = 1;
    public double     StarPreignitonTime    = 0.5f;
    public double     StarIgnition          = 0.25f;
    public double     StarHoldTime          = 1.00f;
    public double     StarFadeTime          = 1.00f;
    public double     StarSize              = 1.00f;
    public double     ColorSpeed            = 0.0;
    public double     BaseColorSpeed        = 0.0;
    public bool       RandomStartColor      = false;
    public bool       RandomStarColorSpeed  = true;
    public int        Direction             = 0;
    public uint       Alignment             = 0;
    public bool       RampedColor           = false;
    private double    CurrentStartColor     = 0;


    protected override void Render(ILEDGraphics graphics)
    {
        double elapsed = (DateTime.UtcNow - _lastBaseColorUpdate).TotalSeconds;
        _lastBaseColorUpdate = DateTime.UtcNow;

        CurrentStartColor += BaseColorSpeed * elapsed * 16;

        for (int iPass = 0; iPass < graphics.DotCount / 10; iPass++)
        {
            if (_random.NextDouble() < NewStarProbability / 100.0)
            {
                double d = _random.NextDouble();
                if (d == 0)
                    _random = new Random(DateTime.Now.Millisecond);

                double pos = (d * graphics.DotCount);
                BaseStar star;
                double colorD = RandomStartColor ? _random.NextDouble() : CurrentStartColor;
                double speedD = RandomStarColorSpeed? _random.NextDouble() * ColorSpeed : ColorSpeed;

                // Create a star, which is done differently depending in which kind of star it is (hue vs palette).  Should
                // probably convert this to a factory pattern that can produce any type of Star, but this is a quick hack
                // to test things that will no doubt hand around untilt he the third or fourth type enters the scene...

                if (typeof(StarType).IsAssignableFrom(typeof(ColorStar)))
                    star = new ColorStar(pos,
                                         MaxSpeed,
                                         Direction,
                                         colorD,
                                         speedD);

                else if (typeof(StarType).IsAssignableFrom(typeof(AlignedPaletteStar)))
                    star = new AlignedPaletteStar(pos,
                                           MaxSpeed,
                                           Direction,
                                           Palette,
                                           colorD,
                                           speedD,
                                           Alignment);

                else if (typeof(StarType).IsAssignableFrom(typeof(PaletteStar)))
                    star = new PaletteStar(pos,
                                           MaxSpeed,
                                           Direction,
                                           Palette,
                                           colorD,
                                           speedD);
                else
                    star = null;

                _Stars.Enqueue(star);
            }
        }

        while (_Stars.Count > graphics.DotCount)
            _Stars.Dequeue();

        graphics.FillSolid(CRGB.Black);

        foreach (BaseStar star in _Stars)
        {
            star.Update();

            CRGB c = new CRGB(star.StarColor);

            // If the star is brand new, it flashes white briefly.  Otherwise it fades over time.
            double fade = 0.0f;
            double age = star.Age;

            double TimeToFadeOut = StarPreignitonTime + StarIgnition + StarHoldTime + StarFadeTime;
            double TimeToHold    = StarPreignitonTime + StarIgnition + StarHoldTime;
            double TimeToIgnite  = StarPreignitonTime + StarIgnition;
            double TimeToFadeIn  = StarPreignitonTime;

            if (age < TimeToFadeIn)
            {
                // In FadeIn
                fade = 1.0 - (age / TimeToFadeIn);
            }
            else if (age < TimeToIgnite)
            {
                // In Ignition (white)
                c = CRGB.White;
            }
            else if (age < TimeToHold)
            {
                // In Hold
                fade = 0.0;
            }
            else if (age <= TimeToFadeOut)
            {
                // In FadeOut
                double fadeTime = age - TimeToHold;
                fade = fadeTime / StarFadeTime;
            }
            else
            {
                // Dark matter (past it's lifetime, waiting to be removed)
                c = CRGB.Black;
            }

            c = c.fadeToBlackBy((double)fade);

            if (!RampedColor)
            {
                graphics.DrawPixels(star._position, (uint)StarSize, c);
            }
            else
            {
                double half = StarSize / 2;

                CRGB c1;
                for (int j = 0; j < StarSize; j++)
                {
                    double dx = Math.Abs(half - j);
                    dx /= half;

                    c1 = c.fadeToBlackBy(dx);

                    if ((int)j == (int)half)
                        c1 = c1.blendWith(CRGB.White, 0.75);

                    graphics.DrawPixels(star._position + j, 1, c1);
                }
            }

        }
        while (_Stars.Count > 0 && _Stars.Peek().Age > StarPreignitonTime + StarIgnition + StarHoldTime + StarFadeTime)
            _Stars.Dequeue();
    }
}

// BaseStar
//
// Star class that keeps track of the basics - position and color.  Position is updated in the Update() loop but color
// must be managed by the derived class or set externally.

public class BaseStar
{
    protected CRGB _starColor;
    public CRGB StarColor
    {
        get { return _starColor; }
        set { _starColor = value; }
    }

    protected Random _rand = new Random();

    public double          _position;
    public double          _velocity;
    public DateTime        _birthTime;
    public DateTime        _lastUpdate;

    public double Age
    {
        get
        {
            return (DateTime.UtcNow - _birthTime).TotalSeconds;
        }
    }

    public BaseStar(double pos, double maxSpeed = 0, int direction = 0)
    {

        _starColor = CRGB.Red;                                  
        _birthTime = DateTime.UtcNow;
        _velocity = _rand.NextDouble() * maxSpeed * 2 - maxSpeed;

        if (direction != 0)
            _velocity = Math.Abs(_velocity) * Math.Sign(direction);

        // If no speed, use an int-rounded slot so we don't span pixels

        _position = (maxSpeed == 0) ? (uint) pos : pos;

        _lastUpdate = DateTime.UtcNow;

    }

    public virtual void Update()
    {
        double deltaSeconds = (DateTime.UtcNow - _lastUpdate).TotalSeconds;

        _position += _velocity * deltaSeconds;
        _lastUpdate = DateTime.UtcNow;
    }
}

// PaletteStar
//
// A BaseStar derivation that keeps track of a palette and sweeps the star color through that palette

public class PaletteStar : BaseStar
{
    protected DateTime        _lastColorChange;
    protected double          _paletteSpeed;
    protected Palette         _palette;
    protected double          _paletteIndex;

    public PaletteStar(double pos, double maxSpeed, int direction, Palette palette, double paletteIndex, double paletteSpeed = 0.0) : base(pos, maxSpeed, direction)
    {
        _palette = palette;
        _lastUpdate = DateTime.UtcNow;
        _lastColorChange = DateTime.UtcNow;
        _paletteSpeed = paletteSpeed;
        _paletteIndex = paletteIndex;
        _palette.Blend = false;
    }
           
    public override void Update()
    {
        double deltaSeconds = (DateTime.UtcNow - _lastUpdate).TotalSeconds;

        base.Update();                                                                  // Resets _lastUpdate, so get value first

        _paletteIndex += _paletteSpeed * deltaSeconds;          //
        _paletteIndex -= (long) _paletteIndex;                  // Just keep digits
        StarColor = _palette[_paletteIndex];
    }
}

// AlignedPaletteStar
//
// A derivation of star that alawys stays aligned on some multiple for postion (like every 8th pixel)

public class AlignedPaletteStar : PaletteStar
{
    protected uint _alignment;

    public AlignedPaletteStar(double pos, double maxSpeed, int direction, Palette palette, double paletteIndex, double paletteSpeed = 0.0, uint alignment = 8)
        : base(pos, maxSpeed, direction, palette, paletteIndex, paletteSpeed)
    {
        _alignment = alignment;
    }

    public override void Update()
    {
        if (_alignment > 1)
        {
            long x = (int)_position;
            x = (x + (_alignment - 1)) / _alignment * _alignment;
            _position = x;
        }
        base.Update();
    }
}

// ColorStar
//
// A derivation of BaseStar that keeps track of it's current hue and changes it over time

public class ColorStar : BaseStar
{
    protected double _hue;
    protected double _hueSpeed;

    public ColorStar(double pos, double maxSpeed, int direction, double startHue, double hueSpeed) : base(pos, maxSpeed, direction)
    {
        _hue = startHue;
        _hueSpeed = hueSpeed;
        StarColor = CRGB.HSV2RGB(startHue);
    }

    public override void Update()
    {
        double deltaSeconds = (DateTime.UtcNow - _lastUpdate).TotalSeconds;                 //  Get the value BEFORE base class Update resets the timer

        base.Update();
        _hue += deltaSeconds * _hueSpeed * 22.5;            // Sixteen steps through the pallete, just like the palette star
        _hue %= 360;
        StarColor = CRGB.HSV2RGB(_hue);
    }
}

