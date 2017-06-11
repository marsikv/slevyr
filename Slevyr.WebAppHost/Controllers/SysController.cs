using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using NLog;
using Slevyr.DataAccess.DAO;
using Slevyr.DataAccess.Model;
using Slevyr.DataAccess.Services;

namespace Slevyr.WebAppHost.Controllers
{
    /// <summary>
    /// Controler pro systemove operace
    /// </summary>
    [Authorize]
    public class SysController : ApiController
    {
        #region Fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        /// <summary>
        /// Vrací verzi API
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public String GetApiVersion()
        {
            Logger.Info("+");

            return Globals.ApiVersion;
        }

        #region RunConfig

        /// <summary>
        /// Vrací parametry běhové konfigurace, jako je umístění databáze, hodnoty timout parametrů apod.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public RunConfig GetRunConfig()
        {
            Logger.Info("+");

            return Globals.RunConfig;
        }


        /// <summary>
        /// Nastavuje parametry běhové konfigurace
        /// </summary>
        /// <param name="isMockupMode"></param>
        /// <param name="isTimerOn"></param>
        /// <param name="refreshTimerPeriod"></param>
        /// <param name="readCasOkNg"></param>
        /// <returns></returns>
        [HttpGet]
        public bool SetRunConfig([FromUri] bool isMockupMode, [FromUri] bool isTimerOn, [FromUri] int refreshTimerPeriod, [FromUri] bool readCasOkNg)
        {
            Logger.Info($"isMockupMode: {isMockupMode}, isTimerOn: {isTimerOn},timerPeriod: {refreshTimerPeriod}");
            Globals.RunConfig.IsMockupMode = isMockupMode;
            Globals.RunConfig.IsRefreshTimerOn = isTimerOn;
            Globals.RunConfig.RefreshTimerPeriod = refreshTimerPeriod;
            Globals.RunConfig.IsReadOkNgTime = readCasOkNg;
            //RunConfig.PortReadTimeout = portReadTimeout;
            //RunConfig.RelaxTime = relaxTime;

            if (isTimerOn)
                SlevyrService.StartSendReceiveWorkers();
            else
                SlevyrService.StopSendReceiveWorkers();

            SlevyrService.StartPacketWorker();

            return true;
        }

        #endregion

        #region UnitConfig

        /// <summary>
        /// Ulozit aktualni konfiguraci jednotky
        /// </summary>
        /// <param name="addr"></param>
        //[HttpPost]
        //public void SaveUnitConfig([FromBody] UnitConfig unitCfg)
        //{
        //    Logger.Info($"Addr:{unitCfg.Addr}");

        //    try
        //    {
        //        SlevyrService.SaveUnitConfig(unitCfg);

        //    }
        //    catch (KeyNotFoundException)
        //    {
        //        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
        //    }
        //}
        [HttpGet]
        public void SaveUnitConfig([FromUri] byte addr)
        {
            Logger.Info($"Addr:{addr}");

            try
            {
                SlevyrService.SaveUnitConfig(SlevyrService.GetUnitConfig(addr));

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

        [HttpGet]
        public void SaveUnitStatus()
        {
            SlevyrService.SaveAllUnitStatus();
        }

        [HttpGet]
        public void RestoreUnitStatus()
        {
            SlevyrService.RestoreAllUnitStatus();
        }


        #endregion

        /// <summary>
        /// otevre COM port dany konfiguraci
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IHttpActionResult OpenPort()
        {
            Logger.Info("+");
            try
            {
                if (Globals.RunConfig.IsMockupMode) return Ok();
                SlevyrService.OpenPort();

                Logger.Info($"Port name:{Globals.PortConfig.Port} isOpen:{SlevyrService.SerialPortIsOpen}");
                if (SlevyrService.SerialPortIsOpen)
                {
                    return Ok();
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return BadRequest();
                throw;
            }
        }

        /// <summary>
        /// zavre COM port
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IHttpActionResult ClosePort()
        {
            Logger.Info("+");

            SlevyrService.ClosePort();

            return Ok();
        }

        /// <summary>
        /// resetuje RF modul
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IHttpActionResult ResetRf()
        {
            Logger.Info("+");

            SlevyrService.SendResetRf();

            return Ok("odeslán příkaz na reset RF");
        }

    }
}
