using System;
using System.Collections;
using System.Collections.Generic;
using NightDriver;

public abstract class LEDEffect 
{
    protected abstract void Render(ILEDGraphics graphics);

    public void DrawFrame(ILEDGraphics graphics)
    {
        //lock(graphics)
        {
            Render(graphics);
        }
    }

}
   