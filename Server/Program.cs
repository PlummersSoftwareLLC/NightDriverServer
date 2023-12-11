//+--------------------------------------------------------------------------
//
// NightDriver - (c) 2018 Dave Plummer.  All Rights Reserved.
//
// File:        NightDriver - Exterior LED Wireless Control
//
// Description:
//
//   Draws remotely on LED strips via WiFi
//
// History:     Oct-12-2018     davepl      Created
//              Jun-06-2018     davepl      C# rewrite from C++
//
//---------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace NightDriver
{
    public class ConsoleApp
    {
        CancellationToken _token;
        public static Statistics Stats = new Statistics();

        // REVIEW - Best was I can find at the moment to conirm whether a console is present.
        //          If not, we might be under Docker, etc, so don't try to use the console

        private static bool _AlreadyFailedConsole = false;

        internal static bool SystemHasConsole
        {
            get
            {
                try
                {
                    if (_AlreadyFailedConsole)
                        return false;

                    if (Console.WindowHeight > 0 && Console.WindowWidth > 0)
                        return true;
                }
                catch (Exception)
                {
                    _AlreadyFailedConsole = true;
                    return false;
                }
                return false;
            }
        }

        // Main
        //
        // Application main loop - starts worker threads

        internal static Location [] g_AllSites =  
        { 
          new CeilingStrip   { FramesPerSecond = 30 }, 
          new ChristmasTruck      { FramesPerSecond = 45 }, 
          new Pillars()           { FramesPerSecond = 30 },
          
          new Cabana()            { FramesPerSecond = 28 },

          new Bench()             { FramesPerSecond = 28 },
          
          new TV()                { FramesPerSecond = 30 },
          new Demo()              { FramesPerSecond = 30 }, 
          new ShopCupboards()     { FramesPerSecond = 20 },  
        
          new ShopSouthWindows1() { FramesPerSecond = 2 },  
          new ShopSouthWindows2() { FramesPerSecond = 2 },  
          new ShopSouthWindows3() { FramesPerSecond = 2 },  
        };

        public static Location[] Locations
        {
            get
            {
                return g_AllSites;
            }
        }


        protected static void myCancelKeyPressHandler(object sender, ConsoleCancelEventArgs args)
        {
            Stats.WriteLine($"  Key pressed: {args.SpecialKey}");
            Stats.WriteLine($"  Cancel property: {args.Cancel}");

            // Set the Cancel property to true to prevent the process from terminating.
            Console.WriteLine("Setting the bShouldExit property to true...");
            
            args.Cancel = false;
        }

        internal void Start(CancellationToken token)
        {
            // Establish an event handler to process key press events.
            if (SystemHasConsole)
                Console.CancelKeyPress += new ConsoleCancelEventHandler(myCancelKeyPressHandler);

            DateTime lastStats = DateTime.UtcNow - TimeSpan.FromDays(1);

            foreach (var site in g_AllSites)
                site.StartWorkerThread(token);

            while (!token.IsCancellationRequested)
            {
                double d = (DateTime.UtcNow - lastStats).TotalMilliseconds;
                // Don't update the stats more than ever 100ms
                if (d >= 60)
                {
                    lastStats = DateTime.UtcNow;

                    if (SystemHasConsole)
                    {
                        Stats.Render(g_AllSites);

                        // If user presses a command key like 'c' to clear, handle it here

                        try
                        {
                            if (Console.KeyAvailable)
                            {
                                ConsoleKeyInfo cki = Console.ReadKey();
                                if (cki.KeyChar == 'c')
                                    Console.Clear();
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    Thread.Sleep(60 - (int)d);
                }
            }
            Stats.WriteLine("Exiting!");
        }
    }
}
