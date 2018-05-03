using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using NLog;
using Slevyr.DataAccess.Services;

namespace Slevyr.WebAppHost.Controllers
{
    [Authorize]
    public class SoundController : ApiController
    {


        #region Fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        //private static DateTime Epoc = new DateTime(1970, 1, 1).ToUniversalTime();


        #endregion

        /// <summary>
        /// spusti alarm, ten se ukonci po 15-ti minutach
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public void StartAlarm()
        {
            Logger.Debug($"");
            SoundService.StartAlarm();
        }

        /// <summary>
        /// ukonci alarm pokud bezi
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public void StopAlarm()
        {
            Logger.Debug($"");
            SoundService.StopAlarm();
        }
    }
}
