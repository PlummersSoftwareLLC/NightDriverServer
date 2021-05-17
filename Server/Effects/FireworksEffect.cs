//+--------------------------------------------------------------------------
//
// NightDriver - (c) 2018 Dave Plummer
//
// File:        FireworksEffect.cs - Draw's little fireworks particle system
//
// Description:
//
//   Part of NightDriver
//
// History:     Oct-12-2018     davepl      Created
//              Jun-06-2018     davepl      C# rewrite from C++
//
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NightDriver;

// Fireworks Effect for Fourth of July

public class FireworksEffect : LEDEffect
{
    // Each particle int the particle system remembers its color, 
    // birth time, postion, velocity, etc.

    public class Particle
    {
        public CRGB _starColor;
        public DateTime _birthTime;
        public DateTime _lastUpdate;
        public double _velocity;
        public double _position;

        public Random _rand = new Random();

        public Particle(CRGB starColor, double pos, double maxSpeed)
        {
            _position = pos;
            _velocity = _rand.NextDouble() * maxSpeed * 2 - maxSpeed;
            _starColor = starColor;
            _birthTime = DateTime.UtcNow;
            _lastUpdate = DateTime.UtcNow;
        }

        public double Age
        {
            get
            {
                return (DateTime.UtcNow - _birthTime).TotalSeconds;
            }
        }

        // As the particle ages we actively fade its color and slow its speed

        public void Update()
        {
            double deltaTime = (DateTime.UtcNow - _lastUpdate).TotalSeconds;
            _position += _velocity * deltaTime;
            _lastUpdate = DateTime.UtcNow;

            _velocity -= (2 * _velocity * deltaTime);

            _starColor = _starColor.fadeToBlackBy((double)_rand.NextDouble() * 0.1f);
        }
    }

    // A Palette256 is just a way of picking a random color from the rainbow.  If you can provide
    // a random color, you're set... this is just the easiest mechanism I had handy.

    protected Palette _Palette = new Palette(CRGB.Rainbow);

    public bool       Blend                     = true;
    public double     MaxSpeed                  = 375.0;        // Max velocity in pixels per second
    public double     NewParticleProbability    = 1.0;         // Odds of a new particle each pass
    public double     ParticlePreignitonTime    = 0.0;          // How long to "wink" into existence
    public double     ParticleIgnition          = 0.2;          // How long to "flash" white at birth
    public double     ParticleHoldTime          = 0.00;         // Main lifecycle time
    public double     ParticleFadeTime          = 2.0;          // Fade out time
    public double     ParticleSize              = 1.00;         // Size of the particle (0 is min)
	
    protected Queue<Particle> _Particles = new Queue<Particle>();   // Keeps track of particles in FIFO

    private Random _random = new Random();

    // All drawing is done in Render, which produces one frame by calling the draw methods on the supplied
    // graphics interface.  As long as you support "Draw a pixel" you should be able to make it work with
    // whatever your're using...

    protected override void Render(ILEDGraphics graphics)
    {
        // Randomly create some new stars this frame; the number we create is tied to the size of the display
        // so that the display size can change and the "effect density" will stay the same

        for (int iPass = 0; iPass < Math.Max(5, graphics.DotCount / 50); iPass++)
        {
            if (_random.NextDouble() < NewParticleProbability * 0.005)
            {
                // Pick a random color and location.  If you don't have FastLED palettes, all you need to do
                // here is generate a random color.

                uint iStartPos = (uint)(_random.NextDouble() * graphics.DotCount);
                CRGB color = CRGB.RandomSaturatedColor; // _Palette.ColorFromPalette(_random.Next(0, _Palette.FullSize), 1.0f, false);
                int c = _random.Next(10, 50);
                double multiplier = _random.NextDouble() * 3;

                for (int i = 1; i < c; i++)
                {
                    Particle particle = new Particle(color, iStartPos, MaxSpeed * _random.NextDouble() * multiplier);
                    _Particles.Enqueue(particle);
                }
            }
        }

        // In the degenerate case of particles not aging out for some reason, we need to set a pseudo-realistic upper
        // bound, and the very number of possible pixels seems like a reasonable one

        while (_Particles.Count > graphics.DotCount * 5)
            _Particles.Dequeue();

        // Start out with an empty canvas

        graphics.FillSolid(CRGB.Black);

        foreach (Particle star in _Particles)
        {
            star.Update();

            CRGB c = new CRGB(star._starColor);

            // If the star is brand new, it flashes white briefly.  Otherwise it fades over time.

            double fade = 0.0f;
            if (star.Age > ParticlePreignitonTime && star.Age < ParticleIgnition + ParticlePreignitonTime)
            {
                c = CRGB.White;
            }
            else
            {
                // Figure out how much to fade and shrink the star based on its age relative to its lifetime

                double age = star.Age;
                if (age < ParticlePreignitonTime)
                    fade = 1.0 - (age / ParticlePreignitonTime);
                else
                {
                    age -= ParticlePreignitonTime;

                    if (age < ParticleHoldTime + ParticleIgnition)
                        fade = 0.0f;                                                // Just born
                    else if (age > ParticleHoldTime + ParticleIgnition + ParticleFadeTime)
                        fade = 1.0f;                                                // Black hole, all faded out
                    else
                    {
                        age -= (ParticleHoldTime + ParticleIgnition);
                        fade = (age / ParticleFadeTime);                              // Fading star
                    }
                }
                c = c.fadeToBlackBy((double)fade);
            }
            ParticleSize = (1 - fade) * (graphics.DotCount / 500.0);
            ParticleSize = Math.Max(1, ParticleSize);
            graphics.DrawPixels(star._position, ParticleSize, c);

        }

        // Remove any particles who have completed their lifespan

        while (_Particles.Count > 0 && _Particles.Peek().Age > ParticleHoldTime + ParticleIgnition + ParticleFadeTime)
            _Particles.Dequeue();
    }
}

