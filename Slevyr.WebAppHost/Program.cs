using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using Microsoft.Owin.Hosting;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Services;
using Slevyr.WebAppHost.Properties;

namespace Slevyr.WebAppHost
{
    /*  rezervace portu pro Web server
        netsh http add urlacl url=http://+:5000/ user=Everyone
        netsh http delete urlacl url=http://+:5000/ user=Everyone
    */


    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Globals.LoadSettings();

            Logger.Info("");
            Logger.Info(Globals.RunConfigToJson());

            int port = Globals.WebAppPort;
            string baseAddress = $"http://localhost:{port}/";

            SlevyrService.Init(Globals.PortConfig, Globals.RunConfig);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (Debugger.IsAttached || args.Length > 0)
            {
                Console.WriteLine("start");
                
                if (Globals.StartWebApi)
                {
                    if (Globals.UseLocalHost)
                    {
                        using (WebApp.Start<Startup>(url: baseAddress))
                        {
                            DoStartInConsole(baseAddress);
                        }
                    }
                    else
                    {
                        using (WebApp.Start($"http://+:{port}/"))
                        {
                            DoStartInConsole(baseAddress);                           
                        }
                    }
                }
                else
                {
                    SlevyrService.Start();
                    Console.ReadLine();
                }

                //Console.WriteLine("Napiš: exit pro ukončení!\n");
                //while (!"exit".Equals(Console.ReadLine()))
                //{
                //    System.Threading.Thread.Sleep(1000);
                //}

                SlevyrService.Stop();

                Console.WriteLine("Ukonceno; stiskni libovolnou klavesu...");
                Console.ReadKey();
            }
            else
            {
                var servicesToRun = new ServiceBase[]
                {
                    new Service()
                };
                ServiceBase.Run(servicesToRun);
            }

        }

        private static void DoStartInConsole(string baseAddress)
        {
            HttpClient client = new HttpClient();

            var response = client.GetAsync(baseAddress + "api/slevyr/getApiVersion").Result;

            if (response != null)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("API version: {0}", response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    Console.WriteLine("ERROR: status code {0}", response.StatusCode);
                }
            }
            else
            {
                Console.WriteLine("ERROR: Impossible to connect to service");
            }

            Logger.Info($"\nWebApp Started on {baseAddress}\n");

            Console.WriteLine("Stiskni [Enter] pro ukončení!\n");

            SlevyrService.Start();  //zde teprve startuji workery pro praci s jednotkami

            Process.Start(baseAddress + "index.html");

            Console.ReadLine();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error("Unhandled exception: {0}",e.ExceptionObject);
            //e.Handled = true;
        }
    }
}