using System;
using NightDriver;

public class FireEffect : LEDEffect
{
    public bool   _Mirrored;                  // Should the flame be "mirrored" - drawn on both ends
    public bool   _Reversed;                  // Only applicable when not mirrored, of course, flips it to other side
    public double _Cooling = 1000f;           // Amount that each pixel fades through temp each frame
    public int    _Sparks = 1;                // Number of white-hot ignition sparks chances per frame
    public int    _Drift = 1;                 // Number of drift passes per frame
    public int    _SparkHeight = 12;          // Height of region in which sparks can be created
    public bool   _Afterburners = false;      // A visually intense, white-hot flame (vs. nice campfire)
    public uint   _Size = 0;                  // How big the drawing area is; will be repeated if strip is bigger
    public double _SparkProbability = 0.5;    // Chance of a new spark
    double[]      _Temperatures;              // Internal table of cell temperatures
    public uint   _speed;

    public Palette _Palette;

    public uint   _CellsPerLED = 1;

    public Random _rand = new Random();

    public FireEffect(uint cSize, bool bMirrored, uint cellsPerLED = 1, uint speed = 1, Palette palette = null)
    {
        _CellsPerLED = cellsPerLED;

        _Size     = cSize * _CellsPerLED;
        _Mirrored = bMirrored;
        _Palette  = palette;
        _speed    = speed;
    }

    DateTime _lastUpdate = DateTime.UtcNow;

    protected virtual CRGB ConvertHeatToColor(double temp)
    {
        if (_Palette is null)
                return CRGB.GetBlackbodyHeatColor(temp);
        temp = Math.Min(1.0, temp);
        return _Palette[temp];
    }

    protected override void Render(ILEDGraphics graphics)
    {
        for (int iSpeed = 0; iSpeed < _speed; iSpeed++)
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
                    _Temperatures[i] = _Temperatures[i] - (double)cooldown;
                }
            }

            // Heat from each cell drifts 'up' and diffuses a little

            for (int pass = 0; pass < _Drift; pass++)
                for (int k = _Temperatures.Length - 1; k >= 2; k--)
                    _Temperatures[k] = (_Temperatures[k - 1] + _Temperatures[k - 2]) / 2.0f;

            // Randomly ignite new 'sparks' near the bottom

            for (int frame = 0; frame < _Sparks; frame++)
            {
                if (_rand.NextDouble() < _SparkProbability)
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

            // Hack for weird TV layout 
            for (int j = _Temperatures.Length; j < graphics.DotCount; j++)
            {
                graphics.DrawPixel((uint)j, 0, ConvertHeatToColor(_Temperatures[j - (uint)_Temperatures.Length]));
            }

            if (_Mirrored)
            {
                uint len = _Size / _CellsPerLED;

                for (uint j = 0; j < len / 2; j += 1)
                {
                    if (_Reversed)
                    {
                        graphics.DrawPixel(j, 0, ConvertHeatToColor(_Temperatures[j * _CellsPerLED]));
                        graphics.DrawPixel((uint)(len - 1 - j), 0, ConvertHeatToColor(_Temperatures[j * _CellsPerLED]));

                    }
                    else
                    {
                        graphics.DrawPixel(((uint)(len - 1 - j)), 0, ConvertHeatToColor(_Temperatures[j * _CellsPerLED]));
                        graphics.DrawPixel(j, 0, ConvertHeatToColor(_Temperatures[j * _CellsPerLED]));
                    }
                }
            }
            else
            {
                for (uint j = 0; j < _Temperatures.Length; j += _CellsPerLED)
                {
                    uint len = _Size / _CellsPerLED;
                    uint i = j / _CellsPerLED;

                    if (_Reversed)
                        graphics.DrawPixel(i, 0, ConvertHeatToColor(_Temperatures[j]));
                    else
                        graphics.DrawPixel((uint)(len - 1 - i), 0, ConvertHeatToColor(_Temperatures[j]));
                }
            }
        }
        graphics.Blur(1);
    }
}

public class PaletteFire : FireEffect
{
    protected Palette palette;

    public PaletteFire(uint cSize, bool bMirrored, Palette pal) : base(cSize, bMirrored)
    {
        palette = pal;
    }

    protected override CRGB ConvertHeatToColor(double temp)
    {
        return palette[Math.Min(1.0, temp)];
    }
}

public class BlueFire : FireEffect
{
    public BlueFire(uint cSize, bool bMirrored) : base(cSize, bMirrored)
    {
    }

    protected override CRGB ConvertHeatToColor(double temp)
    {
        temp = Math.Min(1.0f, temp);
        byte temperature = (byte)(255 * temp);
        byte t192 = (byte)Math.Round((temperature / 255.0f) * 191);

        byte heatramp = (byte)(t192 & 0x3F);
        heatramp <<= 2;

        if (t192 > 0x80)
            return new CRGB(heatramp, 255, 255);
        else if (t192 > 0x40)
            return new CRGB(0, heatramp, 255);
        else
            return new CRGB(0, 0, heatramp);
    }
}

