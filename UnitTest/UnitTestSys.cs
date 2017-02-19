using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SledovaniVyroby.SerialPortWraper;

namespace UnitTest
{
    [TestClass]
    public class UnitTestSys
    {
        [TestMethod]
        public void TestMethod1()
        {

            var portCfg = new SerialPortConfig()
            {
                Port = "COM5",
                BaudRate = 19200,
                Parity = System.IO.Ports.Parity.None,
                DataBits = 8,
                StopBits = System.IO.Ports.StopBits.One,
                ReceivedBytesThreshold = 1,
            };

            var _serialPort = new SerialPortWraper(portCfg);

            _serialPort.Open();

            _serialPort.DtrEnable = true;

            Thread.Sleep(1000);

            _serialPort.DtrEnable = true;

            Thread.Sleep(500);

            _serialPort.DtrEnable = false;

            Thread.Sleep(500);

            _serialPort.DiscardOutBuffer();
            _serialPort.DiscardInBuffer();

            _serialPort.Close();
        }
    }
}
