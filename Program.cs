using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Threading;
using System.IO;
using NightDriver;

namespace WebHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Thread ledControlThread = new Thread(NightDriver.ConsoleApp.Start);
            ledControlThread.IsBackground = true;

            var host = CreateHostBuilder(args).Build();
            var hostTask = host.StartAsync();

            ledControlThread.Start();

            while (ledControlThread.IsAlive)
               Thread.Sleep(1000);

            host.StopAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
