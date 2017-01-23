using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using NLog;
using Slevyr.DataAccess.DAO;
using Slevyr.DataAccess.Model;
using Slevyr.DataAccess.Services;

namespace Slevyr.WebAppHost.Controllers
{
    [Authorize]

    public class UdrzbaController : ApiController
    {
        #region Fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        [HttpGet]
        public IEnumerable<UnitTabule> GetAllUdrzba()
        {
            Logger.Info("+");
            //if (Globals.RunConfig.IsMockupMode) return Mock.MockUnitStatus();

            return SlevyrService.GetAllUdrzba();

        }
    }
}
