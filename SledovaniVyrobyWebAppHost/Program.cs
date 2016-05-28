using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace SledovaniVyrobyWebAppHost
{
    /*

        http://localhost:5000/api/slevy/nastavOkNg?ok=10&ng=5

        http://localhost:5000/api/slevy/nastavDefektivitu?varianta=A&def1=1&def2=33&def3=44

        http://localhost:5000/api/slevy/vratitStavCitacu

        http://localhost:5000/api/slevy/status

        http://localhost:5000/api/slevy/closePort

    */
    class Program
    {
        static void Main()
        {
            string baseAddress = "http://localhost:5000/";

            // Start OWIN host 
            using (WebApp.Start<Startup>(url: baseAddress))
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


                //response = client.GetAsync(baseAddress + "api/slevy/openPort").Result;
                //Console.WriteLine("port open:"+response.Content.ReadAsStringAsync().Result);


                Console.WriteLine("Stiskem klávesy se služba ukončí !\n");

                Process.Start(baseAddress+"index.html");

                Console.ReadLine();

                response = client.GetAsync(baseAddress + "api/slevy/closePort").Result;
            }
        }
    }
}
