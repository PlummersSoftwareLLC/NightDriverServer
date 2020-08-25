using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NightDriver;


// Palette
//
// Returns the indexed color from the main palette; interpolates N colors from the X provided

public class Palette
{
    private readonly CRGB[] colorEntries;
    private int _sizeIn;

    public int OriginalSize
    {
        get
        {
            return _sizeIn;
        }
    }
    public int FullSize
    {
        get
        {
            return colorEntries.Length; 
        }
    }

    public Palette(CRGB [] colors, int sizeOut = 256)
    {
        _sizeIn = colors.Length;
        colorEntries = new CRGB[sizeOut];

        for (int i = 0; i < sizeOut; i++)
        {
            double d        = (double)i / (sizeOut / (double) _sizeIn);
            int index       = (int)d;
            double fraction = d - (int)d;

            CRGB color1 = colors[index];
            CRGB color2 = index + 1 < colors.Length ? colors[index + 1] : colors[0];

            colorEntries[i] = color1.blendWith(color2, 1.0 - fraction);
        }
        //colorEntries[sizeOut - 1] = colors[colors.Length - 1];
    }

    public CRGB ColorFromPalette(double indexD, double brightness, bool blendColors)
    {
        indexD -= System.Math.Floor(indexD);            // Get just the fractional part

        int index = (int)(indexD * FullSize);

        if (blendColors == false)
            return colorEntries[((index / _sizeIn) * _sizeIn)];
        else
            return colorEntries[index];
    }

    static Palette _Rainbow = new Palette(CRGB.Rainbow, 1024);
    public static Palette Rainbow
    {
        get
        {
            return _Rainbow;
        }
    }

    static Palette _Rainbow2 = new Palette(CRGB.Rainbow2, 1024);
    public static Palette Rainbow2
    {
        get
        {
            return _Rainbow2;
        }
    }
}

