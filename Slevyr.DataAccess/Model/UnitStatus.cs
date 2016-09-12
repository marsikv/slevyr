using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Slevyr.DataAccess.Model
{
    public class UnitStatus
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region properties

        public bool SendError { get; set; }
        public string LastSendErrorDescription { get; set; }

        public short Ok { get; set; }
        public short Ng { get; set; }

        public DateTime LastCheckTime { get; set; }
        public string LastCheckTimeTxt => LastCheckTime.ToShortTimeString();

        public DateTime ErrorTime { get; set; }
        public string ErrorTimeTxt => ErrorTime.ToString(CultureInfo.CurrentCulture);

        public bool Handshake { get; set; }
        public DateTime OkNgTime { get; set; }
        public string OkNgTimeTxt => OkNgTime.ToShortTimeString();

        public bool IsOkNg { get; set; }
        public float CasOk { get; set; }
        public DateTime CasOkTime { get; set; }
        public bool IsCasOk { get; set; }
        public float CasNg { get; set; }
        public DateTime CasNgTime { get; set; }
        public bool IsCasNg { get; set; }
        public short RozdilKusu { get; set; }
        public DateTime RozdilKusuTime { get; set; }
        public bool IsRozdilKusu { get; set; }
        public float Defektivita { get; set; }
        public DateTime DefektivitaTime { get; set; }
        public bool IsDefektivita { get; set; }

        public int CilKusuTabule { get; set; }
        public float CilDefectTabule { get; set; }
        public float AktualDefectTabule { get; set; }
        public int RozdilTabule { get; set; }
        
        public bool IsTabule { get; set; }

        #endregion

        public UnitStatus()
        {
        }

        #region methods

        /// <summary>
        /// zjistit jaka je prave ted smena a nastavit cil a defektivitu podle toho
        /// </summary>
        public void RecalcTabule(UnitConfig unitConfig)
        {

            try
            {
                Logger.Debug($"+ unit {unitConfig.Addr}");
                DateTime dateTimeNow = DateTime.Now;

                if (!IsOkNg)
                {
                    IsTabule = false;
                    Logger.Error("nelze spočítat tabuli");
                    return;
                }

                //pocet sekund od zacátku dne
                int secondsFromMidn = dateTimeNow.Second + dateTimeNow.Minute * 60 + dateTimeNow.Hour * 3600;

                //pocet sekund celého dne
                int allDaySec = 24 * 60 * 60;

                //pocet sec. prestavky, nyni prestavka 30min - udelat jako parametr ?
                int prestavkaSec = 30 * 60;

                int zacatekSmeny1Sec = (int)unitConfig.Zacatek1SmenyTime.TotalSeconds;
                int zacatekSmeny2Sec = (int)unitConfig.Zacatek2SmenyTime.TotalSeconds;
                int zacatekSmeny3Sec = (int)unitConfig.Zacatek3SmenyTime.TotalSeconds;

                int zacatekPrestavkySmeny1Sec = (int)unitConfig.Prestavka1SmenyTime.TotalSeconds;
                int zacatekPrestavkySmeny2Sec = (int)unitConfig.Prestavka2SmenyTime.TotalSeconds;
                int zacatekPrestavkySmeny3Sec = (int)unitConfig.Prestavka3SmenyTime.TotalSeconds;

                //výpoèet sekund celé smìny -30 minut na pøestávku * poèet sekund celého dne
                int delkaSmenySec = zacatekPrestavkySmeny2Sec - zacatekPrestavkySmeny1Sec - prestavkaSec;

                int odZacatkuSmenySec = 0;

                Logger.Debug($"actual time is {dateTimeNow}");

                //TODO overit u P.
                if (secondsFromMidn < zacatekPrestavkySmeny3Sec)
                {
                    odZacatkuSmenySec = secondsFromMidn + 2 * 3600;  //Pøed pøestávkou na tøetí smìnì od pùlnoci + 2hodiny ze starého dne
                    CilKusuTabule = unitConfig.Cil3Smeny;
                    CilDefectTabule = unitConfig.Def3Smeny / 10;
                    Logger.Debug("interval i1");

                }
                else if (secondsFromMidn >= zacatekPrestavkySmeny3Sec && secondsFromMidn < zacatekSmeny1Sec)
                {
                    odZacatkuSmenySec = secondsFromMidn - prestavkaSec + 2 * 3600;  //Pøed pøestávkou na tøetí smìnì od pùlnoci po pøestávce + 2hod ze starého dne
                    CilKusuTabule = unitConfig.Cil3Smeny;
                    CilDefectTabule = unitConfig.Def3Smeny / 10;
                    Logger.Debug("interval i2");
                }
                else if (secondsFromMidn >= zacatekSmeny1Sec && secondsFromMidn < zacatekPrestavkySmeny1Sec)
                {
                    odZacatkuSmenySec = secondsFromMidn - zacatekSmeny1Sec;  // Pøed pøestávkou na první smìnì
                    CilKusuTabule = unitConfig.Cil1Smeny;
                    CilDefectTabule = unitConfig.Def1Smeny / 10;
                    Logger.Debug("interval i3");
                }
                else if (secondsFromMidn >= zacatekPrestavkySmeny1Sec && secondsFromMidn < zacatekSmeny2Sec)
                {
                    odZacatkuSmenySec = secondsFromMidn - zacatekSmeny1Sec - prestavkaSec;  // Po pøestávce na první smìnì
                    CilKusuTabule = unitConfig.Cil1Smeny;
                    CilDefectTabule = unitConfig.Def1Smeny / 10;
                    Logger.Debug("interval i4");
                }
                else if (secondsFromMidn >= zacatekSmeny2Sec && secondsFromMidn < zacatekPrestavkySmeny2Sec)
                {
                    odZacatkuSmenySec = secondsFromMidn - zacatekSmeny2Sec;  // Pøed pøestávkou na druhé smìnì
                    CilKusuTabule = unitConfig.Cil2Smeny;
                    CilDefectTabule = unitConfig.Def2Smeny / 10;
                    Logger.Debug("interval i5");
                }
                else if (secondsFromMidn >= zacatekPrestavkySmeny2Sec && secondsFromMidn < zacatekSmeny3Sec)
                {
                    odZacatkuSmenySec = secondsFromMidn - zacatekSmeny2Sec - prestavkaSec;  // Po pøestávce na druhe smìnì
                    CilKusuTabule = unitConfig.Cil2Smeny;
                    CilDefectTabule = unitConfig.Def2Smeny / 10;
                    Logger.Debug("interval i6");
                }
                else if (secondsFromMidn >= zacatekSmeny3Sec && secondsFromMidn < (24 * 3600 - 1))
                {
                    odZacatkuSmenySec = secondsFromMidn - zacatekSmeny3Sec; // Pøed pøestávkou na tøetí smìnì do pùlnoci
                    CilKusuTabule = unitConfig.Cil3Smeny;
                    CilDefectTabule = unitConfig.Def3Smeny / 10;
                    Logger.Debug("interval i7");
                }
                else
                {
                    //  strOdZacatkuSmeny = (strPartOfTheDay) * 86400 + str2Hod * 86400                 '  Pøed pøestávkou na tøetí smìnì od pùlnoci + 2hodiny ze starého dne
                    Logger.Debug("interval i8 - nedef");
                }

                //Pocet sekund na 1 kus
                //var prumCasKus = delkaSmenySec / unitConfig.Cil1Smeny;
                //TODO overit u Patrika
                var prumCasKus = delkaSmenySec / CilKusuTabule;

                var ocekavanyPocOk = (int)(odZacatkuSmenySec / prumCasKus);

                RozdilTabule = Ok - ocekavanyPocOk;

                decimal val = ((decimal)Ng / (decimal)Ok) * 100;

                AktualDefectTabule = (float)Math.Round(val, 2);

                Logger.Debug($"- unit {unitConfig.Addr}");

                IsTabule = true;
            }
            catch (Exception ex)
            {
                IsTabule = false;
                Logger.Error(ex);                
            }
        }

        #endregion
    }
}
