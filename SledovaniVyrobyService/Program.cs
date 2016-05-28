using System;
using System.Threading;
using SledovaniVyroby.SerialPortWraper;
using SledovaniVyroby.SledovaniVyrobyService;

namespace SledovaniVyrobyService
{
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[]
            //{
            //    new Service1()
            //};
            //ServiceBase.Run(ServicesToRun);

            var portCfg = new SerialPortConfig()
            {
                Port = "COM3",
                BaudRate = 19200,
                Parity = System.IO.Ports.Parity.None,
                DataBits = 8,
                StopBits = System.IO.Ports.StopBits.One,
                ReceiveLength = 11
            };

            using (var serialPort = new SerialPortWraper(portCfg))
            {
                serialPort.Open();

                UnitMonitor m = new UnitMonitor(100);
                m.SerialPort = serialPort;

                //zapnu simulaci na jednotce - inkrementaci OK
                //m.Set6f();

                //Thread.Sleep(500);

                //var res = m.SetHandshake(0, 1);

                //Assert.IsTrue(res);

                Thread.Sleep(500);

                int c = 0;
                short ok;
                short ng;

                while (c++ < 10)
                {
                    m.ReadStavCitacu(out ok, out ng);

                    Console.WriteLine($"OK = {ok}  NG = {ng}");

                    Thread.Sleep(300);
                }


                serialPort.Close();
            }
            Console.WriteLine($"TestMethodZapsatNacistCitace - OK");

            Console.ReadLine();
        }
    }
    
}
