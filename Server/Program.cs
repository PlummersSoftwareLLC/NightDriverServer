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
using System.Runtime.InteropServices;
using System.Threading;


namespace NightDriver
{

    internal class ConsoleApp
    {
        private static bool bShouldExit = false;
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
          new Cabana()            { FramesPerSecond = 22 },  // Should be max of 22 given the 8*144
          new Bench()             { FramesPerSecond = 30 },  // Runs flame effect, so looks better at 30   
          //new TV()                { FramesPerSecond = 22 },  // Runs flame effect, so looks better at 30   
          new Tree()              { FramesPerSecond = 24 },  // Runs CharlieBrownTree, looks better at 30
          new Mirror()            { FramesPerSecond = 30 },  // Runs CharlieBrownTree, looks better at 30
          new ShopCupboards()     { FramesPerSecond = 24 },  
          new ShopEastWindows()   { FramesPerSecond = 24 },  
          //new ShopSouthWindows { }
          new ShopSouthWindows1() { FramesPerSecond = 24 },  
          new ShopSouthWindows2() { FramesPerSecond = 24 },  
          new ShopSouthWindows3() { FramesPerSecond = 24 },  
        };

        protected static void myCancelKeyPressHandler(object sender, ConsoleCancelEventArgs args)
        {
            Stats.WriteLine($"  Key pressed: {args.SpecialKey}");
            Stats.WriteLine($"  Cancel property: {args.Cancel}");

            // Set the Cancel property to true to prevent the process from terminating.
            Console.WriteLine("Setting the bShouldExit property to true...");
            bShouldExit = true;
            args.Cancel = false;
        }

        internal static void Start()
        {
            // Establish an event handler to process key press events.
            if (SystemHasConsole)
                Console.CancelKeyPress += new ConsoleCancelEventHandler(myCancelKeyPressHandler);

            DateTime lastStats = DateTime.UtcNow - TimeSpan.FromDays(1);

            foreach (var site in g_AllSites)
                site.StartWorkerThread();

            while (!bShouldExit)
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

                        if (Console.KeyAvailable)
                        {
                            ConsoleKeyInfo cki = Console.ReadKey();
                            if (cki.KeyChar == 'c')
                                Console.Clear();
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
