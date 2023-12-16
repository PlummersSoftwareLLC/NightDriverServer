using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NightDriver;


public class PaletteEffect : LEDEffect
{
    private double _iPixel = 0;

    protected double _iColor;
    protected ILEDGraphics _graphics;
    protected DateTime _lastDraw = DateTime.UtcNow;

    public Palette _Palette = new Palette(CRGB.Rainbow) { Blend = false };
    public double          _LEDColorPerSecond   = -50.0;
    public double          _LEDScrollSpeed      = 50.0;
    public double          _Density             = 1.0;
    public uint            _EveryNthDot         = 25;
    public uint            _DotSize             = 5;
    public bool            _RampedColor         = false;
    public double          _Brightness = 1.0;
    public bool            _Mirrored            = false;

    public PaletteEffect(Palette palette)
    {
        _Palette            = palette;
        _iColor         = 0;
    }

    // Update is called once per frame

    protected override void Render(ILEDGraphics graphics)
    {
        graphics.FillSolid(CRGB.Black);

        double secondsElapsed = (DateTime.UtcNow - _lastDraw).TotalSeconds;
        _lastDraw = DateTime.UtcNow;

        double cPixelsToScroll = secondsElapsed * _LEDScrollSpeed;
        _iPixel += cPixelsToScroll;
        _iPixel %= graphics.DotCount;

        double cColorsToScroll = secondsElapsed * _LEDColorPerSecond;
        _iColor -= (long)_iColor;

        double iColor = _iColor;

        uint cLength = (_Mirrored ? graphics.DotCount / 2 : graphics.DotCount);

        for (uint i=0; i < cLength; i += _EveryNthDot)
        {
            int count = 0;
            for (var j = 0; j < _DotSize && (i + j) < cLength; j++)
            {
                double iPixel = (i + j + _iPixel) % cLength;

                CRGB c = _Palette[iColor].fadeToBlackBy(1.0 - _Brightness);

                double cCenter = graphics.DotCount / 2.0;

                graphics.DrawPixels(iPixel + (_Mirrored ? cCenter : 0), 1, c);
                if (_Mirrored)
                    graphics.DrawPixels(cCenter - iPixel, 1, c);
                count++;

            }
            
            // Avoid pixel 0 flicker as it scrolls by copying pixel 1 onto 0
            
            if (graphics.DotCount > 1)
                graphics.DrawPixel(0, graphics.GetPixel(1));

            iColor +=  count * (_Density / graphics.PixelsPerMeter) * _EveryNthDot;
        }
    }   
}

// PresentsEffect
//
// An effect I created for three decorative boxes of 24 LEDs each, it's a simple inner palette rainbow
// but with a "sparkle" effect every so often on top of that

class PresentsEffect : LEDEffect
{
    PaletteEffect _InnerEffect = new PaletteEffect(Palette.SmoothRainbow)
        {
            _Density           = 4,
            _EveryNthDot       = 1,
            _DotSize           = 1,
            _LEDColorPerSecond = 75,
            _LEDScrollSpeed    = 0
        };

    public PresentsEffect()
    {
    }

    protected override void Render(ILEDGraphics graphics)
    {
        _InnerEffect.DrawFrame(graphics);

        // We divide by 2 so that out flashing lasts two seconds, not one.  

        if (DateTime.UtcNow.Second % 10 == 0)
        {
            var fraction = DateTime.UtcNow.Millisecond / 1000.0;
            var passes = (Math.Sin(fraction * Math.PI)) * graphics.DotCount / 5.0;
            //ConsoleApp.Stats.WriteLine(passes.ToString());
            for (double i = 0; i < passes; i += 1)
            {
                graphics.DrawPixel((uint)(Utilities.RandomDouble() * graphics.DotCount), 0, CRGB.White);
            }
        }
    }    
}

