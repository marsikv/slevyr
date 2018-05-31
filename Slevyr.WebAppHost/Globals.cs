using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Globalization;
using System.IO.Ports;
using Newtonsoft.Json;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Model;
using Slevyr.WebAppHost.Model;

namespace Slevyr.WebAppHost
{
    /// <summary>
    /// Globalne dostupne parametry a pole
    /// </summary>
    public static class Globals
    {
        public static readonly string ApiVersion = "1.1.2";

        public static RunConfig RunConfig { get; private set; }
        public static SerialPortConfig PortConfig { get; private set; }

        public static int WebAppPort;

        public static bool UseLocalHost;

        public static bool StartWebApi;

        public static bool StartSwagger;

        public static string WwwRootDir;

        //public static IEnumerable<string> PowerUsers;

        public static List<User> Users;

        static Globals()
        {
        }

        public static void LoadSettings()
        {
           
            RunConfig = new RunConfig
            {
                IsMockupMode = Boolean.Parse(ConfigurationManager.AppSettings["MockupMode"]),
                IsRefreshTimerOn = Boolean.Parse(ConfigurationManager.AppSettings["IsRefreshTimerOn"]),
                IsReadOkNgTime = Boolean.Parse(ConfigurationManager.AppSettings["IsReadOkNgTime"]),
                //IsWriteEmptyToLog = bool.Parse(ConfigurationManager.AppSettings["IsWriteEmptyToLog"]),
                RefreshTimerPeriod = Int32.Parse(ConfigurationManager.AppSettings["RefreshTimerPeriod"]),
                //WorkerSleepPeriod = int.Parse(ConfigurationManager.AppSettings["WorkerSleepPeriod"]),
                RelaxTime = Int32.Parse(ConfigurationManager.AppSettings["RelaxTime"]),
                ReadResultTimeOut = Int32.Parse(ConfigurationManager.AppSettings["ReadResultTimeOut"]),
                SendCommandTimeOut = Int32.Parse(ConfigurationManager.AppSettings["SendCommandTimeOut"]),
                MinCmdDelay = Int32.Parse(ConfigurationManager.AppSettings["MinCmdDelay"]),
                UnitAddrs = ConfigurationManager.AppSettings["UnitAddrs"].Split(';').Select(Int32.Parse),
                JsonDataFilePath = ConfigurationManager.AppSettings["JsonFilePath"],
                DbFilePath = ConfigurationManager.AppSettings["DbFilePath"],
                SendAttempts = Int32.Parse(ConfigurationManager.AppSettings["SendAttempts"]),
                DefaultExportFileName = ConfigurationManager.AppSettings["DefaultExportFileName"],

                HlaseniPrestavekSoundFile = ConfigurationManager.AppSettings["HlaseniPrestavekSoundFile"],
                HlaseniSmenSoundFile = ConfigurationManager.AppSettings["HlaseniSmenSoundFile"],
                PoplachSoundFile = ConfigurationManager.AppSettings["PoplachSoundFile"],              
            };

            bool b;
            int i;
            if (Boolean.TryParse(ConfigurationManager.AppSettings["IsWaitCommandResult"], out b)) RunConfig.IsWaitCommandResult = b;
            if (Boolean.TryParse(ConfigurationManager.AppSettings["UseDataReceivedEvent"], out b)) RunConfig.UseDataReceivedEvent = b;

            if (Boolean.TryParse(ConfigurationManager.AppSettings["TransmitSound"], out b)) RunConfig.TransmitSound = b;

            var s = ConfigurationManager.AppSettings["DecimalSeparator"];
            if (!String.IsNullOrEmpty(s)) RunConfig.DecimalSeparator = s[0];

            if (Int32.TryParse(ConfigurationManager.AppSettings["CycleForScheduledResetRf"], out i)) RunConfig.CycleForScheduledResetRf = i;
            if (Boolean.TryParse(ConfigurationManager.AppSettings["AutoResetRF"], out b)) RunConfig.IsAutoResetRF = b;

            if (Int32.TryParse(ConfigurationManager.AppSettings["GraphSamplePeriod"], out i)) RunConfig.GraphSamplePeriodSec = i;

            if (Int32.TryParse(ConfigurationManager.AppSettings["GraphMinSamplePeriod"], out i)) RunConfig.GraphMinSamplePeriodSec = i;

            Boolean.TryParse(ConfigurationManager.AppSettings["IsWaitCommandResult"], out b); RunConfig.IsWaitCommandResult = b;

            if (Int32.TryParse(ConfigurationManager.AppSettings["ReadStopDurationPeriod"], out i)) RunConfig.ReadStopDurationPeriod = i;

            var dayPattern = ConfigurationManager.AppSettings["SyncUnitTimePeriod"];
            RunConfig.IsSyncUnitTimePeriodEnabled = !string.IsNullOrEmpty(dayPattern);
            if (RunConfig.IsSyncUnitTimePeriodEnabled)
            {

                //otestovat regularnim vyrazem
                //napr. Fri 23:10

                /*
                var dtn = DateTime.Now;
                // napr.  "Sun 08:30 15 Jun 2008"   
                string dateString = $"{dayPattern} {dtn.Day} {dtn.Month} {dtn.Year}";

                try
                {
                    var result = DateTime.ParseExact(dateString, RunConfig.DateTimeFormat, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    Console.WriteLine("{0} is not in the correct format.", dateString);
                    dayPattern = null;
                }
                */

                RunConfig.SyncUnitTimePeriod = dayPattern;
            }

           

            PortConfig = new SerialPortConfig
            {
                Port = ConfigurationManager.AppSettings["Port"],
                BaudRate = Int32.Parse(ConfigurationManager.AppSettings["BaudRate"]),
                ReceivedBytesThreshold = Int32.Parse(ConfigurationManager.AppSettings["ReceivedBytesThreshold"]),
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
            };           

            Boolean.TryParse(ConfigurationManager.AppSettings["StartWebApi"], out StartWebApi);
            Boolean.TryParse(ConfigurationManager.AppSettings["StartSwagger"], out StartSwagger);
            Boolean.TryParse(ConfigurationManager.AppSettings["UseLocalHost"], out UseLocalHost);
            Int32.TryParse(ConfigurationManager.AppSettings["WebAppPort"], out WebAppPort);
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
