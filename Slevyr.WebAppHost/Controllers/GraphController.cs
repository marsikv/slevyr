using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using NLog;
using Slevyr.DataAccess.Model;
using Slevyr.DataAccess.Services;

namespace Slevyr.WebAppHost.Controllers
{
    /// <summary>
    /// Předává x,y pro vykresleni grafu
    /// </summary>
    public struct DataPoint
    {
        public float x;
        public float y;
    }


    /// <summary>
    /// Provadi export z databaze a vraci CSV jako response
    /// </summary>
    [Authorize]
    public class GraphController : ApiController
    {
        #region Fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        private static DateTime Epoc  = new DateTime(1970, 1, 1).ToUniversalTime();


        #endregion

        /// <summary>
        /// vraci data pro graf
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public List<DataPoint> Get([FromUri]byte addr,[FromUri] string measureName)
        {
            //Logger.Debug($"measureName:{measureName} unit addr:{addr}");

            try
            {
                var res = SlevyrService.GetUnitStatus(addr).SmenaSamples.Select(s => new DataPoint()
                {
                    x = (float)s.SampleTime.TotalHours,
                    y = GetValueOfMeasure(s, measureName)
                }).ToList();
                return res;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }
        }

        [HttpGet]
        public List<DataPoint> GetPast([FromUri]byte addr, [FromUri] string measureName, [FromUri] int smena)
        {
            try
            {
                if (SlevyrService.GetUnitStatus(addr)?.PastSmenaResults[smena]?.SmenaSamples == null)
                {
                    return null;
                }
                var res = SlevyrService.GetUnitStatus(addr).PastSmenaResults[smena].SmenaSamples.Select(s => new DataPoint()
                {
                    x = (float)s.SampleTime.TotalHours,
                    y = GetValueOfMeasure(s, measureName)
                }).ToList();
                return res;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }
        }


        private float GetValueOfMeasure(SmenaSample s,string measureName)
        {
            switch (measureName)
            {
                case "OK":
                    return s.OK;
                case "NG":
                    return s.NG;
                case "PrumCasVyrobyOk":
                    return s.PrumCasVyrobyOk;
                case "StavLinky":
                    return (float)s.StavLinky;
                case "Defektivita":
                    return s.Defectivita;
                case "Rozdil":
                    return s.RozdilKusu;
                case "Prestavka":
                    return s.IsPrestavka;
                default:
                    return 0;
            }

        }

   

    }
}