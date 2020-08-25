using System;
using NightDriver;

public class FireEffect : LEDEffect
{
    public bool  _Mirrored;                  // Should the flame be "mirrored" - drawn on both ends
    public bool  _Reversed;                  // Only applicable when not mirrored, of course, flips it to other side
    public double _Cooling = 100f;            // Amount that each pixel fades through temp each frame
    public int   _Sparks = 2;                // Number of white-hot ignition sparks chances per frame
    public int   _Drift = 3;                 // Number of drift passes per frame
    public int   _SparkHeight = 12;          // Height of region in which sparks can be created
    public bool  _Afterburners = false;      // A visually intense, white-hot flame (vs. nice campfire)
    public uint  _Size = 0;                  // How big the drawing area is; will be repeated if strip is bigger

    double[]     _Temperatures;              // Internal table of cell temperatures

    public Random _rand = new Random();

    public FireEffect(uint cSize, bool bMirrored)
    {
        _Size = cSize;
        _Mirrored = bMirrored;
    }

    DateTime _lastUpdate = DateTime.UtcNow;

    protected override void Render(ILEDGraphics graphics)
    {
        if (_Temperatures == null)
            _Temperatures = new double[_Size];

        _lastUpdate = DateTime.UtcNow;

        // Erase the drawing area
        graphics.FillSolid(CRGB.Black);

        double cooldown = _rand.NextDouble() * _Cooling * (DateTime.UtcNow - _lastUpdate).TotalSeconds;
        _lastUpdate = DateTime.UtcNow;

        for (int i = 0; i < _Temperatures.Length; i++)
        {
            if (cooldown > _Temperatures[i])
            {
                _Temperatures[i] = 0;
            }
            else
            {
                _Temperatures[i] = _Temperatures[i] - (double) cooldown;
            }
        }

        // Heat from each cell drifts 'up' and diffuses a little

        for (int pass = 0; pass < _Drift; pass++)
            for (int k = _Temperatures.Length - 1; k >= 2; k--)
                _Temperatures[k] = (_Temperatures[k - 1] + _Temperatures[k - 2]) / 2.0f;

        // Randomly ignite new 'sparks' near the bottom

        for (int frame = 0; frame < _Sparks; frame++)
        {
            if (_rand.NextDouble() < 0.5f)
            {
                // NB: This randomly rolls over sometimes of course, and that's essential to the effect
                int y = (int)(_rand.NextDouble() * _SparkHeight);
                _Temperatures[y] = (_Temperatures[y] + _rand.NextDouble() * 0.4 + 0.06);

                if (!_Afterburners)
                    while (_Temperatures[y] > 1.0)
                        _Temperatures[y] -= 1.0f;
                else
                    _Temperatures[y] = Math.Min(_Temperatures[y], 1.0f);
            }
        }

        if (true /* Hack for weird TV layout */)
        {
            for (int j = _Temperatures.Length; j < graphics.DotCount; j++)
            {
                graphics.DrawPixel((uint) j, 0, CRGB.GetBlackbodyHeatColor(_Temperatures[j-(uint) _Temperatures.Length]));
            }
        }

        if (_Mirrored)
        {
            // Step 4.  Convert heat to LED colors
            for (uint j = 0; j < (_Temperatures.Length) / 2; j++)
            {
                graphics.DrawPixel((uint)_Temperatures.Length - 1 - j, 0, CRGB.GetBlackbodyHeatColor(_Temperatures[j]));
                graphics.DrawPixel( j, 0, CRGB.GetBlackbodyHeatColor(_Temperatures[j]));
            }
        }
        else
        {
            for (uint j = 0; j < _Temperatures.Length; j++)
                if (_Reversed)
                    graphics.DrawPixel(j, 0, CRGB.GetBlackbodyHeatColor(_Temperatures[j]));
                else
                    graphics.DrawPixel((uint)(_Temperatures.Length - 1 - j), 0, CRGB.GetBlackbodyHeatColor(_Temperatures[j]));
        }

        graphics.Blur(1);
    }
}

