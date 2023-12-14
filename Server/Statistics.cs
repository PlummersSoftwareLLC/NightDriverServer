        //+--------------------------------------------------------------------------
//  NightDriver - (c) 2019 Dave Plummer.  All Rights Reserved.
//
//  File:        Statistics.cs
//
//  Description:
//
//
//
//  History:     6/29/2019         Davepl      Created
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NightDriver
{
    public class Statistics
    {
        const uint cMaxLogLines = 1000;

        public uint SpareMilisecondsPerFrame = 0;

        protected List<Tuple<string, DateTime>> textLines = new List<Tuple<string, DateTime>>();

        static void moveto_xy(int x, int y)
        {
            if (x >= 0 && x < Console.WindowWidth && y >= 0 && y < Console.WindowHeight)
                Console.SetCursorPosition(x, y);
        }

        static void printf_xy(int x, int y, string format, params object[] args)
        {
            moveto_xy(x, y);
            Console.Write(string.Format(format, args));
            ParkCursor();
        }

        public Statistics()
        {
            if (ConsoleApp.SystemHasConsole)
            {
                Console.Clear();
                Console.CursorVisible = false;
                ParkCursor();
            }
        }

        const int ColumnWidth = 16;
        const int ColumnHeight = 17;
        const int BufferTop = 36;
        const int topMargin = 3;
        const int bottomMargin = 0;

        // ParkCursor
        //
        // Leave the cursor at a known x,y position in case any stray output comes out of somewhere

        public static void ParkCursor()
        {
            Console.SetCursorPosition(2, BufferTop + topMargin);
        }

        // When someone wants to write raw text, we queue it into a queue and print it at the end

        public void WriteLine(string text)
        {
            lock (textLines)
            {
                textLines.Add(new Tuple<string, DateTime>(text, DateTime.Now));
                while (textLines.Count > cMaxLogLines)
                    textLines.RemoveAt(0);
            }
        }

        public String Text
        {
            get
            {
                lock (textLines)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (var line in textLines)
                        stringBuilder.Append(line.Item2.ToString() + " " + line.Item1.ToString() + "\r\n");
                    return stringBuilder.ToString();
                }                       
            }
        }


        string Spaces => string.Concat(Enumerable.Repeat(" ", Console.WindowWidth));
        string Dashes => string.Concat(Enumerable.Repeat("-", Console.WindowWidth));
        static string Filled = string.Concat(Enumerable.Repeat("█", 132));
        static string Empty  = string.Concat(Enumerable.Repeat("░", 132));

        public static string Bar(double ratio, int maxLen)
        {
            ratio = Math.Clamp(ratio, 0, 1);
            return (ratio > 0 ? Filled.Substring(0, (int)(ratio * maxLen)) : "") + Empty.Substring(0, Math.Max(0, maxLen - (int)(ratio * maxLen)));
        }

        public void Render(Location[] sites)
        {
            // Detect no console and return without trying to render anything

            if (Console.WindowWidth == 0 || Console.WindowHeight == 0)
                return;

            var allControllers = sites.SelectMany(item => item.LightStrips).Distinct().ToList();
            printf_xy(27, 0, "NIGHTDRIVER LED CONTROL");
            printf_xy(5,  1, "Version 2020.8.17 - Time: {0:F2} - {1}", Utilities.UnixSeconds(), DateTime.Now);
            printf_xy(0, 2, Dashes);

            printf_xy(0, 0 + topMargin, "Slot");
            printf_xy(0, 1 + topMargin, "Name");
            printf_xy(0, 2 + topMargin, "Host");
            printf_xy(0, 3 + topMargin, "Socket");
            printf_xy(0, 4 + topMargin, "WiFi");
            printf_xy(0, 5 + topMargin, "Status");
            printf_xy(0, 6 + topMargin, "Bytes/Sec");
            printf_xy(0, 7 + topMargin, "Clock");
            printf_xy(0, 8 + topMargin, "Buffer");
            printf_xy(0, 9 + topMargin, "Pwr/Brite");
            printf_xy(0,10 + topMargin, "gFPS");
            printf_xy(0,11 + topMargin, "Connects");
            printf_xy(0,12 + topMargin, "Queue Depth");
            printf_xy(0,13 + topMargin, "Offset [FPS]");
            printf_xy(0,14 + topMargin, "Effect Mode");

            uint totalBytes = 0;
            int x, y = 0;

            double epoch = (DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks) / (double)TimeSpan.TicksPerSecond;

            for (int i = 0; i < allControllers.Count; i++)
            {
                var myController = LEDControllerChannel.ControllerSocketForHost(allControllers[i].HostName);
                int slot = i + 1;
                x = (slot * ColumnWidth) % Console.WindowWidth;
                y = (slot * ColumnWidth) / Console.WindowWidth * ColumnHeight;

                var ver = allControllers[slot - 1].Response.flashVersion > 0 ?
                          "v" + allControllers[slot - 1].Response.flashVersion.ToString() + "  " : "---";

                Console.ForegroundColor = ConsoleColor.Gray;
                printf_xy(x, y + topMargin, slot.ToString() + " " + ver);
                Console.ForegroundColor = ConsoleColor.White;
                printf_xy(x, y + 1 + topMargin, allControllers[slot - 1].FriendlyName);
                Console.ForegroundColor = ConsoleColor.Gray;
                printf_xy(x, y + 2 + topMargin, allControllers[slot - 1].HostName);
                Console.ForegroundColor = allControllers[slot - 1].HasSocket ? ConsoleColor.Gray : ConsoleColor.Yellow;
                printf_xy(x, y + 3 + topMargin, allControllers[slot - 1].HasSocket ? "Open  " : "Closed");
                printf_xy(x, y + 4 + topMargin, allControllers[slot - 1].Response.wifiSignal.ToString());
                Console.ForegroundColor = allControllers[slot - 1].ReadyForData ? ConsoleColor.Green : ConsoleColor.Red;
                printf_xy(x, y + 5 + topMargin, allControllers[slot - 1].ReadyForData ? "Ready    " : "Not Ready");
                Console.ForegroundColor = ConsoleColor.Gray;
                printf_xy(x, y + 6 + topMargin, Spaces.Substring(0, ColumnWidth));
                var bps = allControllers[slot - 1].ReadyForData ? allControllers[slot - 1].BytesPerSecond.ToString() : "----";
                printf_xy(x, y + 6 + topMargin, bps);
                totalBytes += allControllers[slot - 1].BytesPerSecond;

                var clock = allControllers[slot - 1].Response.currentClock > 8 ? (allControllers[slot - 1].Response.currentClock - epoch).ToString("F2") : "UNSET";
                printf_xy(x, y + 7 + topMargin, Spaces.Substring(0, ColumnWidth));
                printf_xy(x, y + 7 + topMargin, clock);
                //printf_xy(x, y + 8 + topMargin, allControllers[slot - 1].Response.bufferPos.ToString()+"/"+ allControllers[slot - 1].Response.bufferSize.ToString() + "  ");
                printf_xy(x, y + 8 + topMargin, Bar((allControllers[slot - 1].Response.bufferPos+1) / (double) allControllers[slot - 1].Response.bufferSize, ColumnWidth - 3));
                printf_xy(x, y + 9 + topMargin, allControllers[slot - 1].Response.watts.ToString() + "W " + allControllers[slot - 1].Response.brightness.ToString("F0") + "%");
                printf_xy(x, y + 10 + topMargin, allControllers[slot - 1].Response.fpsDrawing.ToString());
                printf_xy(x, y + 11 + topMargin, allControllers[slot - 1].Connects.ToString() + " ");
                printf_xy(x, y + 12 + topMargin, allControllers[slot - 1].QueueDepth.ToString() + " ");
                if (allControllers[slot - 1].Location != null)
                {
                    printf_xy(x, y + 13 + topMargin, allControllers[slot - 1].TimeOffset.ToString("F2") + " [" + allControllers[slot - 1].Location.FramesPerSecond + "]");
                    printf_xy(x, y + 14 + topMargin, allControllers[slot - 1].Location.CurrentEffectName.Left(ColumnWidth-1));
                }
            }

            int topLine = y + ColumnHeight + 2;

            printf_xy(0, topLine, Dashes);
            printf_xy(0, topLine + 3, Dashes);

            lock (textLines)
            {
                for (int line = Console.WindowHeight - bottomMargin; line >= topLine + 1; line--)
                {
                    const int leftTextMargin = 15;
                    int yLine = line - topLine;
                    if (textLines.Count > yLine)
                    {
                        string text = textLines[yLine].Item1;
                        DateTime timeStamp = textLines[yLine].Item2;

                        printf_xy(0, line + topMargin, "[" + timeStamp.ToString("s.f") + "]");
                        printf_xy(leftTextMargin, line + topMargin, text.Left(Console.WindowWidth - leftTextMargin));                           // Print new line
                        printf_xy(leftTextMargin + text.Length, line + topMargin, Spaces.Substring(0, Math.Max(0, Console.WindowWidth - leftTextMargin - text.Length)));  // Clear old line
                    }
                }
            }

            printf_xy(0, topLine + 1, "Total Bytes Sent : " + totalBytes.ToString() + "       ");
            double ratio = totalBytes / 200000.0;
            if (ratio > 1)
                ratio = 1;
            int leftMargin = 26;
            int maxLen = Math.Min(132, Console.WindowWidth) - leftMargin;
            string bar = Bar(ratio, maxLen);
            printf_xy(leftMargin, topLine + 1, bar);

            printf_xy(0, topLine + 2, "Miliseconds Spare: " + Location.MinimumSpareTime.ToString() + "       ");
            double ratio2 = Location.MinimumSpareTime / 40.0;
            if (ratio2 > 1)
                ratio2 = 1;
            string bar2 = Bar(ratio2, maxLen);
            printf_xy(leftMargin, topLine + 2, bar2);
        }
    }
}
 