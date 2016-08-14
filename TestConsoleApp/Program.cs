using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Model;


namespace TestConsoleApp
{
    using System;
    using System.Windows;

    class Program
    {
        static void Main(string[] args)
        {
            var _portCfg = new SerialPortConfig()
            {
                Port = "COM3",
                BaudRate = 19200,
                Parity = System.IO.Ports.Parity.None,
                DataBits = 8,
                StopBits = System.IO.Ports.StopBits.One,
                ReceiveLength = 11
            };

            using (var serialPort = new SerialPortWraper(_portCfg))
            {
                serialPort.Open();

                //serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.ErrorReceived += SerialPort_ErrorReceived;


                Console.WriteLine($"Port is open: {serialPort.IsOpen}");

                if (!serialPort.IsOpen)
                {
                    Console.WriteLine($"Port not open - exit");
                    return;
                }

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                //m.Set6f();

                //m.SetHandshake(255, 1);

                //Assert.IsTrue(res);

                Thread.Sleep(500);

                int c = 0;
                short ok;
                short ng;

                while (c++ < 10)
                {
                    var res = m.ReadStavCitacu(out ok, out ng);

                    Console.WriteLine($"res = {res}  OK = {ok}  NG = {ng}");

                    Thread.Sleep(300);
                }


                //MessageBox.Show("Hello, world!");

                serialPort.Close();

            }
            Console.WriteLine($"TestMethodZapsatNacistCitace - OK");
            Console.ReadLine();

        }

        private static void SerialPort_ErrorReceived(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine($"Error received");
        }

        private static void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            Console.WriteLine($"Data");
        }
    }

    class Test
    {
        
    }
}
