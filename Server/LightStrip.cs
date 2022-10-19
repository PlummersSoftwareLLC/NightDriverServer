using System;
using System.Threading;
using NightDriver;

namespace NightDriver
{
    public class LightStrip : LEDControllerChannel
    {
        public uint FramesPerBuffer  = 26;              // How many buffer frames the chips have
        
        public const double PercentBufferUse = 0.85;            // How much of the buffer we should use up

        // The only attribute that a light strip adds is that it can be reversed, as you
        // could hand it from either end

        public bool Reversed { get; set; } = false;

        public double TimeOffset
        {
            get
            {
                // return 0.65;

                if (0 == Location.FramesPerSecond)                  // No speed indication yet, can't guess at offset, assume 1 second for now
                    return 1.0;

                if (!Supports64BitClock)                            // Old V001 flash is locked at 22 fps
                {
                    return 1.0;
                }
                else
                {
                    double offset =  (FramesPerBuffer * PercentBufferUse) / Location.FramesPerSecond;
                    return offset;
                }
            }
        }

        public LightStrip(string hostName, string friendlyName, bool compressData, uint width, uint height = 1, uint offset = 0, bool reversed = false, byte channel = 0, bool swapRedGreen = false)
        : base(hostName, friendlyName, width, height, offset, compressData, channel, 0, swapRedGreen)
        {
            Reversed = reversed;
        }

        private byte[] GetPixelData(CRGB [] LEDs)
        {
            return LEDInterop.GetColorBytesAtOffset(LEDs, Offset, Width * Height, Reversed, RedGreenSwap);
        }

        const UInt16 WIFI_COMMAND_PIXELDATA   = 0;
        const UInt16 WIFI_COMMAND_VU          = 1;
        const UInt16 WIFI_COMMAND_CLOCK       = 2;
        const UInt16 WIFI_COMMAND_PIXELDATA64 = 3;

        protected override byte[] GetDataFrame(CRGB [] MainLEDs, DateTime timeStart)
        {
            // The old original code truncated 64 bit values down to 32, and we need to fix that, so it's a in a packet called PIXELDATA64
            // and is only sent to newer flashes taht support it.  Otherwise we send the old original foramt.

            if (false == Supports64BitClock)
            {
                // The timeOffset is how far in the future frames are generated for.  If the chips have a 2 second buffer, you could
                // go up to 2 seconds, but I shoot for the middle of the buffer depth.  Right now it's calculated as using 


                double epoch = (timeStart.Ticks - DateTime.UnixEpoch.Ticks + (TimeOffset * TimeSpan.TicksPerSecond)) / (double)TimeSpan.TicksPerSecond;

                ulong seconds = (ulong)epoch;                                       // Whole part of time number (left of the decimal point)
                ulong uSeconds = (ulong)((epoch - (int)epoch) * 1000000);           // Fractional part of time (right of the decimal point)

                var data = GetPixelData(MainLEDs);
                return LEDInterop.CombineByteArrays(LEDInterop.WORDToBytes(WIFI_COMMAND_PIXELDATA),        // Offset, always zero for us
                                                    LEDInterop.WORDToBytes((UInt16)Channel),               // LED channel on ESP32
                                                    LEDInterop.DWORDToBytes((UInt32)data.Length / 3),      // Number of LEDs
                                                    LEDInterop.DWORDToBytes((UInt32)seconds),              // Timestamp seconds (32 bit truncation)
                                                    LEDInterop.DWORDToBytes((UInt32)uSeconds),             // Timestmap microseconds (32 bit truncation)
                                                    data);                                                 // Color Data
            }
            else
            {
                // The timeOffset is how far in the future frames are generated for.  If the chips have a 2 second buffer, you could
                // go up to 2 seconds, but I shoot for the middle of the buffer depth.  Right now it's calculated as using 

                double epoch = (timeStart.Ticks - DateTime.UnixEpoch.Ticks + (TimeOffset * TimeSpan.TicksPerSecond)) / (double)TimeSpan.TicksPerSecond;
                
                // If the strip clock is within one minute of our clock, adjust the packet time by the amount that we
                // differ.  This will cause more of the buffer to be used, and will help prevent cases where the buffer
                // gets stale because the clock is behind.

                if (Math.Abs(Response.currentClock - epoch) < 60.0)
                {
                    //epoch += (epoch - Response.currentClock) * 0.5;
                }

                ulong seconds = (ulong)epoch;                                       // Whole part of time number (left of the decimal point)
                ulong uSeconds = (ulong)((epoch - (int)epoch) * 1000000);           // Fractional part of time (right of the decimal point)

                var data = GetPixelData(MainLEDs);
                return LEDInterop.CombineByteArrays(LEDInterop.WORDToBytes(WIFI_COMMAND_PIXELDATA64),      // Offset, always zero for us
                                                    LEDInterop.WORDToBytes((UInt16)Channel),               // LED channel on ESP32
                                                    LEDInterop.DWORDToBytes((UInt32)data.Length / 3),      // Number of LEDs
                                                    LEDInterop.ULONGToBytes(seconds),                      // Timestamp seconds (64 bit)
                                                    LEDInterop.ULONGToBytes(uSeconds),                     // Timestmap microseconds (64 bit)
                                                    data);                                                 // Color Data

            }
        }
    };
}

