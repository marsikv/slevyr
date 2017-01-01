using System.Globalization;

namespace Slevyr.DataAccess.Model
{
    public class UnitTabule
    {
        public string LinkaName { get; set; }

        public int CilKusuTabule { get; set; }

        public float CilDefectTabule { get; set; }

        public string CilDefectTabuleStr => CilDefectTabule.ToString(CultureInfo.InvariantCulture);

        public float AktualDefectTabule { get; set; }

        public string AktualDefectTabuleTxt { get; set; }

        public int RozdilTabule { get; set; }

        public string RozdilTabuleTxt => (RozdilTabule == int.MinValue) ? "-" : RozdilTabule.ToString();

    }
}
