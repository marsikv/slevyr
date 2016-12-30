using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slevyr.DataAccess.Model;

namespace Slevyr.WebAppHost
{
    public static class Mock
    {
        public static UnitStatus MockUnitStatus()
        {
            return new UnitStatus()
            {
                Ok = 111,
                Ng = 11,
                CasOk = (float)5.603,
                CasNg = (float)1.54,
                //SendError = false
            };
        }

    }
}
