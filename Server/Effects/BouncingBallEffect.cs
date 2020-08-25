using System;
using System.Collections;
using System.Collections.Generic;
using NightDriver;

public class BouncingBallEffect : LEDEffect
{
    public bool  Reversed;
    public double Gravity = -9.81;
    public double StartHeight = 1.0;
    public uint  BallSize = 1;

    double ImpactVelocityStart;
    public int BallCount = 3;

    double[] ClockTimeSinceLastBounce;
    double[] TimeSinceLastBounce;
    double[] Height;
    double[] ImpactVelocity;
    double[] Dampening;

    static readonly CRGB[] Colors = { CRGB.Red, CRGB.Blue, CRGB.Green, CRGB.Orange, CRGB.Cyan, CRGB.Yellow, CRGB.Purple };

    // Start is called before the first frame update
    public BouncingBallEffect()
    {
        ImpactVelocityStart = Math.Sqrt(-2 * Gravity * StartHeight);

        ClockTimeSinceLastBounce = new double[BallCount];
        TimeSinceLastBounce      = new double[BallCount];
        Height                   = new double[BallCount];
        ImpactVelocity           = new double[BallCount];
        Dampening                = new double[BallCount];

        for (int i = 0; i < BallCount; i++)
        {
            Height[i]                   = StartHeight;
            ImpactVelocity[i]           = ImpactVelocityStart;
            ClockTimeSinceLastBounce[i] = DateTime.UtcNow.Ticks / (double) TimeSpan.TicksPerSecond;
            Dampening[i]                = 1.00f - i / Math.Pow(BallCount, 2);
            TimeSinceLastBounce[i]      = 0.0f;
        }
    }

    protected override void Render(ILEDGraphics graphics)
    {
        graphics.FillSolid(CRGB.Black);

        for (uint i = 0; i < BallCount; i++)
        {
            TimeSinceLastBounce[i] = (DateTime.UtcNow.Ticks / (double) TimeSpan.TicksPerSecond - ClockTimeSinceLastBounce[i]) / 5;
            Height[i] = 0.5f * Gravity * Math.Pow(TimeSinceLastBounce[i], 2.0f) + ImpactVelocity[i] * TimeSinceLastBounce[i];

            if (Height[i] < 0)
            {
                Height[i] = 0;
                ImpactVelocity[i] = Dampening[i] * ImpactVelocity[i];
                ClockTimeSinceLastBounce[i] = DateTime.UtcNow.Ticks / (double) TimeSpan.TicksPerSecond;

                if (ImpactVelocity[i] < 0.1f)
                    ImpactVelocity[i] = ImpactVelocityStart;
            }

            double position = Height[i] * (graphics.DotCount - 1) / StartHeight;

            if (Reversed)
                graphics.DrawPixels((uint)(graphics.DotCount - 1 - position), BallSize, Colors[i]);
            else
                graphics.DrawPixels((uint)(position), BallSize, Colors[i]);
        }
    }
}