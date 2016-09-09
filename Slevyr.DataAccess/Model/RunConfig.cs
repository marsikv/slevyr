using System.Collections.Generic;

namespace Slevyr.DataAccess.Model
{
    public class RunConfig
    {
        public bool IsMockupMode;
        public bool IsRefreshTimerOn;
        public int RefreshTimerPeriod;
        //public int WorkerTimerPeriod;
        public int RelaxTime;
        public int PortReadTimeout;
        public string DataFilePath;
        public IEnumerable<int> UnitAddrs;
    }
}
