using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ProjectMaster
{
    public class Program
    {
#if DEBUG
        public const bool Debug = true;
#else
        public const bool Debug = false;
        #endif

        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                .UseStartup<Startup>()
                .Build();

        public static void ApplicationStarted()
        {
        }

        public static void ApplicationStopping()
        {
            Console.WriteLine("Application stopping!");
        }

        public static void ApplicationStopped()
        {
            Console.WriteLine("Application stopped!");
        }
    }
}