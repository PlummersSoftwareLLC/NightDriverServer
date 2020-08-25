using System;
using System.Collections;
using System.Collections.Generic;
using NightDriver;

namespace NightDriver
{
    public class CabanaYardLight : LEDEffect
    {
        // Update is called once per frame

        void FillRange(ILEDGraphics graphics, uint x, uint len, CRGB color)
        {
            for (uint i = x; i < x + len; i++)
                graphics.DrawPixel(i, 0, color);
        }
                
        protected override void Render(ILEDGraphics graphics)
        {
            CRGB color = CRGB.Incandescent;

            graphics.FillSolid(CRGB.Black);


            for (uint i = 0; i < graphics.DotCount; i += 16)
                graphics.DrawPixel(i, 0, CRGB.White.fadeToBlackBy(0.5f));

            FillRange(graphics, 0, 5 * 144 - 1, color);
            FillRange(graphics, 11*144, 5 * 144 +54, color);
            FillRange(graphics, 19*144 + 117, 2 * 144-24, color);
        }
    }
}