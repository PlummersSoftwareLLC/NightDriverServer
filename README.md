# NightDriverServer

## Purpose

The purpose of the NightDriverServer project is to serve as a "demo" of how to send color data over WiFi to a NightDriverStrip instance on an ESP32.  You must have the equivalent of the "LEDSTRIP" proejct running with wifi and incoming data enabled (which LEDSTRIP does by default).  When run, the ESP32 will connect to WiFi and wait for TCP connections on port 49152.

While this demo is in C#, any lanugage that can create a byte array and send it to a socket will work.  I've done examples in C++, Python, and C# in the past.

## Packet Format

The format for a packet of color data, a single frame, is:

The function LightStrip::GetDataFrame is the one that puts together the packet and returns a byte array to be sent to the socket.

```csharp
    var data = GetPixelData(MainLEDs);
    return
        LEDInterop.CombineByteArrays(LEDInterop.WORDToBytes(WIFI_COMMAND_PIXELDATA64), 
            LEDInterop.WORDToBytes((UInt16)Channel), // LED channel on ESP32
            LEDInterop.DWORDToBytes((UInt32)data.Length / 3), // Number of LEDs
            LEDInterop.ULONGToBytes(seconds), // Timestamp seconds (64 bit)
            LEDInterop.ULONGToBytes(uSeconds), // Timestmap microseconds (64 bit)
            data); // Color Data
```

The packet format, then, starts with a WORD magic cookie of WIFI_COMMAND_PIXELDATA64, which is 3.  
The next WORD is the LED channel (or 0 for 'all channels'.)
The next DWORD is the number of LEDs being set
The next ULONG (64 bits) is the timestamp (whole seconds)
The next ULONG (64 bits) is the timestamp (microseconds)
Then the raw color data in RGB format (one 3-byte RGB triplet per LED being sent)

This demo sets the timestamp to be a second or two in the future, depending on how much RAM the NightDriverStrip has, so that each strip can hold its data until the timestamp comes due and then they all show it at the same time, which is how multiple strips stay in sync.

## Compressed Packets

This data may optionally be compressed.  In the function CompressFame, you can see where the above packet is wrapped in a compression packet. The format of a compressed packet is:

First DWORD is the header tag:  0x44415645  (just a magic value)
Second DWORD is compressed data length in bytes
Third DWORD is the original data length in bytes
Fourth DWORD is unused, set to 0x12345678 for now
Then the bytes of the original color data packet in LZCompressed format

You can see how the original packet is compressed in the Compress function, which uses a ZLIBStream to make the lzcompressed data in a manner that can be decompressed by the ESP32 that is receiving the data.

## How the Server Works

The program creates an array of Location objects.  A Location implements the GraphicsBase interface so you can call GetPixel and SetPixel and so on.

The main function here is DrawAndEnqueueAll, which figures out which effect is currently running.  It then calls that effect's DrawFrame call which in turn will draw into the Location.  Once the drawing is complete it calls CompressAndEnqueueData on the controller object, which will put the data into a queue, one per location.

Each Location has a peer thread running an LEDControllerChannel, and it is that controller that talks directly to the socket on the strip, sending a packer every frame.

## How to Cheat and Get Started

None of this complexity is needed uness you have many locations.  If you were just sending to a single strip, you could simply write a loop that sent frames to the strip 20 times per second.  If you set the timestamps to 0, it should draw that frame immediately instead of buffering it.  That's the quickest way to get something running in a new lanugage or project.
