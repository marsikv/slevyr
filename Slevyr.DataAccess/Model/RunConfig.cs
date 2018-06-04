using System;
using System.Collections.Generic;
using System.Globalization;

namespace Slevyr.DataAccess.Model
{
    public class RunConfig
    {
        private const string DateTimeFormat = "ddd HH:mm d M yyyy"; //napr.  "Sun 08:30 15 Jun 2008"   

        public bool IsMockupMode;
        public bool IsRefreshTimerOn; //predava se do HTML strance "LinkaTabule" a aktivuje timer nacitani stavu

        public int
            RefreshTimerPeriod =
                3000; //perioda [ms] predavana pro timer nacitani stavu pomoci JS v HTML strance "LinkaTabule"

        public int
            PriorityCommandTimeOut =
                10000; //timeout pro prioritni prikazy, jako je treba nastaveni jednotek nebo ResetRF

        //public int WorkerSleepPeriod = 100;   //doba [ms] po kterou spi worker pred tim nez nacte stav dalsi jednotky
        public int RelaxTime = 300; //cekame [ms] nez se posle dalsi pozadavek 
        public int SendCommandTimeOut = 350; //timeout [ms] pro nacitani potvrzeni odeslaneho prikazu
        public int ReadResultTimeOut = 5000; //timeout [ms] pro nacitani dat ktere maji prijit po odeslanem prikazu

        public int
            MinCmdDelay =
                600; //příkazy na jednu adresu jednotky by nemyly jít častěji jak jednou za cca 600ms, poté dochází k zahlcení bufferu u 

        public int
            ReadStopDurationPeriod = 300; //perioda pro nacitani kumulativniho casu z jednotek prikaze 0x6f v sec 

        ///<summary>urcuje zda se nacita posledni cas OK a NG</summary>
        public bool IsReadOkNgTime = true;

        //public int PortReadTimeout;
        public string JsonDataFilePath;
        public string DbFilePath;

        /// <summary>seznam adres jednotek (linek)</summary>
        public IEnumerable<int> UnitAddrs;

        /// <summary>po odeslani prikazu thread ceka na response nebo timeout</summary>
        public bool IsWaitCommandResult;

        ///<summary></summary>
        public int SendAttempts; //urcuje max. pocet pokusu ktere provadi send

        /// <summary>pro cteni dat z portu pouzivan detereceived event</summary>
        public bool UseDataReceivedEvent { get; set; }
        //public bool OldSyncMode { get; set; }   //stary rezim komunikace synchronni s vyuzitim await, bez datareceived handleru

        ///<summary> výchozí hodnota pro export do souboru </summary>
        public string DefaultExportFileName { get; set; }

        /// <summary>  znak pro separator desetinne casti v CSV exportu  </summary>
        public char DecimalSeparator = ',';

        /// <summary>pocet cyklu pro kterych se provede planovany reset RF - nepovinny</summary>
        public int? CycleForScheduledResetRf;

        /// <summary>vraci zda se provede planovany reset RF </summary>
        public bool IsScheduledResetRF => CycleForScheduledResetRf.HasValue && CycleForScheduledResetRf > 0;

        public string SyncUnitTimePeriod;

        public bool IsSyncUnitTimePeriodEnabled;

        /// <summary> Provede reset RF kdyz je detekovane vetsi mnozstvi chyb a testovaci paket neni potrzen </summary>
        public bool IsAutoResetRF;

        /// <summary> tak dlouho se min. ceka nez se udela dalsi vzorek stavu linky pro graf [sec], 0 je vypnuto </summary>
        public int GraphSamplePeriodSec = 60;

        /// <summary>  Min. cas pro ulozeni vzorku i pokud se stav linky nezmenil, [sec]</summary>
        public int GraphMinSamplePeriodSec = int.MaxValue;

        /// <summary>  zkukovy soubor pro hlaseni prestavek (waw)</summary>
        public string HlaseniPrestavekSoundFile;

        /// <summary>  zkukovy soubor pro hlaseni smen (waw)</summary>
        public string HlaseniSmenSoundFile;

        /// <summary>  zkukovy soubor pro poplach (waw)</summary>
        public string PoplachSoundFile;

        public bool TransmitSound=false;

        public int LastSyncDay => _lastSyncDay;

        public bool IsHlaseniSmenSoundEnabled => !String.IsNullOrWhiteSpace(HlaseniSmenSoundFile);

        public bool IsHlaseniPrestavekSoundEnabled => !String.IsNullOrWhiteSpace(HlaseniPrestavekSoundFile);

        public bool IsPoplachSoundEnabled => !String.IsNullOrWhiteSpace(PoplachSoundFile);

        /// <summary> cekani na spusteni zesilovace po poslani priazu 0x18</summary>
        public static int DelayAfterStartTransmision = 6000; 

        private int _lastSyncDay;


        /// <summary>
        /// Vraci true pokud byl cas pro planovanou synchronizaci casu jednotek a v tomto dni jeste synchronizace neprobehla
        /// raci true jen pri prvnim platnem vyhodnoceni, 
        /// </summary>
        /// <returns></returns>
        public bool IsSyncUnitTime()
        {
            bool res=false;

            // SyncUnitTimePeriod je napr. Fri 23:10

            var dtn = DateTime.Now;
            // napr.  "Sun 08:30 15 Jun 2008"   
            string dateString = $"{SyncUnitTimePeriod} {dtn.Day} {dtn.Month} {dtn.Year}";

            if (dtn.DayOfYear > _lastSyncDay)
            try
            {
                DateTime dateTimeForSync;
                //var parsedExact = DateTime.TryParseExact(dateString, RunConfig.DateTimeFormat, CultureInfo.InvariantCulture, out dateTimeForSync);

                dateTimeForSync = DateTime.ParseExact(dateString, RunConfig.DateTimeFormat, CultureInfo.InvariantCulture);

                res = dtn >= dateTimeForSync ;

                if (res)
                {
                    _lastSyncDay = dtn.DayOfYear;
                }
            }
            catch (FormatException)
            {
                //sem to pada kdyz se datum nevyhodnoti jako platne
                //Console.WriteLine("{0} is not in the correct format.", dateString);
            }

            return res;

        }
    }
}
