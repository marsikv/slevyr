using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Model;


namespace UnitTest
{

    [TestClass]
    public class UnitTestPlantMonitor
    {
        #region fields

        SerialPortConfig _portCfg;
        //SerialPortWraper _sp;

        private TestContext testContextInstance;
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #endregion

        #region ctor

        public UnitTestPlantMonitor()
        {
            _portCfg = new SerialPortConfig()
            {
                Port = "COM4",
                BaudRate = 19200,
                Parity = System.IO.Ports.Parity.None,
                DataBits = 8,
                StopBits = System.IO.Ports.StopBits.One,
                ReceiveLength = 11
            };
        }

        #endregion

        #region Init&cleanup

        [TestInitialize]
        public void TestInit()
        {
            //_sp = new SerialPortWraper(_portCfg);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            //_sp.Close();
            //_sp.Dispose();
        }

        #endregion

        #region private methods

        private void PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            Console.WriteLine(sb.ToString());
        }

        #endregion

        [TestMethod]
        public void TestMethodComEnumerationNotEmpty()
        {
            var res = System.IO.Ports.SerialPort.GetPortNames();
            Assert.IsTrue(res.Length > 0);
            Console.WriteLine("Nalezené porty:");
            foreach (var re in res)
            {
                Console.WriteLine($"{re}");
            }
            
        }

        [TestMethod]
        public void TestMethodOpenPort()
        {
            using (SerialPortWraper serialPort = new SerialPortWraper(_portCfg))
            {
                //Thread.Sleep(1000);
                if (serialPort.IsOpen) serialPort.Close();
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");
                Console.WriteLine($"port {_portCfg.Port} otevřen");
                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");
                Console.WriteLine($"port {_portCfg.Port} uzavřen");

                Thread.Sleep(500);
            }
            Console.WriteLine($"TestMethodOpenPort - OK");
        }

        [TestMethod]
        public async Task TestMethodNastavCileSmen18()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");


                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                var res = await m.SetCileSmen('A', 100, 50, 10);

                Assert.IsTrue(res);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");

                Thread.Sleep(500);
            }
            Console.WriteLine($"TestMethodNastavCileSmen18 - OK");
        }

        [TestMethod]
        public async void TestMethodNastavSmennost16()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                var res = await m.SetSmennost('A');

                Assert.IsTrue(res);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");

                Thread.Sleep(500);
            }
            Console.WriteLine($"TestMethodNastavSmennost16 - OK");
        }


        [TestMethod]
        public async void TestMethodZapsatCitace4()
        {
            Console.WriteLine($"TestMethodZapsatCitace4 - start");
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                try
                {
                    serialPort.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //throw;
                }

                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                var res = await m.SetCitace(99, 33);

                Assert.IsTrue(res);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");

                Thread.Sleep(500);
            }
            Console.WriteLine($"TestMethodZapsatCitace4 - OK");
        }


        [TestMethod]
        public async void TestMethodReset7()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                Assert.IsTrue(await m.Reset());

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");

                Thread.Sleep(500);
            }
            Console.WriteLine($"TestMethodReset7 - OK");
        }

        [TestMethod]
        public async void TestMethodVratitZaklNastaveni9()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                //var data = await m.ReadZaklNastaveni();
                //var task = m.ReadZaklNastaveni();

                //PrintByteArray(task.Result);

                ZaklNastaveniDto zaklNastaveni = await m.ReadZaklNastaveni();

                Console.WriteLine($"minOK={zaklNastaveni.MinOk} minNG={zaklNastaveni.MinNg} verzeSw1={zaklNastaveni.VerzeSw1}");

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");

                Thread.Sleep(500);
            }
            Console.WriteLine($"TestMethodVratitZaklNastaveni9 - OK");
        }

        //[TestMethod]
        public async void TestMethodNastavJas()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                var res = await m.SetJasLcd(10);

                Assert.IsTrue(res);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");

                Thread.Sleep(500);
            }
            Console.WriteLine($"TestMethodVratitZaklNastaveni9 - OK");
        }

        [TestMethod]
        public async void TestMethodVratitStavCitacu96()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                //var data = await m.ReadZaklNastaveni();
                //var task = m.ReadZaklNastaveni();

                //PrintByteArray(task.Result);

                Console.WriteLine($"TestMethodVratitZaklNastaveni9 - OK");

                StavCitacuDto stavCitacu = await m.ReadStavCitacu();

                Assert.IsTrue(stavCitacu != null);

                Console.WriteLine($"OK={stavCitacu.Ok} NG={stavCitacu.Ng}");

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");

                Thread.Sleep(500);
            }
            Console.WriteLine($"TestMethodVratitStavCitacu96 - OK");
        }

        [TestMethod]
        public async void TestMethodZapsatNacistCitace()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                short okVal = 123;
                short ngVal = 54;

                var res = await m.SetCitace(okVal, ngVal);

                Assert.IsTrue(res);

                short ok;
                short ng;

                Thread.Sleep(500);

                StavCitacuDto stavCitacu = await m.ReadStavCitacu();

                Assert.IsTrue(stavCitacu != null);

                Console.WriteLine($"OK={stavCitacu.Ok} NG={stavCitacu.Ng}");

                Assert.AreEqual(stavCitacu.Ok, okVal);
                Assert.AreEqual(stavCitacu.Ng, ngVal);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");

                Thread.Sleep(500);
            }
            Console.WriteLine($"TestMethodZapsatNacistCitace - OK");
        }
    }
}
