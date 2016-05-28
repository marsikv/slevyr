using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SledovaniVyroby.SerialPortWraper;
using SledovaniVyroby.SledovaniVyrobyService;

namespace UnitTest
{

    [TestClass]
    public class UnitTestPlantMonitor
    {
        #region fields

        SerialPortConfig _portCfg;

        #endregion

        #region ctor

        public UnitTestPlantMonitor()
        {
            _portCfg = new SerialPortConfig()
            {
                Port = "COM3",
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
        }

        [TestCleanup]
        public void TestCleanup()
        {
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
            System.Console.WriteLine("Nalezené porty:");
            foreach (var re in res)
            {
                System.Console.WriteLine($"{re}");
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
            }
            Console.WriteLine($"TestMethodOpenPort - OK");
        }

        [TestMethod]
        public void TestMethodNastavCileSmen18()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");


                PlantMonitor m = new PlantMonitor(100);
                m.SerialPort = serialPort;

                var task = m.NastavCileSmen18('A', 100, 50, 10);

                task.Wait();

                //var data = await m.NastavCileSmen18('A', 100, 50, 10);

                PrintByteArray(task.Result);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");
            }
            Console.WriteLine($"TestMethodNastavCileSmen18 - OK");
        }

        [TestMethod]
        public void TestMethodNastavSmennost16Task()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                PlantMonitor m = new PlantMonitor(100);
                m.SerialPort = serialPort;

                var res = m.NastavSmennost16('A');

                Assert.IsTrue(res);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");                
            }
            Console.WriteLine($"TestMethodNastavSmennost16Task - OK");
        }


        [TestMethod]
        public void TestMethodZapsatCitace4()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                PlantMonitor m = new PlantMonitor(100);
                m.SerialPort = serialPort;

                //var data = await m.ZapsatCitace4(99,33);
                var task = m.ZapsatCitace4(99, 33);
                task.Wait();


                PrintByteArray(task.Result);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");
            }
            Console.WriteLine($"TestMethodZapsatCitace4 - OK");
        }


        [TestMethod]
        public void TestMethodReset7()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                PlantMonitor m = new PlantMonitor(100);
                m.SerialPort = serialPort;

                var task = m.Reset7();

                //var data = await m.Reset7();

                PrintByteArray(task.Result);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");
            }
            Console.WriteLine($"TestMethodReset7 - OK");
        }

        [TestMethod]
        public void TestMethodVratitZaklNastaveni9()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                PlantMonitor m = new PlantMonitor(100);
                m.SerialPort = serialPort;

                //var data = await m.VratitZaklNastaveni9();
                var task = m.VratitZaklNastaveni9();

                PrintByteArray(task.Result);

                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");
            }
            Console.WriteLine($"TestMethodVratitZaklNastaveni9 - OK");
        }
    }
}
