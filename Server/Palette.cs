using System;
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
    protected readonly CRGB[] colorEntries;
    public bool Blend { get; set; } = true;

    public int OriginalSize
    {
        get
        {
            return colorEntries.Length;
        }
    }

    public virtual CRGB this[double d]
    {
        get
        {
            d -= ((long)d);                                     // Wrap around to 0-1 scale so that 3.4 -> 0.4, for example

            double fracPerColor = 1.0 / colorEntries.Length;                        // How much, on 0-1 scale, each provided color take up (8 colors -> .125)
            double indexD       = d / fracPerColor;                                 // Convert from 0-1 to 0-N (so if we have 8 colors, .275 becomes color # 2.2).
            int    index        = (int)(indexD) % colorEntries.Length;    // The integer portion, which will be the base color (color #2 in this example)
            double fraction     = indexD - index;                                 // How much of the next ordinal color to blend with (0.2 of the color to the right)
            //double fraction = indexD = Math.Floor(indexD);                          // How much of the next ordinal color to blend with (0.2 of the color to the right)
            
            // Find the first integral color

            CRGB color1 = colorEntries[index];
            if (Blend == false)
                return color1;

            // Blend the colors.  Account for wrap-around so we get a smooth transition all the way around the color wheel

            CRGB color2   = colorEntries[(index + 1) % colorEntries.Length];
            CRGB colorOut = color1.blendWith(color2, 1 - fraction);

            return colorOut;
        }
    }

    public Palette(CRGB [] colors)
    {
        colorEntries = colors;
    }

    static Palette _Rainbow = new Palette(CRGB.Rainbow);
    public static Palette Rainbow
    {   
        get
        {
            return _Rainbow;
        }
    }

    static GaussianPalette _SmoothRainbow = new GaussianPalette(CRGB.Rainbow2);
    public static GaussianPalette SmoothRainbow
    {
        get
        {
            return _SmoothRainbow;
        }
    }

    static GaussianPalette _SteppedRainbow = new GaussianPalette(CRGB.Rainbow2) { Blend = false };
    public static GaussianPalette SteppedRainbow
    {
        get
        {
            return _SteppedRainbow;
        }
    }
}

// GaussianPalette
//
// Goes one step further by blending with up to 5 colors using a 1D Gassian weighted kernel

public class GaussianPalette : Palette
{
    protected double _Smoothing = 0.0;
    public double [] _Factors = new double[] { 0.06136, 0.24477, 0.38774, 0.24477, 0.06136 };

    public GaussianPalette(CRGB[] colors) : base(colors)
    {
        _Smoothing = 1.0 / colors.Length;
    }

    public override CRGB this[double d]
    {
        get
        {
            double s = _Smoothing / OriginalSize;

            double red   = base[d - s * 2].r * _Factors[0] +
                           base[d - s    ].r * _Factors[1] +
                           base[d        ].r * _Factors[2] +
                           base[d + s    ].r * _Factors[3] +
                           base[d + s * 2].r * _Factors[4];

            double green = base[d - s * 2].g * _Factors[0] +
                           base[d - s    ].g * _Factors[1] +
                           base[d        ].g * _Factors[2] +
                           base[d + s    ].g * _Factors[3] +
                           base[d + s * 2].g * _Factors[4];

            double blue  = base[d - s * 2].b * _Factors[0] +
                           base[d - s    ].b * _Factors[1] +
                           base[d        ].b * _Factors[2] +
                           base[d + s    ].b * _Factors[3] +
                           base[d + s * 2].b * _Factors[4];

            return new CRGB((byte)red, (byte)green, (byte)blue);
        }
    }
}

