using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.DAO;
using Slevyr.DataAccess.Model;
using Slevyr.DataAccess.Services;

namespace Slevyr.WebAppHost
{

    public class SlevyrController : ApiController
    {
        #region Fields

        static readonly string ApiVersion = "1.1";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region ctor

        static SlevyrController()
        {
            Logger.Info("+");     
        }

        #endregion

        #region RunConfig

        /// <summary>
        /// Vrací verzi API
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public String GetApiVersion()
        {
            Logger.Info("+");
            
            return ApiVersion;
        }

        /// <summary>
        /// Vrací parametry běhové konfigurace, jako je umístění databáze, hodnoty timout parametrů apod.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public RunConfig GetConfig()
        {
            Logger.Info("+");
 
            return Globals.RunConfig;
        }


        [HttpGet]
        public bool SetConfig([FromUri] bool isMockupMode,[FromUri] bool isTimerOn, [FromUri] int refreshTimerPeriod, [FromUri] bool readCasOkNg)
        {
            Logger.Info($"isMockupMode: {isMockupMode}, isTimerOn: {isTimerOn},timerPeriod: {refreshTimerPeriod}");
            Globals.RunConfig.IsMockupMode = isMockupMode;
            Globals.RunConfig.IsRefreshTimerOn = isTimerOn;
            Globals.RunConfig.RefreshTimerPeriod = refreshTimerPeriod;
            Globals.RunConfig.IsReadOkNgTime = readCasOkNg;
            //RunConfig.PortReadTimeout = portReadTimeout;
            //RunConfig.RelaxTime = relaxTime;

            if (isTimerOn)
                SlevyrService.StartSendWorker();
            else
                SlevyrService.StopSendWorker();

            SlevyrService.StartPacketWorker();

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
                if (Globals.RunConfig.IsMockupMode) return true;
                SlevyrService.OpenPort();

                Logger.Info($"Port name:{Globals.PortConfig.Port} isOpen:{SlevyrService.SerialPortIsOpen}");
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

        //[HttpGet]
        //public UnitStatus Status([FromUri] byte addr)
        //{
        //    Logger.Info("+");
        //    if (Globals.RunConfig.IsMockupMode) return Mock.MockUnitStatus();

        //    try
        //    {
        //        return SlevyrService.GetUnitStatus(addr);
        //    }
        //    catch (KeyNotFoundException)
        //    {
        //        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
        //    }
        //}

        [HttpGet]
        public IEnumerable<UnitTabule> GetAllTabule()
        {
            Logger.Info("+");
            //if (Globals.RunConfig.IsMockupMode) return Mock.MockUnitStatus();

            return SlevyrService.GetAllTabule();

        }
      

        [HttpGet]
        public UnitStatus GetStatus([FromUri] byte addr)
        {
            Logger.Info($"+ {addr}");

            if (Globals.RunConfig.IsMockupMode) return Mock.MockUnitStatus();

            try
            {
                return SlevyrService.GetUnitStatus(addr);
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

            if (Globals.RunConfig.IsMockupMode) return true;

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
        public bool NastavVariantuSmeny([FromUri] byte addr, [FromUri] string varianta)
        {
            Logger.Info($"addr:{addr} NastavVariantuSmeny:{varianta}");

            if (string.IsNullOrWhiteSpace(varianta) || varianta.Trim().Length != 1) 
            {
                throw new ArgumentException();
            }

            byte[] asciiBytes = Encoding.ASCII.GetBytes(varianta.Trim().ToUpper());

            if (Globals.RunConfig.IsMockupMode) return true;

            try
            {
                return SlevyrService.NastavVariantuSmeny(addr, asciiBytes[0]);
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

            if (Globals.RunConfig.IsMockupMode) return true;

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

            if (Globals.RunConfig.IsMockupMode) return true;

            try
            {
                return SlevyrService.NastavCileSmen(addr, varianta, cil1, cil2, cil3);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavOkNg([FromUri] byte addr, [FromUri] short ok, [FromUri] short ng)
        {
            Logger.Info($"addr:{addr} ok:{ok} ng:{ng}");

            if (Globals.RunConfig.IsMockupMode) return true;

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

            if (Globals.RunConfig.IsMockupMode) return true;

            try
            {
                //return SlevyrService.NastavAktualniCas(addr);
                SlevyrService.NastavAktualniCas(addr);
                return true;
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavAktualniCasAllUnits()
        {
            Logger.Info("+");

            if (Globals.RunConfig.IsMockupMode) return true;

            try
            {
                foreach (var addr in Globals.RunConfig.UnitAddrs)
                {
                    SlevyrService.NastavAktualniCas(addr);
                }
                return true;
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

            if (Globals.RunConfig.IsMockupMode) return true;

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
                return SlevyrService.NastavDefektivitu(addr, varianta, (short)(def1Val*10), (short)(def2Val*10), (short)(def3Val*10));
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

            if (Globals.RunConfig.IsMockupMode) return true;

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

            if (Globals.RunConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().Ok++;
                Mock.MockUnitStatus().Ng++;
                return true;
            }

            try
            {
                return SlevyrService.SendCtiStavCitacu(addr);
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

            if (Globals.RunConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().CasOkNgTime = DateTime.Now;
                return true;
            }

            try
            {
                return SlevyrService.SendCtiCyklusOkNg(addr);
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

        [HttpPost]
        public IHttpActionResult ExportInterval([FromBody]IntervalExport value)
        {
            Logger.Info($"exportFilename:{value.FileName} from:{value.TimeFromStr} to:{value.TimeToStr}");
            if (string.IsNullOrWhiteSpace(value.FileName) || string.IsNullOrWhiteSpace(value.TimeFromStr) ||
                string.IsNullOrWhiteSpace(value.TimeToStr))
            {
                return BadRequest("Neplatné parametry");
            }

            var cnt = SqlliteDao.ExportToCsv(value);
            return Ok($"Počet exportovaných záznamů: {cnt}");
        }


    }
}
