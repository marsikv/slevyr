using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Model;
using Slevyr.DataAccess.Services;
using Slevyr.WebAppHost.Properties;

namespace Slevyr.WebAppHost
{
    /*
    oprávnění pro různé uživatele, příkazy jsou v Hex: 
        Host: jen prohlížet
        Uživatel: měnit 10,11,12,13,14,15.
        Administrátor basic: + měnit 3,4,5,7.
        Administrátor advanced: + zbytek.


        nastavit cas 
    */

    public class SlevyrController : ApiController
    {
        #region Fields

        static readonly RunConfig RunConfig;

        static readonly SerialPortConfig PortConfig;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region ctor

        static SlevyrController()
        {
            Logger.Info("+");

            var unitAddrs = Settings.Default.UnitAddrs.Split(';').Select(s => Int32.Parse(s));

            RunConfig = new RunConfig
            {
                IsMockupMode = Settings.Default.MockupMode,
                IsRefreshTimerOn = Settings.Default.IsRefreshTimerOn,
                IsReadOkNgTime = Settings.Default.IsReadOkNgTime,
                RefreshTimerPeriod = Settings.Default.RefreshTimerPeriod,
                WorkerSleepPeriod = Settings.Default.WorkerSleepPeriod,
                RelaxTime = Settings.Default.RelaxTime,
                ReadResultTimeOut = Settings.Default.ReadResultTimeOut,
                SendCommandTimeOut = Settings.Default.SendCommandTimeOut,
                DataFilePath = Settings.Default.JsonFilePath,
                UnitAddrs = unitAddrs,
                IsWriteEmptyToLog = Settings.Default.IsWriteEmptyToLog
            };

            PortConfig = new SerialPortConfig()
            {
                Port = Settings.Default.Port,
                BaudRate = Settings.Default.BaudRate,
                Parity = System.IO.Ports.Parity.None,
                DataBits = 8,
                StopBits = System.IO.Ports.StopBits.One,
                ReceiveLength = 11
            };

            SlevyrService.Init(PortConfig, RunConfig);

            Logger.Info("unit count: " + SlevyrService.UnitCount);

            if (RunConfig.IsRefreshTimerOn) SlevyrService.StartWorker();
            
        }

        #endregion

        #region RunConfig

        [HttpGet]
        public RunConfig GetConfig()
        {
            Logger.Info("+");
 
            return RunConfig;
        }


        [HttpGet]
        public bool SetConfig([FromUri] bool isMockupMode,[FromUri] bool isTimerOn, [FromUri] int refreshTimerPeriod, [FromUri] bool readCasOkNg)
        {
            Logger.Info($"isMockupMode: {isMockupMode}, isTimerOn: {isTimerOn},timerPeriod: {refreshTimerPeriod}");
            RunConfig.IsMockupMode = isMockupMode;
            RunConfig.IsRefreshTimerOn = isTimerOn;
            RunConfig.RefreshTimerPeriod = refreshTimerPeriod;
            RunConfig.IsReadOkNgTime = readCasOkNg;
            //RunConfig.PortReadTimeout = portReadTimeout;
            //RunConfig.RelaxTime = relaxTime;

            if (isTimerOn)
                SlevyrService.StartWorker();
            else
                SlevyrService.StopWorker();

            return true;
        }

        #endregion

        #region open close port

        [HttpGet]
        public bool OpenPort()
        {
            Logger.Info("+");
            try
            {
                if (RunConfig.IsMockupMode) return true;
                SlevyrService.OpenPort();
               
                Logger.Info($"Port name:{PortConfig.Port} isOpen:{SlevyrService.SerialPortIsOpen}");
                return SlevyrService.SerialPortIsOpen;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        [HttpGet]
        public bool ClosePort()
        {
            Logger.Info("+");

            SlevyrService.ClosePort();

            return !SlevyrService.SerialPortIsOpen;
        }

        #endregion

        #region UnitStatus operations

        [HttpGet]
        public UnitStatus Status([FromUri] byte addr)
        {
            Logger.Info("+");
            if (RunConfig.IsMockupMode) return Mock.MockUnitStatus();

            try
            {
                return SlevyrService.Status(addr);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public UnitStatus RefreshStatus([FromUri] byte addr)
        {
            Logger.Info($"+ {addr}");
            //if (RunConfig.IsMockupMode) return Mock.MockUnitStatus();

            try
            {
                return SlevyrService.RefreshStatus(addr);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public UnitStatus GetStatus([FromUri] byte addr)
        {
            Logger.Info($"+ {addr}");
            //if (RunConfig.IsMockupMode) return Mock.MockUnitStatus();

            try
            {
                return SlevyrService.Status(addr);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }


        [HttpGet]
        public bool NastavJednotku([FromUri] byte addr, [FromUri] bool writeProtectEEprom, [FromUri] byte minOK, [FromUri] byte minNG, 
            [FromUri] bool bootloaderOn, [FromUri] byte parovanyLED,
            [FromUri] byte rozliseniCidel, [FromUri] byte pracovniJasLed)
        {
            Logger.Info($"addr:{addr} writeProtectEEprom:{writeProtectEEprom} minOK:{minOK} minNG:{minNG} parovanyLED:{parovanyLED}");

            if (RunConfig.IsMockupMode) return true;

            try
            {
                //bool writeProtectEEpromVal = String.Equals(writeProtectEEprom, "ANO",StringComparison.InvariantCultureIgnoreCase) ;
                //bool bootloaderOnVal = String.Equals(bootloaderOn, "ANO",StringComparison.InvariantCultureIgnoreCase);

                return SlevyrService.NastavStatus(addr, writeProtectEEprom, minOK, minNG, bootloaderOn, parovanyLED, rozliseniCidel, pracovniJasLed);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavPrestavkySmen([FromUri] byte addr, [FromUri] char varianta, [FromUri] string prest1, [FromUri] string prest2, [FromUri] string prest3)
        {
            Logger.Info($"addr:{addr}  prest1:{prest1} prest2:{prest2} prest2:{prest2}");

            if (RunConfig.IsMockupMode) return true;

            var p1 = TimeSpan.Parse(prest1);
            var p2 = TimeSpan.Parse(prest2);
            var p3 = TimeSpan.Parse(prest3);

            try
            {
                return SlevyrService.NastavPrestavky(addr,varianta, p1, p2, p3);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }


        [HttpGet]
        public bool NastavCileSmen([FromUri] byte addr, [FromUri] char varianta, [FromUri] short cil1, [FromUri] short cil2, [FromUri] short cil3)
        {
            Logger.Info($"addr:{addr} var:{varianta} cil1:{cil1} cil2:{cil2} cil3:{cil3}");

            bool res = false;

            if (RunConfig.IsMockupMode) return true;

            try
            {
                return SlevyrService.NastavCileSmen(addr, varianta, cil1, cil2, cil3);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
            return res;
        }

        [HttpGet]
        public bool NastavOkNg([FromUri] byte addr, [FromUri] short ok, [FromUri] short ng)
        {
            Logger.Info($"addr:{addr} ok:{ok} ng:{ng}");

            if (RunConfig.IsMockupMode) return true;

            try
            {
                return SlevyrService.NastavOkNg(addr, ok, ng);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavAktualniCas([FromUri] byte addr)
        {
            Logger.Info("+");

            if (RunConfig.IsMockupMode) return true;

            try
            {
                return SlevyrService.NastavAktualniCas(addr);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavDefektivitu([FromUri] byte addr, [FromUri]char varianta, [FromUri]string def1, [FromUri]string def2, [FromUri]string def3)
        {
            Logger.Info($"addr:{addr} varianta:{varianta} def1:{def1} def2:{def2} def3:{def3}");

            if (RunConfig.IsMockupMode) return true;

            double def1Val; 
            double def2Val;
            double def3Val;

            if (!(double.TryParse(def1, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out def1Val) && 
                double.TryParse(def2, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out def2Val) && 
                double.TryParse(def3, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out def3Val)))
            {
                throw new ArgumentException("Neplatná hodnota pro cíl");
            }

            try
            {
                Logger.Info($"cil1:{def1Val}");
                return SlevyrService.NastavDefektivitu(addr, varianta, (short)(def1Val * 10), (short)(def2Val * 10), (short)(def3Val * 10));
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavHandshake([FromUri] byte addr, [FromUri] bool value)
        {
            Logger.Info($"addr:{addr} val:{value}");

            if (RunConfig.IsMockupMode) return true;

            try
            {

                return SlevyrService.NastavHandshake(addr, value);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

        }

        [HttpGet]
        public bool CtiStavCitacu([FromUri] byte addr)
        {
            Logger.Info("+");

            if (RunConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().Ok++;
                Mock.MockUnitStatus().Ng++;
                return true;
            }

            try
            {
                return SlevyrService.CtiStavCitacu(addr);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

        }

        [HttpGet]
        public bool CtiCyklusOkNg([FromUri] byte addr)
        {
            Logger.Info("+");

            if (RunConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().CasOkTime = DateTime.Now;
                Mock.MockUnitStatus().CasNgTime = DateTime.Now;
                return true;
            }

            try
            {
                float ok;
                float ng;
                return SlevyrService.CtiCyklusOkNg(addr);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

        }

        #endregion


        [HttpPost]
        public void SaveUnitConfig([FromBody] UnitConfig unitCfg)
        {
            Logger.Info($"Addr:{unitCfg.Addr}");

            try
            {
                SlevyrService.SaveUnitConfig(unitCfg);

            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public UnitConfig GetUnitConfig([FromUri] byte addr)
        {
            Logger.Info($"Addr:{addr}");

            try
            {
                var res = SlevyrService.GetUnitConfig(addr);
                return res;
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }



    }
}
