using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slevyr.DataAccess.Model
{
    public class IntervalExport
    {
        private string _timeFromStr;
        private string _timeToStr;

        public string FileName { get; set; }
        public string TimeFromStr
        {
            get { return _timeFromStr; }
            set
            {
                _timeFromStr = value;
                TimeFrom = DateTime.Parse(value);
            }
        }

        public string TimeToStr
        {
            get { return _timeToStr; }
            set
            {
                _timeToStr = value;
                TimeTo = DateTime.Parse(value);
            }
        }
        public int UnitId { get; set; }
        public bool ExportAll { get; set; }
        public bool ExportAllSeparated { get; set; }
        public DateTime TimeFrom { get; set; }
        public DateTime TimeTo { get; set; }

        public IntervalExport()
        {
            
        }

        public IntervalExport(string timeFromStr, string timeToStr)
        {
            TimeFrom = string.IsNullOrWhiteSpace(timeFromStr) ? DateTime.Now : DateTime.Parse(timeFromStr);
            TimeTo = string.IsNullOrWhiteSpace(timeToStr) ? DateTime.Now : DateTime.Parse(timeToStr);
        }
    }
}
