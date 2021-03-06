﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using NLog;
using NLog.Config;
using Slevyr.DataAccess.Services;
using Slevyr.WebAppHost.Properties;

namespace Slevyr.WebAppHost
{
    /*
        http://localhost:5000/api/slevyr/nastavOkNg?ok=10&ng=5

        http://localhost:5000/api/slevyr/nastavDefektivitu?varianta=A&def1=1&def2=33&def3=44

        http://localhost:5000/api/slevyr/vratitStavCitacu

        http://localhost:5000/api/slevyr/status

        http://localhost:5000/api/slevyr/closePort



        netsh http add urlacl url=http://+:5000/ user=Everyone
        netsh http delete urlacl url=http://+:5000/ user=Everyone

    */


    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main()
        {
            bool useLocalHost = Settings.Default.UseLocalHost;
            int port = Settings.Default.WebAppPort;
            string baseAddress = $"http://localhost:{port}/";

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //ConfigurationItemFactory.Default.Targets.RegisterDefinition("MyFirst", typeof(MyNamespace.MyFirstTarget));


            // Start OWIN host 
            if (useLocalHost)
            {
                using (WebApp.Start<Startup>(url: baseAddress))
                {
                    DoStart(port, baseAddress);
                }
            }
            else
            {
                using (WebApp.Start($"http://+:{port}/"))
                {
                    DoStart(port,baseAddress);
                }
            }
        }

        private static void DoStart(int port, string baseAddress)
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

            Console.WriteLine("Stiskem klávesy se služba ukončí !\n");

            Process.Start(baseAddress + "menu.html");

            Console.ReadLine();

            SlevyrService.ClosePort();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error("Unhandled exception: {0}",e.ExceptionObject);
            //e.Handled = true;
        }
    }
}