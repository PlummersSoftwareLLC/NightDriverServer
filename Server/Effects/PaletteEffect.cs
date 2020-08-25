using System;
using System.Collections;
using System.Collections.Generic;
using NightDriver;


public class PaletteEffect : LEDEffect
{
    private double _shiftAmount = 0;

    protected double       _startIndex;
    protected ILEDGraphics _graphics;
    protected DateTime     _lastDraw = DateTime.UtcNow;

    public Palette         _Palette             = new Palette(CRGB.Rainbow);
    public double          _LEDColorPerSecond   = 15.0;
    public double          _LEDScrollSpeed      = 0.0;
    public double          _Density             = 1.0;
    public double          _EveryNthDot         = 5.0f;
    public bool            _Blend               = true;
    public uint            _DotSize             = 2;
    public bool            _RampedColor         = false;
    public double          _Brightness = 1.0;

    public PaletteEffect(Palette palette)
    {
        _Palette            = palette;
        _startIndex         = 0;
    }

    // Update is called once per frame

    protected override void Render(ILEDGraphics graphics)
    {
        graphics.FillSolid(CRGB.Black);

        double secondsElapsed = (DateTime.UtcNow - _lastDraw).TotalSeconds;
        _lastDraw = DateTime.UtcNow;

        _shiftAmount += secondsElapsed * _LEDScrollSpeed;


        _startIndex += secondsElapsed * _LEDColorPerSecond;
        _startIndex %= _Palette.FullSize;
        double colorIndex = _startIndex;

        for (double i = _shiftAmount - _EveryNthDot; i < _shiftAmount + graphics.DotCount; i += _EveryNthDot)
        {
            CRGB c = _Palette.ColorFromPalette((double)colorIndex / _Palette.FullSize, 1.0f, _Blend);

            if (false == _RampedColor)
            {
                graphics.DrawPixels(i % graphics.DotCount, _DotSize, c.fadeToBlackBy(1.0 - _Brightness));
            }
            else
            {
                double half = _DotSize / 2;

                CRGB c1;
                for (int j = 0; j < _DotSize; j++)
                {
                    double dx = Math.Abs(half - j);
                    dx /= half;

                    c1 = c.fadeToBlackBy(dx);

                    if ((int)j == (int)half)
                        c1 = c1.blendWith(CRGB.White, 0.5);

                    graphics.DrawPixels((i + j) % graphics.DotCount, 1, c1.fadeToBlackBy(1.0 - _Brightness));
                }
            }
            colorIndex += 1 * _Density * (_Palette.FullSize / (double) _Palette.OriginalSize);
            colorIndex %= _Palette.FullSize;
        }
    }

}
