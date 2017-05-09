using System.Collections.Generic;

namespace Slevyr.DataAccess.Model
{
    public class RunConfig
    {
        public bool IsMockupMode;
        public bool IsRefreshTimerOn;         //predava se do HTML strance "LinkaTabule" a aktivuje timer nacitani stavu
        public int RefreshTimerPeriod = 3000; //perioda [ms] predavana pro timer nacitani stavu pomoci JS v HTML strance "LinkaTabule"

        public int PriorityCommandTimeOut = 10000; //timeout pro prioritni prikazy, jako je treba nastaveni jednotek nebo ResetRF

        //public int WorkerSleepPeriod = 100;   //doba [ms] po kterou spi worker pred tim nez nacte stav dalsi jednotky
        public int RelaxTime = 300;           //cekame [ms] nez se posle dalsi pozadavek 
        public int SendCommandTimeOut = 350;  //timeout [ms] pro nacitani potvrzeni odeslaneho prikazu
        public int ReadResultTimeOut = 5000;  //timeout [ms] pro nacitani dat ktere maji prijit po odeslanem prikazu
        public int MinCmdDelay = 600;         //příkazy na jednu adresu jednotky by nemyly jít častěji jak jednou za cca 600ms, poté dochází k zahlcení bufferu u 
        
        public int ReadStopDurationPeriod = 300; //perioda pro nacitani kumulativniho casu z jednotek prikaze 0x6f v sec 

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
        public int SendAttempts;            //urcuje max. pocet pokusu ktere provadi send

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

        /// <summary> Provede reset RF kdyz je detekovane vetsi mnozstvi chyb a testovaci paket neni potrzen </summary>
        public bool IsAutoResetRF;

        /// <summary> tak dlouho se min. ceka nez se udela dalsi vzorek stavu linky pro graf [sec], 0 je vypnuto </summary>
        public int GraphSamplePeriodSec = 60;

        /// <summary>  Min. cas pro ulozeni vzorku i pokud se stav linky nezmenil, [sec]</summary>
        public int GraphMinSamplePeriodSec = int.MaxValue;


    }
}
