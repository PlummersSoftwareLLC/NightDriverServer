using System;

namespace NightDriver
{
    public class TimeOfNightEffect : LEDEffect
    {
        int     zone = -8;              // Seattle time Zone
        double  latitude = 47.6;        // Seattle lat
        double  longitude = -122.32;    // Seattle lon 
        bool    dst = true;             // Day Light Savings 

        public TimeOfNightEffect()
        {
        }

        DateTime _lastPrint = DateTime.Now;

        protected override void Render(ILEDGraphics graphics)
        {
            var julianDate = SunriseSunset.calcJD(DateTime.Today);
            double sunRiseUTC = SunriseSunset.calcSunRiseUTC(julianDate, latitude, longitude);
            double sunSetUTC = SunriseSunset.calcSunSetUTC(julianDate, latitude, longitude);
            DateTime? sunRiseN = SunriseSunset.getDateTime(sunRiseUTC, zone, DateTime.Today, dst);
            DateTime? sunSetN = SunriseSunset.getDateTime(sunSetUTC, zone, DateTime.Today, dst);

            if (!sunRiseN.HasValue || !sunSetN.HasValue)
            {
                ConsoleApp.Stats.WriteLine("NO DATA calculated in TimeOfNightEffect");
                return;
            }

            DateTime sunRise = sunRiseN.Value;
            DateTime sunSet = sunSetN.Value;

            double secondsOfDay = (sunSet - sunRise).TotalSeconds;
            double secondsOfNight = (sunRise + TimeSpan.FromDays(1) - sunSet).TotalSeconds;

            bool   bIsDark;
            double fractionOfPeriod;

            if (DateTime.Now > sunSet)
            {
                // Nighttime - after sunset
                bIsDark = true;
                fractionOfPeriod = (DateTime.Now - sunSet).TotalSeconds / secondsOfNight;
            }
            else if (DateTime.Now < sunRise)
            {
                // Nighttime - before sunrise
                bIsDark = true;
                fractionOfPeriod = (DateTime.Now - (sunSet - TimeSpan.FromDays(1))).TotalSeconds / secondsOfNight;
            }
            else
            {
                // (DateTime.Now  sunSet)
                bIsDark = false;
                fractionOfPeriod = (DateTime.Now - sunRise).TotalSeconds / secondsOfDay;
            }

            if (DateTime.Now - _lastPrint > TimeSpan.FromSeconds(1))
            {
                _lastPrint = DateTime.Now;
                String s = String.Format("Dark: {0}, Fraction: {1}", bIsDark, fractionOfPeriod);
                ConsoleApp.Stats.WriteLine(s);
            }
            //Conso1eApp.Stats.WriteLine("Render"); 
            graphics.FillSolid(CRGB.Black);
        }       
    }
}
