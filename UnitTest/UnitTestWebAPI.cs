using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class UnitTestWebAPI
    {
        string baseAddress = "http://localhost:5000/";

        [TestMethod]
        public void TestConnectToService()
        {
            using (HttpClient client = new HttpClient())
            {
                var response = client.GetAsync(baseAddress + "api/slevy").Result;

                Assert.IsNotNull(response, "Impossible to connect to service");

                Console.WriteLine("Information from service: {0}", response.Content.ReadAsStringAsync().Result);
            }
        }

        [TestMethod]
        public void TestStatus()
        {
            using (HttpClient client = new HttpClient())
            {
                var response = client.GetAsync(baseAddress + "api/slevy/status?Addr=100").Result;
                Assert.IsNotNull(response);
                Assert.IsNotNull(response.Content.ReadAsStringAsync().Result);
                Console.WriteLine("status: " + response.Content.ReadAsStringAsync().Result);
            }
        }

        [TestMethod]
        public void TestNastavOkNg()
        {
            using (HttpClient client = new HttpClient())
            {
                var response = client.GetAsync(baseAddress + "api/slevy/nastavOkNg?Addr=100&ok=10&ng=5").Result;
                Assert.IsNotNull(response);
                Assert.IsNotNull(response.Content.ReadAsStringAsync().Result);
                Console.WriteLine("nastav ok a ng:" + response.Content.ReadAsStringAsync().Result);
            }
        }

        [TestMethod]
        public void TestRefreshStavCitacu()
        {
            using (HttpClient client = new HttpClient())
            {
                var response = client.GetAsync(baseAddress + "api/slevy/refreshStavCitacu?Addr=100").Result;
                Assert.IsNotNull(response);
                Assert.IsNotNull(response.Content.ReadAsStringAsync().Result);
                Console.WriteLine("refresh citace: " + response.Content.ReadAsStringAsync().Result);
            }
        }

        [TestMethod]
        public void TestSetConfig()
        {
            using (HttpClient client = new HttpClient())
            {
                //public bool SetConfig([FromUri] bool isMockupMode, [FromUri] bool isTimerOn, [FromUri] int timerPeriod)



                var response = client.GetAsync(baseAddress + "api/slevy/SetConfig?isMockupMode=true&isTimerOn=true&timerPeriod=100").Result;

                Assert.IsNotNull(response);
                Assert.IsNotNull(response.Content.ReadAsStringAsync().Result);
                Console.WriteLine("SetConfig: " + response.Content.ReadAsStringAsync().Result);
            }
        }
    }
}
