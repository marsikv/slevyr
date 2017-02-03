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
    /// Provadi export z databaze a vraci CSV jako response
    /// </summary>
    [Authorize]
    public class ExportController : ApiController
    {
        #region Fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        private class ExportActionResult : IHttpActionResult
        {
            public ExportActionResult(IntervalExportDef exportDef, char decimalSeparator)
            {
                _exportDef = exportDef;
                _decimalSeparator = decimalSeparator;
            }

            private readonly IntervalExportDef _exportDef;
            private readonly char _decimalSeparator;

            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                //using (var memStream = new MemoryStream())
                var memStream = new MemoryStream();

                var cnt = SqlliteDao.ExportToCsv(_exportDef, _decimalSeparator, memStream);

                memStream.Position = 0;

                HttpResponseMessage response = new HttpResponseMessage();
                response.Content = new StreamContent(memStream);
                response.Content.Headers.ContentLength = memStream.Length;

                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment"){ FileName = "export.csv" };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

                // NOTE: Here I am just setting the result on the Task and not really doing any async stuff. 
                // But let's say you do stuff like contacting a File hosting service to get the file, then you would do 'async' stuff here.

                return Task.FromResult(response);

                //TODO uzavrit memStream kdy ?
            }
        }


        /// <summary>
        /// Operace provádí export z databáze pro obecně zadaný časový interval, výsledný CSV soubor ulozeny na serveru
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult ExportInterval([FromBody]IntervalExportDef value)
        {
            Logger.Info($"exportFilename:{value.FileName} from:{value.TimeFromStr} to:{value.TimeToStr}");
            if (string.IsNullOrWhiteSpace(value.FileName) || string.IsNullOrWhiteSpace(value.TimeFromStr) ||
                string.IsNullOrWhiteSpace(value.TimeToStr))
            {
                return BadRequest("Neplatné parametry");
            }

            var cnt = SqlliteDao.ExportToCsv(value, Globals.RunConfig.DecimalSeparator, null);
            return Ok($"Počet exportovaných záznamů od {value.TimeFrom} do {value.TimeTo} : {cnt}");
        }

        /// <summary>
        /// Operace provádí export z databáze pro obecně zadaný časový interval, response CSV soubor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// 
        [HttpGet]
        public IHttpActionResult ExportIntervalDownload([FromUri] string fileName, [FromUri] string timeFrom, [FromUri] string timeTo, 
            [FromUri]int unitId, [FromUri]bool expAll, [FromUri]bool expAllSepar)
        {
            Logger.Info($"exportFilename:{fileName} from:{timeFrom} to:{timeTo}");
            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(timeFrom) ||
                string.IsNullOrWhiteSpace(timeTo))
            {
                return BadRequest("Neplatné parametry");
            }

            var exportDef = new IntervalExportDef()
            {
                FileName = fileName,
                UnitId = unitId,
                ExportAllSeparated = expAllSepar,
                ExportAll = expAll,
                TimeToStr = timeTo,
                TimeFromStr = timeFrom
            };

            //using (var memStream = new MemoryStream(strFilePath, FileMode.OpenOrCreate, FileAccess.Write)), Encoding.GetEncoding("ISO-8859-2")))

            return new ExportActionResult(exportDef, Globals.RunConfig.DecimalSeparator);
        }

        /// <summary>
        /// Operace provádí download preddefinovaneho exportu z databáze, vysledný soubor uloží na server
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="unitId"></param>
        /// <param name="expAll"></param>
        /// <param name="expAllSepar"></param>
        /// <param name="exportVariant"></param>
        /// <returns></returns>
        [HttpGet]
        public IHttpActionResult ExportPredef([FromUri] string fileName, [FromUri]int unitId, [FromUri]bool expAll, [FromUri]bool expAllSepar, [FromUri]int exportVariant)
        {
            Logger.Info($"exportFilename:{fileName} exportvar:{exportVariant}");
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Neplatná hodnota pro název souboru");
            }

            var exportDef = new IntervalExportDef()
            {
                ExportAll = expAll,
                UnitId = unitId,
                FileName = fileName,
                ExportAllSeparated = expAllSepar
            };

            DateTime now = DateTime.Now;
            bool moveToPrevious = false;

            switch (exportVariant)
            {
                case 1:
                    exportDef.TimeFrom = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0);
                    exportDef.TimeTo = new DateTime(now.Year, now.Month, now.Day, 14, 0, 0);
                    moveToPrevious = (now.Hour < 14); //smena prave probiha nebo jeste nezacala, musim o den zpet
                    break;
                case 2:
                    exportDef.TimeFrom = new DateTime(now.Year, now.Month, now.Day, 14, 0, 0);
                    exportDef.TimeTo = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
                    moveToPrevious = (DateTime.Now.Hour < 22); //smena prave probiha, musim o den zpet
                    break;
                case 3:
                    exportDef.TimeFrom = new DateTime(now.Year, now.Month, now.Day - 1, 22, 0, 0);
                    exportDef.TimeTo = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0);
                    moveToPrevious = (now.Hour >= 22) || (DateTime.Now.Hour < 6); //smena prave probiha, musim o den zpet
                    break;
                default: throw new ArgumentException("Neplatná hodnota pro variantu exportu");
            }

            if (moveToPrevious)
            {
                exportDef.TimeFrom = exportDef.TimeFrom.AddDays(-1);
                exportDef.TimeTo = exportDef.TimeTo.AddDays(-1);
            }

            var cnt = SqlliteDao.ExportToCsv(exportDef, Globals.RunConfig.DecimalSeparator, null);
            return Ok($"Počet exportovaných záznamů od {exportDef.TimeFrom} do {exportDef.TimeTo} : {cnt} \n{(moveToPrevious ? "Směna probíhá nebo ještě nezačala, export byl proveden za předchozí den !" : "")}");
        }


        /// <summary>
        /// Operace provádí download preddefinovaneho exportu z databáze
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="unitId"></param>
        /// <param name="expAll"></param>
        /// <param name="expAllSepar"></param>
        /// <param name="exportVariant"></param>
        /// <returns>CSV soubor</returns>
        [HttpGet]
        public IHttpActionResult ExportPredefDownload( [FromUri]int unitId, [FromUri]int exportVariant)
        {

            var exportDef = new IntervalExportDef()
            {
                ExportAll = false,
                UnitId = unitId,
                ExportAllSeparated = false
            };

            DateTime now = DateTime.Now;
            bool moveToPrevious = false;

            switch (exportVariant)
            {
                case 1:
                    exportDef.TimeFrom = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0);
                    //exportDef.TimeFrom = new DateTime(now.Year, 1, 1, 6, 0, 0);//debug
                    exportDef.TimeTo = new DateTime(now.Year, now.Month, now.Day, 14, 0, 0);
                    moveToPrevious = (now.Hour < 14); //smena prave probiha nebo jeste nezacala, musim o den zpet
                    break;
                case 2:
                    exportDef.TimeFrom = new DateTime(now.Year, now.Month, now.Day, 14, 0, 0);
                    exportDef.TimeTo = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
                    moveToPrevious = (DateTime.Now.Hour < 22); //smena prave probiha, musim o den zpet
                    break;
                case 3:
                    exportDef.TimeFrom = new DateTime(now.Year, now.Month, now.Day - 1, 22, 0, 0);
                    exportDef.TimeTo = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0);
                    moveToPrevious = (now.Hour >= 22) || (DateTime.Now.Hour < 6); //smena prave probiha, musim o den zpet
                    break;
                default: throw new ArgumentException("Neplatná hodnota pro variantu exportu");
            }

            if (moveToPrevious)
            {
                exportDef.TimeFrom = exportDef.TimeFrom.AddDays(-1);
                exportDef.TimeTo = exportDef.TimeTo.AddDays(-1);
            }

            var res = new ExportActionResult(exportDef, Globals.RunConfig.DecimalSeparator);

            return res;

            //var cnt = SqlliteDao.ExportToCsv(exportDef, Globals.RunConfig.DecimalSeparator);
            //return Ok($"Počet exportovaných záznamů od {exportDef.TimeFrom} do {exportDef.TimeTo} : {cnt} \n{(moveToPrevious ? "Směna probíhá nebo ještě nezačala, export byl proveden za předchozí den !" : "")}");
        }


   

    }
}