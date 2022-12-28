using System;
using System.Collections;
using System.Collections.Generic;
using NightDriver;

[Serializable]
public class LEDEffect 
{
    public virtual string EffectName
    {
        get
        {
            return this.GetType().Name;
        }
    }

    protected virtual void Render(ILEDGraphics graphics)
    {
        // BUGBUG class and this methoi would be abstract except for serialization requiremets // BUGBUG What?  What serialization?
        throw new ApplicationException("Render Base Class called - This is abstract");
    }
    
    public void DrawFrame(ILEDGraphics graphics)
    {
        //lock(graphics)
        {
            Render(graphics);
        }
    }

}
   