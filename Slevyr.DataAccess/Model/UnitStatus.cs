using System;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Globalization;
using NLog;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{

    public enum MachineStateEnum
    {
        [Description("Výroba")]
        Vyroba = 0,
        [Description("Přerušení výroby")]
        Preruseni = 1,
        [Description("Stop stroje")]
        Stop =2,
        [Description("Změna modelu")]
        ZmenaModelu = 3,
        [Description("Porucha")]
        Porucha = 4,
        [Description("Servis")]
        Servis =5,
        [Description("-")]
        NotAvailable = 90,  //nedari se zjistit stav (komunikační problémy...)
        [Description("Neznámý stav")]
        Neznamy =99
    }

    /// <summary>
    /// směny, hodnota je příkaz pro zjištění posledních hondot ok, ng
    /// </summary>
    public enum SmenyEnum
    {
        [Description("Smena 1 - ranni")]
        Smena1 = UnitMonitor.CmdReadStavCitacuRanniSmena,
        [Description("Smena 2 - odpoledni")]
        Smena2 = UnitMonitor.CmdReadStavCitacuOdpoledniSmena,
        [Description("Smena 3 - nocni")]
        Smena3 = UnitMonitor.CmdReadStavCitacuNocniSmena,
        Nedef = 0,
    }

    public class UnitStatus
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region properties

        public UnitTabule Tabule { get; private set; }

        public int Addr { get; set; }

        private SmenyEnum CurrentSmena { get; set; }

        public int Ok { get; set; }

        public int Ng { get; set; }             

        public DateTime LastCheckTime { get; set; }
        public string LastCheckTimeTxt => LastCheckTime.ToShortTimeString();

        public DateTime ErrorTime { get; set; }
        public string ErrorTimeTxt => ErrorTime.ToString(CultureInfo.CurrentCulture);

        public DateTime OkNgTime { get; set; }
        public string OkNgTimeTxt => OkNgTime.ToShortTimeString();

        public bool IsOkNg { get; set; }
        /// <summary>
        /// čas posledního OK kusu, Sekundy na desetiny
        /// </summary>
        public float CasOk { get; set; }

        /// <summary>
        /// Průměrný čas OK kusu, sekundy na desetiny
        /// </summary>
        public float AvgCasOk { get; set; }

        /// <summary>
        /// Průměrný čas NG kusu, sekundy na desetiny
        /// </summary>
        public float AvgCasNg { get; set; }

        public string CasOkStr => CasOk.ToString(CultureInfo.InvariantCulture);

        public float PrumCasVyrobyOk => (Ok != 0) ? UbehlyCasSmenySec /(float) Ok:float.NaN;

        public string PrumCasVyrobyOkStr => (Ok != 0) ? (UbehlyCasSmenySec / (float)Ok).ToString(CultureInfo.InvariantCulture) : "null";

        public float PrumCasVyrobyNg => (Ng != 0) ? UbehlyCasSmenySec / (float)Ng : float.NaN;

        public string PrumCasVyrobyNgStr => (Ng != 0) ? (UbehlyCasSmenySec / (float)Ng).ToString(CultureInfo.InvariantCulture) : "null";

        public DateTime CasOkNgTime { get; set; }

        public bool IsCasOkNg { get; set; }
        /// <summary>
        /// čas posledního NG kusu, Sekundy na desetiny
        /// </summary>
        public float CasNg { get; set; }
        public string CasNgStr => CasNg.ToString(CultureInfo.InvariantCulture);
        //public DateTime CasNgTime { get; set; }
        //public bool IsCasNg { get; set; }
        public int RozdilKusu { get; set; }  
        public DateTime RozdilKusuTime { get; set; }
        public bool IsRozdilKusu { get; set; }
        public float Defektivita { get; set; }
        public DateTime DefektivitaTime { get; set; }
        public bool IsDefektivita { get; set; }
       
        /// <summary>
        /// cas v sec. ktery uz aktualni smene ubehl
        /// </summary>
        public int UbehlyCasSmenySec { get; set; }

        public bool IsTabuleOk { get; set; }
        public byte MinOk { get; set; }
        public byte MinNg { get; set; }
        public byte VerzeSw1 { get; set; }
        public byte VerzeSw2 { get; set; }
        public byte VerzeSw3 { get; set; }

        /// <summary>
        /// Pocet OK ktery se zaznamena po konci smeny
        /// </summary>
        public short LastOk { get; set; }
        /// <summary>
        /// Pocet NG ktery se zaznamena po konci smeny
        /// </summary>
        public short LastNg { get; set; }
        //public MachineStateEnum LastMachineStatus { get; set; }
        //public short LastMachineStopTime { get; set; }

        #endregion

        #region events
        /// <summary>
        /// event se vyhazuje po prechodu z jedne smeny na druhy (napr. z ranni na odpoledni)
        /// tzn. aktualni smena konci (vraci se jako parametr) a zacina nova
        /// </summary>
        public event EventHandler<SmenyEnum> PrechodSmeny;

        #endregion

        public UnitStatus()
        {
            CurrentSmena = SmenyEnum.Nedef;
            Tabule = new UnitTabule();
        }

        #region methods

        //pocet sekund celého dne
        private const int AllDaySec = 24 * 60 * 60;  //86400
        //pocet sec. prestavky, nyni prestavka 30min - udelat jako parametr ?
        private const int PrestavkaSec = 30 * 60;
        //8hod - 0.5hod. prestavka
        private const int DelkaSmenySec = 27000;


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

                //pocet sekund od zacátku dne (sekundy od pulnoci)
                int timeSec = dateTimeNow.Second + dateTimeNow.Minute * 60 + dateTimeNow.Hour * 3600;

                int zacatekSmeny1Sec = (int)unitConfig.Zacatek1SmenyTime.TotalSeconds;
                int zacatekSmeny2Sec = (int)unitConfig.Zacatek2SmenyTime.TotalSeconds;
                int zacatekSmeny3Sec = (int)unitConfig.Zacatek3SmenyTime.TotalSeconds;

                int zacatekPrestavkySmeny1Sec = (int)unitConfig.Prestavka1SmenyTime.TotalSeconds;
                int zacatekPrestavkySmeny2Sec = (int)unitConfig.Prestavka2SmenyTime.TotalSeconds;
                int zacatekPrestavkySmeny3Sec = (int)unitConfig.Prestavka3SmenyTime.TotalSeconds;
                int konecPrestavkySmeny1Sec = zacatekPrestavkySmeny1Sec + PrestavkaSec;
                int konecPrestavkySmeny2Sec = zacatekPrestavkySmeny2Sec + PrestavkaSec;
                int konecPrestavkySmeny3Sec = zacatekPrestavkySmeny3Sec + PrestavkaSec;

                //int odZacatkuSmenySec = 0;

                Tabule.IsPrestavkaTabule = false;

                Logger.Debug($"actual time is {dateTimeNow}");

                SmenyEnum smena = SmenyEnum.Nedef;

                if (timeSec > zacatekSmeny1Sec && timeSec < zacatekSmeny2Sec)  //smena1 = ranni 6-14h
                {
                    if (timeSec < zacatekPrestavkySmeny1Sec)
                    {
                        //C1
                        Tabule.IsPrestavkaTabule = false;
                        UbehlyCasSmenySec = timeSec - zacatekSmeny1Sec;
                    }
                    else if (timeSec > zacatekPrestavkySmeny1Sec && timeSec < konecPrestavkySmeny1Sec)
                    {
                        //prestavka
                        Tabule.IsPrestavkaTabule = true;
                        UbehlyCasSmenySec = zacatekPrestavkySmeny1Sec - zacatekSmeny1Sec;  //cas se zastavil na zacatku prestavky
                    }
                    else
                    {
                        //C2
                        Tabule.IsPrestavkaTabule = false;
                        UbehlyCasSmenySec = timeSec - zacatekSmeny1Sec + (zacatekPrestavkySmeny1Sec - konecPrestavkySmeny1Sec);
                        //UbehlyCasSmenySec = zacatekPrestavkySmeny1Sec - zacatekSmeny1Sec + secondsFromMidn - konecPrestavkySmeny1Sec;
                    }
                    Tabule.CilKusuTabule = unitConfig.Cil1Smeny;
                    Tabule.CilDefectTabule = unitConfig.Def1Smeny;
                    smena = SmenyEnum.Smena1;

                }                
                else if (timeSec > zacatekSmeny2Sec && timeSec < zacatekSmeny3Sec) //smena 2 = odpoledni 14-22h
                {
                    if (timeSec < zacatekPrestavkySmeny2Sec)
                    {
                        //C1
                        Tabule.IsPrestavkaTabule = false;
                        UbehlyCasSmenySec = timeSec - zacatekSmeny2Sec;
                    }
                    else if (timeSec > zacatekPrestavkySmeny2Sec && timeSec < konecPrestavkySmeny2Sec)
                    {
                        //prestavka
                        Tabule.IsPrestavkaTabule = true;
                        UbehlyCasSmenySec = zacatekPrestavkySmeny2Sec - zacatekSmeny2Sec;  //cas se zastavil na zacatku prestavky
                    }
                    else
                    {
                        //C2
                        Tabule.IsPrestavkaTabule = false;
                        UbehlyCasSmenySec = zacatekPrestavkySmeny2Sec - zacatekSmeny2Sec + timeSec - konecPrestavkySmeny2Sec;
                    }
                    Tabule.CilKusuTabule = unitConfig.Cil2Smeny;
                    Tabule.CilDefectTabule = unitConfig.Def2Smeny;
                    smena = SmenyEnum.Smena2;

                }
                else if (timeSec > zacatekSmeny3Sec || timeSec < zacatekSmeny1Sec) //smena 3 = nocni 22-6h
                {
                    if (timeSec > zacatekSmeny3Sec)
                    {
                        //C1
                        Tabule.IsPrestavkaTabule = false;
                        UbehlyCasSmenySec = timeSec - zacatekSmeny3Sec;
                    }
                    else if (timeSec < zacatekPrestavkySmeny3Sec)
                    {
                        //C2 po pulnoci
                        Tabule.IsPrestavkaTabule = false;
                        UbehlyCasSmenySec = (AllDaySec - zacatekSmeny3Sec) + timeSec;  //tzn. pripocteme sekundy z min. dne
                    }
                    else if (timeSec >= zacatekPrestavkySmeny3Sec && timeSec < konecPrestavkySmeny3Sec)
                    {
                        //prestavka
                        Tabule.IsPrestavkaTabule = true;
                        UbehlyCasSmenySec = 0;
                    }
                    else
                    {
                        //C3
                        Tabule.IsPrestavkaTabule = false;
                        UbehlyCasSmenySec = 86400 - zacatekSmeny3Sec + zacatekPrestavkySmeny3Sec + timeSec - konecPrestavkySmeny3Sec;
                    }
                    Tabule.CilKusuTabule = unitConfig.Cil3Smeny;
                    Tabule.CilDefectTabule = unitConfig.Def3Smeny;
                    smena = SmenyEnum.Smena3;
                }

                Logger.Debug(smena);

                if (smena != SmenyEnum.Nedef && smena != CurrentSmena)  //dochazi ke zmene smeny
                {
                    if (CurrentSmena != SmenyEnum.Nedef)  //aby bylo zajisteno ze se opravdu jedna o prechod z jedne smeny do druhe 
                    {
                        //udalost oznamuje ze aktualni smena konci
                        //po odchyceni se po definovanem zpozdeni zaradi prikaz k nacteni stavu do fronty adhoc prikazu
                        PrechodSmeny?.Invoke(this, CurrentSmena);  
                    }
                    CurrentSmena = smena;
                }


                //if (!IsPrestavkaTabule)   //issue https://github.com/marsikv/slevyr/issues/42
                {
                    try
                    {
                        double casNa1Kus = (double)DelkaSmenySec / (double)Tabule.CilKusuTabule;
                        var aktualniCil = UbehlyCasSmenySec / casNa1Kus;
                        Tabule.RozdilTabule = (int)Math.Round(Ok - aktualniCil);
                    }
                    catch (Exception ex)
                    {
                        Tabule.RozdilTabule = int.MinValue;
                        Logger.Error(ex);
                    }

                    try
                    {
                        Tabule.AktualDefectTabule = (float)Ng / (float)Ok;
                    }
                    catch (Exception)
                    {
                        Tabule.AktualDefectTabule = float.NaN;
                    }

                    Tabule.AktualDefectTabuleTxt = (float.IsNaN(Tabule.AktualDefectTabule) || Ok == 0) ? "-" : Math.Round(((decimal)Ng / (decimal)Ok) * 100, 2).ToString(CultureInfo.CurrentCulture);

                    Tabule.AktualDefectTabuleStr = (float.IsNaN(Tabule.AktualDefectTabule) || Ok == 0) ? "null" : Math.Round(((decimal)Ng / (decimal)Ok) * 100, 2).ToString(CultureInfo.InvariantCulture);

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

        public void SetStopTime(MachineStateEnum machineStatus)
        {
            if (machineStatus == MachineStateEnum.Porucha && Tabule.MachineStatus != machineStatus)
            {
                //zaznamenam cas kdy jsme poruchu zaznamenali
                Tabule.MachineStopTime = DateTime.Now;
            }
            else if (machineStatus == MachineStateEnum.Vyroba)
            {
                //kdyz uz je normalni tak stop time resetuju
                Tabule.MachineStopTime = null;
            }
        }
    }
}
