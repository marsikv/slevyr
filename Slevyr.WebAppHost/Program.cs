using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using NLog;
using Slevyr.WebAppHost.Properties;

namespace Slevyr.WebAppHost
{
    /*
        http://localhost:5000/api/slevy/nastavOkNg?ok=10&ng=5

        http://localhost:5000/api/slevy/nastavDefektivitu?varianta=A&def1=1&def2=33&def3=44

        http://localhost:5000/api/slevy/vratitStavCitacu

        http://localhost:5000/api/slevy/status

        http://localhost:5000/api/slevy/closePort



        netsh http add urlacl url=http://+:5000/ user=Everyone
        netsh http delete urlacl url=http://+:5000/ user=Everyone

    */

    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main()
        {
            int port = Settings.Default.WebAppPort;
            string baseAddress = $"http://localhost:{port}/";


            // Start OWIN host 
            using (WebApp.Start($"http://+:{port}/"))
            //using (WebApp.Start<Startup>(url: baseAddress))
            {

                HttpClient client = new HttpClient();

                var response = client.GetAsync(baseAddress + "api/slevy").Result;

                if (response != null)
                {
                    Console.WriteLine("Information from service: {0}", response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    Console.WriteLine("ERROR: Impossible to connect to service");
                }


                Logger.Info($"\nWebApp Started on {baseAddress}\n");

                Console.WriteLine("Stiskem klávesy se služba ukončí !\n");

                Process.Start(baseAddress + "menu.html");

                Console.ReadLine();

                response = client.GetAsync(baseAddress + "api/slevy/closePort").Result;
            }
        }
    }
}
