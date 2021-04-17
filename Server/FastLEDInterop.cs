using System;
using System.IO;
using ZLIB;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NightDriver
{
    public static class MathExtensions
    {
        public static decimal Map(this decimal value, decimal fromSource, decimal toSource, decimal fromTarget, decimal toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }
    }

    public static class QueueExtensions
    {
        public static IEnumerable<T> DequeueChunk<T>(this ConcurrentQueue<T> queue, int chunkSize)
        {
            for (int i = 0; i < chunkSize && queue.Count > 0; i++)
            {
                T result;
                if (false == queue.TryDequeue(out result))
                    throw new Exception("Unable to Dequeue the data!");
                yield return result;
            }
        }
    }

    // LEDInterop
    //
    // Functions fdor working directly with the color data in an LED string

    public static class LEDInterop
    {
        // scale8 - Given a value i, scales it down by scale/256th 

        public static byte scale8(byte i, byte scale)
        {
            return (byte)(((ushort)i * (ushort)(scale)) >> 8);
        }

        public static byte scale8_video(byte i, byte scale)
        {
            byte j = (byte)((((int)i * (int)scale) >> 8) + ((i != 0 && scale != 0) ? 1 : 0));
            return j;
        }

        // fill_solid - fills a rnage of LEDs with a given color value

        public static void fill_solid(CRGB[] leds, CRGB color)
        {
            for (int i = 0; i < leds.Length; i++)
                leds[i] = color;
        }

        // fill_rainbow - fills a range of LEDs rotating through a hue wheel

        public static void fill_rainbow(CRGB[] leds, byte initialHue, double deltaHue)
        {
            double hue = initialHue;
            for (int i = 0; i < leds.Length; i++, hue += deltaHue)
                leds[i] = CRGB.HSV2RGB(hue % 360, 1.0, 1.0);
        }

        // GetColorBytes - get the color data as a packaged up array of bytes

        /*
        public static byte[] GetColorBytes(CRGB[] leds)
        {
            byte[] data = new byte[leds.Length * 3];
            for (int i = 0; i < leds.Length; i++)
            {
                data[i * 3]     = leds[i].r;
                data[i * 3 + 1] = leds[i].g;
                data[i * 3 + 2] = leds[i].b;
            }
            return data;
        }
        */

        // GetColorBytesAtOffset
        //
        // Reach into the main array of CRGBs and grab the color bytes for a strand

        public static byte [] GetColorBytesAtOffset(CRGB [] main, uint offset, uint length, bool bReversed = false, bool bRedGreenSwap = false)
        {
            byte[] data = new byte[length * 3];
            for (int i = 0; i < length; i++)
            {
                if (bRedGreenSwap)
                {
                    data[i * 3]     = bReversed ? main[offset + length - 1 - i].r : main[offset + i].g;
                    data[i * 3 + 1] = bReversed ? main[offset + length - 1 - i].g : main[offset + i].r;
                }
                else
                {
                    data[i * 3]     = bReversed ? main[offset + length - 1 - i].r : main[offset + i].r;
                    data[i * 3 + 1] = bReversed ? main[offset + length - 1 - i].g : main[offset + i].g;
                }
                data[i * 3 + 2] = bReversed ? main[offset + length - 1 - i].b : main[offset+i].b;
            }
            return data;
        }

        // When a strip is physically reversed, it's quicker to extract the color data in the
        // reverse order in the first place than it is to extracr and reverse it in two steps,
        // so we provide this methiod to support that

        /*
        public static byte[] GetColorBytesReversed(CRGB[] leds)
        {
            byte[] data = new byte[leds.Length * 3];
            for (int i = 0; i < leds.Length; i++)
            {
                data[i * 3]     = leds[leds.Length - 1 - i].r;
                data[i * 3 + 1] = leds[leds.Length - 1 - i].g;
                data[i * 3 + 2] = leds[leds.Length - 1 - i].b;
            }
            return data;
        }
        */
        // DWORD and WORD to Bytes - Flatten 16 and 32 bit values to memory

        public static byte[] ULONGToBytes(UInt64 input)
        {
            return new byte[8]
            {
                (byte)((input      ) & 0xff),
                (byte)((input >>  8) & 0xff),
                (byte)((input >> 16) & 0xff),
                (byte)((input >> 24) & 0xff),
                (byte)((input >> 32) & 0xff),
                (byte)((input >> 40) & 0xff),
                (byte)((input >> 48) & 0xff),
                (byte)((input >> 56) & 0xff),
            };
        }

        public static byte[] DWORDToBytes(UInt32 input)
        {
            return new byte[4]
            {
                (byte)((input      ) & 0xff),
                (byte)((input >>  8) & 0xff),
                (byte)((input >> 16) & 0xff),
                (byte)((input >> 24) & 0xff),
            };
        }

        public static byte[] WORDToBytes(UInt16 input)
        {
            return new byte[2]
            {
                (byte)((input      ) & 0xff),
                (byte)((input >>  8) & 0xff),
            };
        }

        // CombineByteArrays - Combine N arrays and returns them as one new big new one

        public static byte[] CombineByteArrays(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        // CompressMemory
        //
        // Compress a buffer using ZLIB, return the compressed version of it as a ZLIB stream

        public static byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new ZLIBStream(compressedStream, System.IO.Compression.CompressionLevel.Optimal))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        // DecompressMemory
        //
        // Expands a buffer using ZLib, returns the uncompressed version of it

        public static byte[] Decompress(byte[] input)
        {
            using (var inStream = new MemoryStream(input))
            using (var bigStream = new ZLIBStream(inStream, System.IO.Compression.CompressionMode.Decompress))
            using (var bigStreamOut = new MemoryStream())
            {
                bigStream.CopyTo(bigStreamOut);
                return bigStreamOut.ToArray();
            }
        }
    }
}

