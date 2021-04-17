using System;
using System.Collections;
using System.Collections.Generic;
using NightDriver;


public class PaletteEffect : LEDEffect
{
    private double _iPixel = 0;

    protected double       _iColor;
    protected ILEDGraphics _graphics;
    protected DateTime     _lastDraw = DateTime.UtcNow;

    public Palette         _Palette             = new Palette(CRGB.Rainbow);
    public double          _LEDColorPerSecond   = 1.0;
    public double          _LEDScrollSpeed      = 0.0;
    public double          _Density             = 1.0;
    public double          _EveryNthDot         = 5.0f;
    public uint            _DotSize             = 2;
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
        _iColor += cColorsToScroll / _Palette.OriginalSize;
        _iColor -= (long)_iColor;

        double iColor = _iColor;

        uint cLength = (_Mirrored ? graphics.DotCount / 2 : graphics.DotCount);

        for (double i = 0; i < cLength; i += _EveryNthDot)
        {
            int count = 0;
            for (uint j = 0; j < _DotSize && (i + j) < cLength; j++)
            {
                double iPixel = (i + j + _iPixel) % cLength;

                CRGB c = _Palette[iColor].fadeToBlackBy(1.0 - _Brightness);

                double cCenter = graphics.DotCount / 2.0;

                graphics.DrawPixels(iPixel + (_Mirrored ? cCenter : 0), 1, c);
                if (_Mirrored)
                    graphics.DrawPixels(cCenter - iPixel, 1, c);
                count++;

            }
            iColor += count * _Density / _Palette.OriginalSize;

        }
    }
    
}
