using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public List<Tuple<int,int>> Get([FromUri]byte addr,[FromUri] string measureName)
        {
            Logger.Info($"measureName:{measureName} unit addr:{addr}");

            //time=s.sampleTime,measure=s.OK

            try
            {
                return SlevyrService.GetUnitStatus(addr).SmenaSamples.Select(s=>new Tuple<int, int>((int)s.sampleTime.TotalSeconds,s.OK)).ToList();
            }
            catch (Exception)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }
        }


        public static int DateTimeToTimeStamp(DateTime dt)
        {
            return (int)(dt.ToUniversalTime().Subtract(Epoc).TotalSeconds);
        }
   

    }
}