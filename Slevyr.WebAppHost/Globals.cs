using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Model;

namespace Slevyr.WebAppHost
{
    public static class Globals
    {
        public static RunConfig RunConfig { get; private set; }

        public static SerialPortConfig PortConfig { get; private set; }

        public static int WebAppPort;

        public static bool UseLocalHost;

        public static bool StartWebApi;

        public static bool StartSwagger;

        public static string WwwRootDir;


        static Globals()
        {
        }

        public static void LoadSettings()
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
                JsonDataFilePath = ConfigurationManager.AppSettings["JsonFilePath"],
                DbFilePath = ConfigurationManager.AppSettings["DbFilePath"],
                SendAttempts = int.Parse(ConfigurationManager.AppSettings["SendAttempts"]),
            };

            bool b;
            bool.TryParse(ConfigurationManager.AppSettings["OldSyncMode"], out b); RunConfig.OldSyncMode = b;            
            bool.TryParse(ConfigurationManager.AppSettings["IsWaitCommandResult"], out b); RunConfig.IsWaitCommandResult = b;
            //if (RunConfig.DbFilePath == null) RunConfig.DbFilePath = RunConfig.JsonDataFilePath;

            PortConfig = new SerialPortConfig
            {
                Port = ConfigurationManager.AppSettings["Port"],
                BaudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]),
                ReceivedBytesThreshold = int.Parse(ConfigurationManager.AppSettings["ReceivedBytesThreshold"]),
                Parity = System.IO.Ports.Parity.None,
                DataBits = 8,
                StopBits = System.IO.Ports.StopBits.One,
                ReceiveLength = 11
            };

            bool.TryParse(ConfigurationManager.AppSettings["StartWebApi"], out StartWebApi);
            bool.TryParse(ConfigurationManager.AppSettings["StartSwagger"], out StartSwagger);
            bool.TryParse(ConfigurationManager.AppSettings["UseLocalHost"], out UseLocalHost);
            int.TryParse(ConfigurationManager.AppSettings["WebAppPort"], out WebAppPort);
            WwwRootDir = ConfigurationManager.AppSettings["www.rootDir"];
        }

        public static string RunConfigToJson()
        {
            return JsonConvert.SerializeObject(RunConfig);          
        }
    
}



}
