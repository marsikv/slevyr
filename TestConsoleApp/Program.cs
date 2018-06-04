using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.DAO;
using Slevyr.DataAccess.Model;


namespace TestConsoleApp
{
    using System;
    using System.Windows;

    class Program
    {
        private static SerialPortConfig _portCfg = new SerialPortConfig()
        {
            Port = "COM3",
            BaudRate = 19200,
            Parity = System.IO.Ports.Parity.None,
            DataBits = 8,
            StopBits = System.IO.Ports.StopBits.One,
            ReceivedBytesThreshold = 11,
            //ReceiveLength = 11
        };

        static void Main(string[] args)
        {
            //TestReadPort();

            //TestCreateSchema();

            //TestPortSendRead();

            TestPortSendRozhlasStart();
        }

        private static void SerialPort_ErrorReceived(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine($"Error received");
        }

        private static void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;

            if (!sp.IsOpen) return;

            var len = sp.BytesToRead;

            Byte[] buf = new Byte[len];

            sp.Read(buf, 0, len);

            Console.WriteLine($" <- {len:00}; {BitConverter.ToString(buf)}");
        }

        //private static void TestCreateSchema()
        //{
        //    var status = new UnitStatus() {Ok = 134, CasOk = 5454, CilKusuTabule = 1000, AktualDefectTabule = 0};
        //    SqlliteDao.OpenConnection(true);
        //    SqlliteDao.AddUnitState(100,status);
        //    status.Ok = 135;
        //    status.CasOk = 5455;
        //    SqlliteDao.AddUnitState(100, status);
        //    SqlliteDao.CloseConnection();
        //}


            /// <summary>
            /// posilam prikaz pomoci UnitMonitor
            /// </summary>
        static void TestUnitMonitorSend()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();

                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.ErrorReceived += SerialPort_ErrorReceived;

                Console.WriteLine($"Port is open: {serialPort.IsOpen}");

                if (!serialPort.IsOpen)
                {
                    Console.WriteLine($"Port not open - exit");
                    return;
                }


                var runCfg = new RunConfig() { IsReadOkNgTime = true, };

                UnitMonitor m = new UnitMonitor(100,runCfg);
                
              

                //m.Set6f();

                //m.SetHandshake(255, 1);

                //Assert.IsTrue(res);

                Thread.Sleep(500);

                int c = 0;
                int ok;
                int ng;

                while (c++ < 10)
                {
                    var res = m.SendReadStavCitacu();

                    Thread.Sleep(300);
                }


                //MessageBox.Show("Hello, world!");

                serialPort.Close();

            }
            Console.WriteLine($"TestMethodZapsatNacistCitace - OK");
            Console.ReadLine();
        }

        /// <summary>
        /// Testuji odeslani primo na port
        /// </summary>
        static void TestPortSendRozhlasStart()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();

                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.ErrorReceived += SerialPort_ErrorReceived;

                Console.WriteLine($"Port is open: {serialPort.IsOpen}");

                if (!serialPort.IsOpen)
                {
                    Console.WriteLine($"Port not open - exit");
                    return;
                }

                byte[] _sendBuff = { 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                serialPort.Write(_sendBuff, _sendBuff.Length);

                Console.WriteLine($"0x10 odeslano, cekam na enter");

                //Thread.Sleep(500);

                Console.ReadLine();

                serialPort.Close();

            }
            Console.WriteLine($"OK");

            Console.ReadLine();

        }

        static void TestPortSendRead()
        {
            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();

                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.ErrorReceived += SerialPort_ErrorReceived;

                Console.WriteLine($"Port is open: {serialPort.IsOpen}");

                if (!serialPort.IsOpen)
                {
                    Console.WriteLine($"Port not open - exit");
                    return;
                }
             
                byte[] _sendBuff = { 0x00, 0x00, 0x63, 0x6C, 0x00, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00 };

                serialPort.Write(_sendBuff, _sendBuff.Length);

                Thread.Sleep(500);

                serialPort.Close();

            }
            Console.WriteLine($"OK");
            Console.ReadLine();
        }
    }
}
