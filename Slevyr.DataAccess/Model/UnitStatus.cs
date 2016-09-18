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

        /// <summary>
        /// 1 - stroj stoji
        /// 0 - stroj jede
        /// 255 - stroj ma poruchu 
        /// 2 - Nezname
        /// </summary>
        public short MachineStatus { get; set; }

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
        public bool IsPrestavkaTabule { get; set; }
        public int CilKusuTabule { get; set; }
        public float CilDefectTabule { get; set; }
        public float AktualDefectTabule { get; set; }
        public string AktualDefectTabuleTxt => float.IsNaN(AktualDefectTabule) ? "-" : Math.Round(((decimal)Ng / (decimal)Ok) * 100, 2).ToString(CultureInfo.CurrentCulture);

        public int RozdilTabule { get; set; }
        public string RozdilTabuleTxt => (RozdilTabule == int.MinValue) ? "-" : RozdilTabule.ToString();

        public bool IsTabuleOk { get; set; }

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
                //zatim jen pro typ A

                Logger.Debug($"+ unit {unitConfig.Addr}");
                DateTime dateTimeNow = DateTime.Now;

                if (!IsOkNg)
                {
                    IsTabuleOk = false;
                    Logger.Error("nelze spočítat tabuli");
                    return;
                }

                //pocet sekund od zacátku dne
                int secondsFromMidn = dateTimeNow.Second + dateTimeNow.Minute * 60 + dateTimeNow.Hour * 3600;

                //pocet sekund celého dne
                //int allDaySec = 24 * 60 * 60;

                //pocet sec. prestavky, nyni prestavka 30min - udelat jako parametr ?
                int prestavkaSec = 30 * 60;
                //8hod - 0.5hod. prestavka
                int delkaSmenySec = 27000;

                int zacatekSmeny1Sec = (int)unitConfig.Zacatek1SmenyTime.TotalSeconds;
                int zacatekSmeny2Sec = (int)unitConfig.Zacatek2SmenyTime.TotalSeconds;
                int zacatekSmeny3Sec = (int)unitConfig.Zacatek3SmenyTime.TotalSeconds;

                int zacatekPrestavkySmeny1Sec = (int)unitConfig.Prestavka1SmenyTime.TotalSeconds;
                int zacatekPrestavkySmeny2Sec = (int)unitConfig.Prestavka2SmenyTime.TotalSeconds;
                int zacatekPrestavkySmeny3Sec = (int)unitConfig.Prestavka3SmenyTime.TotalSeconds;
                int konecPrestavkySmeny1Sec = zacatekPrestavkySmeny1Sec + prestavkaSec;
                int konecPrestavkySmeny2Sec = zacatekPrestavkySmeny2Sec + prestavkaSec;
                int konecPrestavkySmeny3Sec = zacatekPrestavkySmeny3Sec + prestavkaSec;

                int odZacatkuSmenySec = 0;

                IsPrestavkaTabule = false;

                int pracovniCasSec = 0;

                Logger.Debug($"actual time is {dateTimeNow}");

                if (secondsFromMidn > zacatekSmeny1Sec && secondsFromMidn < zacatekSmeny2Sec)  //smena1
                {
                    string o;
                    if (secondsFromMidn < zacatekPrestavkySmeny1Sec)
                    {
                        IsPrestavkaTabule = false;
                        pracovniCasSec = secondsFromMidn - zacatekSmeny1Sec;
                        o = "c1";
                    }
                    else if (secondsFromMidn > zacatekPrestavkySmeny1Sec && secondsFromMidn < konecPrestavkySmeny1Sec)
                    {
                        IsPrestavkaTabule = true;
                        pracovniCasSec = 0;
                        o = "pr";
                    }
                    else
                    {
                        IsPrestavkaTabule = false;
                        pracovniCasSec = zacatekPrestavkySmeny1Sec - zacatekSmeny1Sec + secondsFromMidn - konecPrestavkySmeny1Sec;
                        o = "c2";
                    }
                    CilKusuTabule = unitConfig.Cil1Smeny;
                    CilDefectTabule = unitConfig.Def1Smeny;
                    Logger.Debug("smena1 "+o);
                }                
                else if (secondsFromMidn > zacatekSmeny2Sec && secondsFromMidn < zacatekSmeny3Sec) //smena 2
                {
                    string o;
                    if (secondsFromMidn < zacatekPrestavkySmeny2Sec)
                    {
                        IsPrestavkaTabule = false;
                        pracovniCasSec = secondsFromMidn - zacatekSmeny2Sec;
                        o = "c1";
                    }
                    else if (secondsFromMidn > zacatekPrestavkySmeny2Sec && secondsFromMidn < konecPrestavkySmeny2Sec)
                    {
                        IsPrestavkaTabule = true;
                        pracovniCasSec = 0;
                        o = "pr";
                    }
                    else
                    {
                        IsPrestavkaTabule = false;
                        pracovniCasSec = zacatekPrestavkySmeny2Sec - zacatekSmeny2Sec + secondsFromMidn - konecPrestavkySmeny2Sec;
                        o = "c2";
                    }
                    CilKusuTabule = unitConfig.Cil2Smeny;
                    CilDefectTabule = unitConfig.Def2Smeny;
                    Logger.Debug("smena2 " + o);
                }
                else if (secondsFromMidn > zacatekSmeny3Sec || secondsFromMidn < zacatekSmeny1Sec) //smena 3
                {
                    string o;
                    if (secondsFromMidn > zacatekSmeny3Sec)
                    {
                        IsPrestavkaTabule = false;
                        pracovniCasSec = secondsFromMidn - zacatekSmeny3Sec;
                        o = "c1";
                    }
                    else if (secondsFromMidn < zacatekPrestavkySmeny3Sec)
                    {
                        IsPrestavkaTabule = false;
                        pracovniCasSec = 86400 - zacatekSmeny3Sec + secondsFromMidn;
                        o = "c2 (po pulnoci)";
                    }
                    else if (secondsFromMidn >= zacatekPrestavkySmeny3Sec && secondsFromMidn < konecPrestavkySmeny3Sec)
                    {
                        IsPrestavkaTabule = true;
                        pracovniCasSec = 0;
                        o = "pr";
                    }
                    else
                    {
                        IsPrestavkaTabule = false;
                        pracovniCasSec = 86400 - zacatekSmeny3Sec + zacatekPrestavkySmeny3Sec + secondsFromMidn - konecPrestavkySmeny3Sec;
                        o = "c3";
                    }
                    CilKusuTabule = unitConfig.Cil3Smeny;
                    CilDefectTabule = unitConfig.Def3Smeny;
                    Logger.Debug("smena3 " + o);
                }

                if (!IsPrestavkaTabule)
                {
                    
                    try
                    {
                        double casNa1Kus = (double)delkaSmenySec / (double)CilKusuTabule;
                        var aktualniCil = pracovniCasSec / casNa1Kus;
                        RozdilTabule = (int)Math.Round(Ok - aktualniCil);
                    }
                    catch (Exception)
                    {
                        RozdilTabule = int.MinValue;
                    }
                    
                    try
                    {
                        AktualDefectTabule = (float)Ng / (float)Ok;
                    }
                    catch (Exception)
                    {
                        AktualDefectTabule = float.NaN;
                    }
                }

                Logger.Debug($"- unit {unitConfig.Addr}");

                IsTabuleOk = true;
            }
            catch (Exception ex)
            {
                IsTabuleOk = false;
                Logger.Error(ex);                
            }
        }

        #endregion
    }
}
