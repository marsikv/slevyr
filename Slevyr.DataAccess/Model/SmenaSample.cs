using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slevyr.DataAccess.Model
{
    /// <summary>
    /// pro ulozeni vzorku hodnot v ramci smeny pro potreby grafu
    /// </summary>
    public class SmenaSample
    {
        public TimeSpan sampleTime { get; set; }
        public int OK { get; set; }
        public int NG { get; set; }
        public float PrumCasVyrobyOk { get; set; }
        public float PrumCasVyrobyNg { get; set; }
        public float Defectivita { get; set; }

    }
}

