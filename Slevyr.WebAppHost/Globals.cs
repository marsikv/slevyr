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
                RunConfig = new RunConfig
                {
                    IsMockupMode = bool.Parse(ConfigurationManager.AppSettings["MockupMode"]),
                    IsRefreshTimerOn = bool.Parse(ConfigurationManager.AppSettings["IsRefreshTimerOn"]),
                    IsReadOkNgTime = bool.Parse(ConfigurationManager.AppSettings["IsReadOkNgTime"]),
                    //IsWriteEmptyToLog = bool.Parse(ConfigurationManager.AppSettings["IsWriteEmptyToLog"]),
                    RefreshTimerPeriod = int.Parse(ConfigurationManager.AppSettings["RefreshTimerPeriod"]),
                    WorkerSleepPeriod = int.Parse(ConfigurationManager.AppSettings["WorkerSleepPeriod"]),
                    RelaxTime = int.Parse(ConfigurationManager.AppSettings["RelaxTime"]),
                    ReadResultTimeOut = int.Parse(ConfigurationManager.AppSettings["ReadResultTimeOut"]),
                    SendCommandTimeOut = int.Parse(ConfigurationManager.AppSettings["SendCommandTimeOut"]),
                    MinCmdDelay = int.Parse(ConfigurationManager.AppSettings["MinCmdDelay"]),
                    UnitAddrs = ConfigurationManager.AppSettings["UnitAddrs"].Split(';').Select(int.Parse),
                    DataFilePath = ConfigurationManager.AppSettings["JsonFilePath"],
                    IsWaitCommandResult = bool.Parse(ConfigurationManager.AppSettings["IsWaitCommandResult"]),
                    SendAttempts = int.Parse(ConfigurationManager.AppSettings["SendAttempts"]),
                };

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
