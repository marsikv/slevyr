using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using Newtonsoft.Json;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Model;
using Slevyr.WebAppHost.Model;

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

        //public static IEnumerable<string> PowerUsers;

        public static List<Model.User> Users;

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
                //WorkerSleepPeriod = int.Parse(ConfigurationManager.AppSettings["WorkerSleepPeriod"]),
                RelaxTime = int.Parse(ConfigurationManager.AppSettings["RelaxTime"]),
                ReadResultTimeOut = int.Parse(ConfigurationManager.AppSettings["ReadResultTimeOut"]),
                SendCommandTimeOut = int.Parse(ConfigurationManager.AppSettings["SendCommandTimeOut"]),
                MinCmdDelay = int.Parse(ConfigurationManager.AppSettings["MinCmdDelay"]),
                UnitAddrs = ConfigurationManager.AppSettings["UnitAddrs"].Split(';').Select(int.Parse),
                JsonDataFilePath = ConfigurationManager.AppSettings["JsonFilePath"],
                DbFilePath = ConfigurationManager.AppSettings["DbFilePath"],
                SendAttempts = int.Parse(ConfigurationManager.AppSettings["SendAttempts"]),
                DefaultExportFileName = ConfigurationManager.AppSettings["DefaultExportFileName"],                
            };

            bool b;
            bool.TryParse(ConfigurationManager.AppSettings["IsWaitCommandResult"], out b); RunConfig.IsWaitCommandResult = b;
            bool.TryParse(ConfigurationManager.AppSettings["UseDataReceivedEvent"], out b); RunConfig.UseDataReceivedEvent = b;
            var s = ConfigurationManager.AppSettings["DecimalSeparator"];
            if (!String.IsNullOrEmpty(s)) RunConfig.DecimalSeparator = s[0];

            PortConfig = new SerialPortConfig
            {
                Port = ConfigurationManager.AppSettings["Port"],
                BaudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]),
                ReceivedBytesThreshold = int.Parse(ConfigurationManager.AppSettings["ReceivedBytesThreshold"]),
                Parity = System.IO.Ports.Parity.None,
                DataBits = 8,
                StopBits = System.IO.Ports.StopBits.One,
            };           

            bool.TryParse(ConfigurationManager.AppSettings["StartWebApi"], out StartWebApi);
            bool.TryParse(ConfigurationManager.AppSettings["StartSwagger"], out StartSwagger);
            bool.TryParse(ConfigurationManager.AppSettings["UseLocalHost"], out UseLocalHost);
            int.TryParse(ConfigurationManager.AppSettings["WebAppPort"], out WebAppPort);
            WwwRootDir = ConfigurationManager.AppSettings["www.rootDir"];

            var usersDef = ConfigurationManager.AppSettings["Users"]?.Split(';');
            Users = usersDef?.Select(u => new User(u)).ToList();
            if (Users != null)
            {
                ClassifyAdminUsers(ConfigurationManager.AppSettings["AdminUsers"]?.Split(';'));
                ClassifyMaintenanceUsers(ConfigurationManager.AppSettings["MaintenanceUsers"]?.Split(';'));
            }
        }

        public static string RunConfigToJson()
        {
            return JsonConvert.SerializeObject(RunConfig);          
        }

        private static void ClassifyAdminUsers(IEnumerable<string> userNames)
        {
            foreach (var user in userNames.Select(pu => Users.FirstOrDefault(u => u.Name.Equals(pu, StringComparison.InvariantCultureIgnoreCase))))
            {
                user?.SetAdminRole();
            }
        }

        private static void ClassifyMaintenanceUsers(IEnumerable<string> userNames)
        {
            foreach (var user in userNames.Select(pu => Users.FirstOrDefault(u => u.Name.Equals(pu, StringComparison.InvariantCultureIgnoreCase))))
            {
                user?.SetMaintenanceRole();
            }
        }
    }



}
