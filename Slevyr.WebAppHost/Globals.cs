using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Newtonsoft.Json.Linq;
using Slevyr.DataAccess.Model;

namespace Slevyr.WebAppHost
{
    public static class Globals
    {
        public static RunConfig RunConfig { get; private set; }

        public static string Port { get; private set; }
        public static int BaudRate { get; private set; }

        public static int ReceivedBytesThreshold { get; private set; }

        static Globals()
        {
        }

        public static void LoadSettings()
        {
            try
            {
                RunConfig = new RunConfig();

                RunConfig.IsMockupMode = bool.Parse(ConfigurationManager.AppSettings["MockupMode"]);
                RunConfig.IsRefreshTimerOn = bool.Parse(ConfigurationManager.AppSettings["IsRefreshTimerOn"]);
                RunConfig.IsReadOkNgTime = bool.Parse(ConfigurationManager.AppSettings["IsReadOkNgTime"]);
                RunConfig.IsWriteEmptyToLog = bool.Parse(ConfigurationManager.AppSettings["IsWriteEmptyToLog"]);

                RunConfig.RefreshTimerPeriod = int.Parse(ConfigurationManager.AppSettings["RefreshTimerPeriod"]);
                RunConfig.WorkerSleepPeriod = int.Parse(ConfigurationManager.AppSettings["WorkerSleepPeriod"]);
                RunConfig.RelaxTime = int.Parse(ConfigurationManager.AppSettings["RelaxTime"]);
                RunConfig.ReadResultTimeOut = int.Parse(ConfigurationManager.AppSettings["ReadResultTimeOut"]);
                RunConfig.SendCommandTimeOut = int.Parse(ConfigurationManager.AppSettings["SendCommandTimeOut"]);
                RunConfig.MinCmdDelay = int.Parse(ConfigurationManager.AppSettings["MinCmdDelay"]);

                RunConfig.UnitAddrs = ConfigurationManager.AppSettings["UnitAddrs"].Split(';').Select(int.Parse);

                RunConfig.DataFilePath = ConfigurationManager.AppSettings["JsonFilePath"];

                Port = ConfigurationManager.AppSettings["Port"];
                BaudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]);
                ReceivedBytesThreshold = int.Parse(ConfigurationManager.AppSettings["ReceivedBytesThreshold"]);
            }
            catch (Exception )
            {
                throw;
            }
        }
    }

}
