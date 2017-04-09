using System;
using System.ComponentModel;
using System.Globalization;
using NLog;

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
        #region consts
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        //pocet sekund celého dne
        private const int AllDaySec = 24 * 60 * 60;  //86400
        //pocet sec. prestavky, nyni prestavka 30min - udelat jako parametr ?
        private const int PrestavkaSec = 30 * 60;

        private const int Prestavka1Sec = 30 * 60;  //prvni prestavka smennost B
        private const int Prestavka2Sec = 20 * 60;  //druha prestavka smennost B
        
        public const int DelkaSmenyASec = 27000;  //8hod - 30min. prestavka 
        public const int DelkaSmenyBSec = 40200;  //12h - 50min. prestavka

        #endregion

        #region private fields

        //private TimeSpan _cumulativeStopTimeSpan = new TimeSpan();
        //private int _lastMachineStopDuration;

        #endregion

        #region properties

        /// <summary>
        /// celkovy stop time stroje za smenu
        /// </summary>
        //public TimeSpan CumulativeStopTimeSpan => _cumulativeStopTimeSpan;  - tu uz nepotrebuji protoze prikazy 6c,6d,6e vraceji kumulovany stop time

        public UnitTabule Tabule { get; private set; }

        public SmenaResult[] LastSmenaResults 
        {
            get; private set;
        }

        public int Addr { get; set; }

        private SmenyEnum CurrentSmena { get; set; }

        /// <summary>
        /// cislo smeny: 1,2,3
        /// </summary>
        private int CurrentSmenaAsNum => GetSmenaNum(CurrentSmena);

        public int Ok { get; set; }

        public int Ng { get; set; }

        public int FinalOk { get; set; }   //po nacteni stavu konce smeny

        public int FinalNg { get; set; }   //po nacteni stavu konce smeny

        public int FinalMachineStopDuration { get; set; }  // Jak dlouho stroj stoji v sec - po nacteni stavu konce smeny

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

        public string CasOkStr => float.IsNaN(CasOk) ? "null" : CasOk.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Průměrný čas OK kusu, sekundy na desetiny
        /// </summary>
        public float AvgCasOk { get; set; }

        /// <summary>
        /// Průměrný čas NG kusu, sekundy na desetiny
        /// </summary>
        public float AvgCasNg { get; set; }

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
        public string CasNgStr => float.IsNaN(CasNg) ? "null" : CasNg.ToString(CultureInfo.InvariantCulture);

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
        private int UbehlyCasSmenySec { get; set; }

        public bool IsTabuleOk { get; set; }

        public bool IsTypSmennostiA { get; set; }

        public short ZmenaModeluDuration { get; set; }
        public short PoruchaDuration { get; set; }
        public short ServisDuration { get; set; }

        /* -- nevyuzivame
        public byte MinOk { get; set; }
        public byte MinNg { get; set; }
        public byte VerzeSw1 { get; set; }
        public byte VerzeSw2 { get; set; }
        public byte VerzeSw3 { get; set; }
        */

        #endregion

        #region events
        /// <summary>
        /// event se vyhazuje po prechodu z jedne smeny na druhy (napr. z ranni na odpoledni)
        /// tzn. aktualni smena konci (vraci se jako parametr) a zacina nova
        /// </summary>
        public event EventHandler<SmenyEnum> PrechodSmeny;

        #endregion

        #region ctor

        public UnitStatus()
        {
            CurrentSmena = SmenyEnum.Nedef;
            Tabule = new UnitTabule();
            LastSmenaResults = new SmenaResult[3];
            
            PrepareNewSmena();
        }

        #endregion

        #region methods

        public static int GetSmenaNum(SmenyEnum smena)
        {
            switch (smena)
            {
                case SmenyEnum.Smena1:
                    return 1;
                case SmenyEnum.Smena2:
                    return 2;
                case SmenyEnum.Smena3:
                    return 3;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// vola se vzdy pri zacatku nove smeny
        /// zde je mozne inicializace nejaky stavovych property
        /// </summary>
        private void PrepareNewSmena()
        {
            //Ok = 0;
            //Ng = 0;
            //CasOk = float.NaN;
            //CasNg = float.NaN;
            //Defektivita = float.NaN;
            FinalMachineStopDuration = 0;
            FinalOk = 0;
            FinalNg = 0;
            ZmenaModeluDuration = 0;
            PoruchaDuration = 0;
            ServisDuration = 0;
            IsTabuleOk = false;
        }

        private void PrepocetTabule(SmenyEnum smena, bool isTypSmennostiA)
        {
            Logger.Debug(smena);

            if (smena != SmenyEnum.Nedef && smena != CurrentSmena)  //dochazi ke zmene smeny
            {
                if (CurrentSmena != SmenyEnum.Nedef)  //aby bylo zajisteno ze se opravdu jedna o prechod z jedne smeny do druhe 
                {
                    PrepareNewSmena();

                    //udalost oznamuje ze aktualni smena konci
                    //po odchyceni se po definovanem zpozdeni zaradi prikaz k nacteni stavu do fronty adhoc prikazu
                    PrechodSmeny?.Invoke(this, CurrentSmena);
                }
                CurrentSmena = smena;
            }

            try
            {
                double delkaSmeny = isTypSmennostiA ? DelkaSmenyASec : DelkaSmenyBSec;
                double casNa1Kus = delkaSmeny / (double)Tabule.CilKusuTabule;
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
                Tabule.AktualDefectTabule = (float)Ng / (float)Ok * 100;
            }
            catch (Exception)
            {
                Tabule.AktualDefectTabule = float.NaN;
            }

            Tabule.AktualDefectTabuleTxt = (float.IsNaN(Tabule.AktualDefectTabule) || Ok == 0) ? "-" : Math.Round(((decimal)Ng / (decimal)Ok) * 100, 2).ToString(CultureInfo.CurrentCulture);

            Tabule.AktualDefectTabuleStr = (float.IsNaN(Tabule.AktualDefectTabule) || Ok == 0) ? "null" : Math.Round(((decimal)Ng / (decimal)Ok) * 100, 2).ToString(CultureInfo.InvariantCulture);
        }


        public void SetStopTime(MachineStateEnum machineStatus)
        {
            if (machineStatus == MachineStateEnum.Vyroba)
            {
                //_cumulativeStopTimeSpan.Add(new TimeSpan(0, 0, _lastMachineStopDuration));
            }
            else
            {
                //uchovam si posledni zjistenou delku stop stavu 
                //_lastMachineStopDuration = Tabule.MachineStopDuration;
            }

            if (machineStatus == MachineStateEnum.Porucha && Tabule.MachineStatus != machineStatus)
            {
                //zaznamenam cas kdy jsme poruchu zaznamenali
                Tabule.MachineStopTime = DateTime.Now;
            }
            else if (machineStatus == MachineStateEnum.Vyroba && Tabule.MachineStopTime.HasValue)
            {
               
                //kdyz uz je normalni tak stop time resetuju
                Tabule.MachineStopTime = null;
            }
        }


        /// <summary>
        /// zjistit jaka je prave ted smena a nastavit cil a defektivitu podle toho
        /// </summary>
        public void RecalcTabuleA(UnitConfig unitConfig)
        {
            try
            {
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
                        UbehlyCasSmenySec = AllDaySec - zacatekSmeny3Sec + zacatekPrestavkySmeny3Sec + timeSec - konecPrestavkySmeny3Sec;
                    }
                    Tabule.CilKusuTabule = unitConfig.Cil3Smeny;
                    Tabule.CilDefectTabule = unitConfig.Def3Smeny;
                    smena = SmenyEnum.Smena3;
                }

                PrepocetTabule(smena, unitConfig.IsTypSmennostiA);

                Logger.Debug($"- unit {unitConfig.Addr}");

                IsTabuleOk = true;
            }
            catch (Exception ex)
            {
                IsTabuleOk = false;
                Logger.Error(ex);                
            }
        }

        public void RecalcTabuleB(UnitConfig unitConfig)
        {
            try
            {
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

                int zacatek1PrestavkySmeny1Sec = (int)unitConfig.Prestavka1Smeny1Time.TotalSeconds;
                int zacatek1PrestavkySmeny2Sec = (int)unitConfig.Prestavka1Smeny2Time.TotalSeconds;

                int zacatek2PrestavkySmeny1Sec = (int)unitConfig.Prestavka2Smeny1Time.TotalSeconds;
                int zacatek2PrestavkySmeny2Sec = (int)unitConfig.Prestavka2Smeny2Time.TotalSeconds;

                int konec1PrestavkySmeny1Sec = zacatek1PrestavkySmeny1Sec + Prestavka1Sec;
                int konec1PrestavkySmeny2Sec = zacatek1PrestavkySmeny2Sec + Prestavka1Sec;

                int konec2PrestavkySmeny1Sec = zacatek2PrestavkySmeny1Sec + Prestavka2Sec;
                int konec2PrestavkySmeny2Sec = zacatek2PrestavkySmeny2Sec + Prestavka2Sec;

                Tabule.IsPrestavkaTabule = false;

                Logger.Debug($"actual time is {dateTimeNow}");

                SmenyEnum smena = SmenyEnum.Nedef;
                Tabule.IsPrestavkaTabule = false;

                if (timeSec >= zacatekSmeny1Sec && timeSec < zacatekSmeny2Sec)  //smena1 B (denni typicky od 6:00)
                {
                    if (timeSec < zacatek1PrestavkySmeny1Sec)
                    {
                        //C1
                        UbehlyCasSmenySec = timeSec - zacatekSmeny1Sec;
                    }
                    else if (timeSec >= zacatek1PrestavkySmeny1Sec && timeSec < konec1PrestavkySmeny1Sec)
                    {
                        //prestavka 1
                        Tabule.IsPrestavkaTabule = true;
                        UbehlyCasSmenySec = zacatek1PrestavkySmeny1Sec - zacatekSmeny1Sec;  //cas se zastavil na zacatku 1. prestavky
                    }
                    else if (timeSec >= konec1PrestavkySmeny1Sec && timeSec < zacatek2PrestavkySmeny1Sec)
                    {
                        //C2
                        UbehlyCasSmenySec = timeSec - zacatekSmeny1Sec - Prestavka1Sec;  //odectu cas 1. prestavky
                    }
                    else if (timeSec >= zacatek2PrestavkySmeny1Sec && timeSec < konec2PrestavkySmeny1Sec)
                    {
                        //prestavka 2
                        Tabule.IsPrestavkaTabule = true;
                        UbehlyCasSmenySec = zacatek2PrestavkySmeny1Sec - zacatekSmeny1Sec;  //cas se zastavil na zacatku 2. prestavky
                    }
                    else if (timeSec >= konec2PrestavkySmeny1Sec && timeSec < zacatekSmeny2Sec)
                    {
                        //C3
                        UbehlyCasSmenySec = timeSec - zacatekSmeny1Sec - Prestavka1Sec - Prestavka2Sec;  //odectu cas 2. prestavky
                    }
                    else
                    {
                        //chyba
                        Logger.Error($"chybne vyhodnoceni intervalu smeny time:{timeSec}");
                    }

                    Tabule.CilKusuTabule = unitConfig.Cil1Smeny;
                    Tabule.CilDefectTabule = unitConfig.Def1Smeny;
                    smena = SmenyEnum.Smena1;
                }
                
                else if (timeSec >= zacatekSmeny2Sec || timeSec < zacatekSmeny1Sec) //smena 2 B (nocni typicky od 18:00)
                {
                    if (timeSec >= zacatekSmeny2Sec && timeSec < zacatek1PrestavkySmeny2Sec)
                    {
                        //C1
                        UbehlyCasSmenySec = timeSec - zacatekSmeny2Sec;
                    }
                    else if (timeSec >= zacatek1PrestavkySmeny2Sec && timeSec < konec1PrestavkySmeny2Sec)  
                    {
                        //prestavka 1, typicky 22:00 do 22:30
                        Tabule.IsPrestavkaTabule = true;
                        UbehlyCasSmenySec = zacatek1PrestavkySmeny2Sec - zacatekSmeny2Sec;  //cas se zastavil na zacatku 1. prestavky 2. smeny
                    }
                    else if (timeSec >= konec1PrestavkySmeny2Sec )  //pokracuje az do pulnoci (predpokladame ze C2 se vzdy deli pulnoci)
                    {
                        //C2 pred pulnoci
                        UbehlyCasSmenySec = timeSec - zacatekSmeny2Sec - Prestavka1Sec;  //odectu cas 1. prestavky
                    }
                    else if (timeSec < zacatek2PrestavkySmeny2Sec)  //pokracuje po pulnoci po 2. prestavku (predpokladame ze C2 se vzdy deli pulnoci)
                    {
                        //C2 po pulnoci
                        UbehlyCasSmenySec = (AllDaySec - zacatekSmeny2Sec - Prestavka1Sec) + timeSec;  //pripocteme sekundy z min. dne
                    }
                    else if (timeSec >= zacatek2PrestavkySmeny2Sec && timeSec < konec2PrestavkySmeny2Sec)
                    {
                        //prestavka 2, typicky 02:00 do 02:20
                        Tabule.IsPrestavkaTabule = true;
                        UbehlyCasSmenySec = (AllDaySec - zacatekSmeny2Sec - Prestavka1Sec) + zacatek2PrestavkySmeny2Sec;  //cas se zastavil na zacatku 2. prestavky 2. smeny + pripocteme sekundy z min. dne
                    }
                    else if (timeSec >= konec2PrestavkySmeny2Sec && timeSec < zacatekSmeny1Sec)
                    {
                        //C3
                        UbehlyCasSmenySec = (AllDaySec - zacatekSmeny2Sec - Prestavka1Sec) + timeSec - Prestavka2Sec;  //odectu cas 2. prestavky
                    }
                    else
                    {
                        //chyba
                        Logger.Error($"chybne vyhodnoceni intervalu smeny time:{timeSec}");
                    }

                    Tabule.CilKusuTabule = unitConfig.Cil2Smeny;
                    Tabule.CilDefectTabule = unitConfig.Def2Smeny;
                    smena = SmenyEnum.Smena2;
                }

                PrepocetTabule(smena, unitConfig.IsTypSmennostiA);
                
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
