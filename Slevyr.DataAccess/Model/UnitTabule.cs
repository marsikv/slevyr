using System;
using System.Globalization;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitTabule
    {
        public string LinkaName { get; set; }

        public int Addr { get; set; }

        public int CilKusuTabule { get; set; }

        public float CilDefectTabule { get; set; }

        public string CilDefectTabuleStr => CilDefectTabule.ToString(CultureInfo.InvariantCulture);

        public string CilDefectTabuleTxt => CilDefectTabule.ToString(CultureInfo.CurrentCulture);

        public float AktualDefectTabule { get; set; }

        public string AktualDefectTabuleTxt { get; set; }

        public string AktualDefectTabuleStr { get; set; }

        public int RozdilTabule { get; set; }

        public string RozdilTabuleTxt => (RozdilTabule == int.MinValue) ? "-" : RozdilTabule.ToString();

        /// <summary>
        /// Stav stroje
        /// </summary>
        public MachineStateEnum MachineStatus { get; set; }

        public string MachineStatusTxt => Helper.GetDescriptionFromEnumValue(MachineStatus);

        /// <summary>
        /// Jak dlouho stroj stoji v sec
        /// </summary>
        public int MachineStopDuration { get; set; }

        /// <summary>
        /// Zaznamena cas kdu byla zjistena porucha
        /// </summary>
        public DateTime? MachineStopTime { get; set; }

        public string MachineStopTimeTxt => MachineStopTime?.ToShortTimeString() ?? "-";

        public bool IsPrestavkaTabule { get; set; }

        public UnitTabule()
        {
            AktualDefectTabuleTxt = "-";
            MachineStatus = MachineStateEnum.NotAvailable;
        }

    }
}
