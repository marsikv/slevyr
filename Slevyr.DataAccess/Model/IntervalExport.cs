using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slevyr.DataAccess.Model
{
    public class IntervalExport
    {
        public string FileName { get; set; }
        public string TimeFromStr { get; set; }
        public string TimeToStr { get; set; }
        public int UnitId { get; set; }
        public bool ExportAll { get; set; }
        public bool ExportAllSeparated { get; set; }
        public DateTime TimeFrom => string.IsNullOrWhiteSpace(TimeFromStr) ? DateTime.Now: DateTime.Parse(TimeFromStr);
        public DateTime TimeTo => string.IsNullOrWhiteSpace(TimeToStr) ? DateTime.Now : DateTime.Parse(TimeToStr);
    }
}
