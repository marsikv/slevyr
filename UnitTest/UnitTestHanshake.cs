using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Model;


namespace UnitTest
{
    /// <summary>
    /// Summary description for UnitTestHanshake
    /// </summary>
    [TestClass]
    public class UnitTestHanshake
    {

        SerialPortConfig _portCfg;

        public UnitTestHanshake()
        {
            _portCfg = new SerialPortConfig()
            {
                Port = "COM3",
                BaudRate = 19200,
                Parity = System.IO.Ports.Parity.None,
                DataBits = 8,
                StopBits = System.IO.Ports.StopBits.One,
                //ReceiveLength = 11
            };
        }

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

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion
            /*
        [TestMethod]
        public void TestMethod1()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();
                Assert.IsTrue(serialPort.IsOpen, $"port {_portCfg.Port} nelze otevřít");

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                m.Set6f();

                m.SetHandshake(0, 1);

                //Assert.IsTrue(res);

                Thread.Sleep(500);

                int c = 0;
                int ok;
                int ng;

                while (c++ < 100)
                {
                    var res = m.r();

                    if (res)
                    {
                        m.UnitStatus.Ok = ok;
                        m.UnitStatus.Ng = ng;
                    }

                    Console.WriteLine($"OK = {ok}  NG = {ng}");

                    Thread.Sleep(300);
                }


                serialPort.Close();
                Assert.IsFalse(serialPort.IsOpen, $"port {_portCfg.Port} nelze uzařít");

                
            }
            Console.WriteLine($"TestMethodZapsatNacistCitace - OK");
        }
        */
    }
    
}
