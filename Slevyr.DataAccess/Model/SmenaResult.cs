using System;

namespace Slevyr.DataAccess.Model
{
    public class SmenaResult
    {
        public SmenaResult(UnitStatus unitStatus, UnitConfig unitConfig, SmenyEnum smena)
        {
            Ok = unitStatus.FinalOk >= 0 ? unitStatus.FinalOk : -1;
            Ng = unitStatus.FinalNg >= 0 ? unitStatus.FinalNg : -1; 

            Defektivita = (Ok != 0) ? (float)Ng / (float)Ok * 100 : float.NaN;

            float delkaSmeny =  unitConfig.IsTypSmennostiA ? UnitStatus.DelkaSmenyASec : UnitStatus.DelkaSmenyBSec;

            PrumCyklusOk = (Ok != 0) ? delkaSmeny / (float)Ok : float.NaN;

            double cilKusu;
            switch (smena)
            {
                case SmenyEnum.Smena1:
                    cilKusu = unitConfig.Cil1Smeny;
                    break;
                case SmenyEnum.Smena2:
                    cilKusu = unitConfig.Cil2Smeny;
                    break;
                case SmenyEnum.Smena3:
                    cilKusu = unitConfig.Cil3Smeny;
                    break;
                default:
                    cilKusu = 0;
                    break;
            }

            RozdilKusu = (int)Math.Round(Ok - cilKusu);

            StopTime = unitStatus.FinalMachineStopDuration >= 0 ? 
                new TimeSpan(0,0,unitStatus.FinalMachineStopDuration) : TimeSpan.Zero;
        }

        public int Ok { get; set; }

        public int Ng { get; set; }

        public float PrumCyklusOk { get; set; }
    
        public int RozdilKusu { get; set; }

        public float Defektivita { get; set; }

        public TimeSpan StopTime { get; set; }

        public string StopTimeTxt => StopTime.ToString();
    }
}