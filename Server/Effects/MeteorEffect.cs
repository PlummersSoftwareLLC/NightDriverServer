using System;
using System.Collections;
using System.Collections.Generic;
using NightDriver;

public class Meteor
{
    public double  _hue;
    public double  _pos;
    public bool    _bGoingLeft;
    public double  _speed;
    public double  _meteorSize;
    public bool    _bBounce;

    public void Reverse()
    {
        _bGoingLeft = !_bGoingLeft;
    }

    public Meteor(double hue, double pos, bool bGoingLeft, bool bBounce, double speed, double size)
    {
        _hue = hue;
        _pos = pos;
        _bGoingLeft = bGoingLeft;
        _speed = speed;
        _meteorSize = size;
    }

    public void Draw(ILEDGraphics graphics)
    {

        _pos = (_bGoingLeft) ? _pos - _speed : _pos + _speed;
        if (_pos < 0)
            _pos += graphics.DotCount;
        if (_pos >= graphics.DotCount)
            _pos -= graphics.DotCount;

        if (_bBounce)
        {
            if (_pos < _meteorSize)
            {
                _bGoingLeft = false;
                _pos = _meteorSize;
            }
            if (_pos >= graphics.DotCount)
            {
                _bGoingLeft = true;
                _pos = graphics.DotCount - 1;
            }
        }

        for (double j = 0; j < _meteorSize; j++)                    // Draw the meteor head
        {
            double x = (_pos - j);
            if (x < graphics.DotCount && x >= 0)
            {
                _hue = _hue + 0.50f;
                _hue %= 360;
                CRGB color = CRGB.HSV2RGB(_hue, 1.0, 0.8);
                graphics.DrawPixels(x, 1, color);
            }
        }
    }
}

// A meteor that zooms through the strip 
public class MeteorEffect : LEDEffect
{
    protected ILEDGraphics _graphics;
    protected int          _meteorCount;
    protected double       _meteorTrailDecay;
    protected double       _meteorSpeedMin;
    protected double       _meteorSpeedMax;
    protected Meteor []    _Meteors;
    protected double       _hueval;

    protected bool         _bFirstDraw = true;

    public MeteorEffect(int    dotCount, 
                        bool   bBounce = true, 
                        int    meteorCount = 4, 
                        int    meteorSize = 4, 
                        double trailDecay = 1.0, 
                        double minSpeed = 0.5, 
                        double maxSpeed = 0.5)
    {
        meteorCount += 1;
        meteorCount /= 2;           // Force an even number of meteors
        meteorCount *= 2;

        _hueval             = 0;
        _meteorCount        = meteorCount;
        _meteorTrailDecay   = trailDecay;
        _meteorSpeedMin     = minSpeed;
        _meteorSpeedMax     = maxSpeed;

        _Meteors = new Meteor[meteorCount];
        int halfCount = meteorCount / 2;

        for (int i = 0; i < halfCount; i++)
        {
            _hueval += 20;
            _hueval %= 360;

            _Meteors[i] = new Meteor(_hueval,
                                     i * dotCount / halfCount,  
                                     true, 
                                     bBounce,
                                     Utilities.RandomDouble(_meteorSpeedMin, _meteorSpeedMax),
                                     meteorSize);
            int j = halfCount + i;
            _Meteors[j] = new Meteor(_hueval,
                                     i * dotCount / halfCount,
                                     false,
                                     bBounce,
                                     Utilities.RandomDouble(_meteorSpeedMin, _meteorSpeedMax),
                                     meteorSize);

        }                
    }

    // Update is called once per frame

    protected override void Render(ILEDGraphics graphics)
    {
        if (_bFirstDraw)
        {
            graphics.FillSolid(CRGB.Black);
            _bFirstDraw = false;
        }

        for (uint j = 0; j < graphics.DotCount; j++)
        {
            if ((_meteorTrailDecay == 0) || (Utilities.RandomByte() > 64))
            {
                CRGB c = graphics.GetPixel(j, 0);
                //c.fadeToBlackBy(_meteorTrailDecay);
                c *= _meteorTrailDecay;
                graphics.DrawPixel(j, 0, c);
            }
        }

        for (int i = 0; i < _meteorCount; i++)
            if (null != _Meteors[i])
                _Meteors[i].Draw(graphics);
    }
    
}
