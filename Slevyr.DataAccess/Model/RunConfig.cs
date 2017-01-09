using System.Collections.Generic;

namespace Slevyr.DataAccess.Model
{
    public class RunConfig
    {
        public bool IsMockupMode;
        public bool IsRefreshTimerOn;         //predava se do HTML strance "LinkaTabule" a aktivuje timer nacitani stavu
        public int RefreshTimerPeriod = 3000; //perioda [ms] predavana pro timer nacitani stavu pomoci JS v HTML strance "LinkaTabule"

        //public int WorkerSleepPeriod = 100;   //doba [ms] po kterou spi worker pred tim nez nacte stav dalsi jednotky
        public int RelaxTime = 300;           //cekame [ms] nez se posle dalsi pozadavek 
        public int SendCommandTimeOut = 350;  //timeout [ms] pro nacitani potvrzeni odeslaneho prikazu
        public int ReadResultTimeOut = 5000;  //timeout [ms] pro nacitani dat ktere maji prijit po odeslanem prikazu
        public int MinCmdDelay = 600;         //příkazy na jednu adresu jednotky by nemyly jít častěji jak jednou za cca 600ms, poté dochází k zahlcení bufferu u 

        ///<summary>urcuje zda se nacita posledni cas OK a NG</summary>
        public bool IsReadOkNgTime = true; 
        //public int PortReadTimeout;
        public string JsonDataFilePath;
        public string DbFilePath;

        public IEnumerable<int> UnitAddrs;
        //public bool IsWriteEmptyToLog;       //pokud je true zapisuje do logu jednotek radek s prazdnymi hodnotami pokud dojde k chybe pri vycitani jednotky

        //public bool IsWaitCommandConfirmation;   //na to se ceka vzdy
        /// <summary>po odeslani prikazu thread ceka na response nebo timeout</summary>
        public bool IsWaitCommandResult;
        ///<summary></summary>
        public int SendAttempts;            //urcuje max. pocet pokusu ktere provadi send

        /// <summary>pro cteni dat z portu pouzivan detereceived event</summary>
        public bool UseDataReceivedEvent { get; set; } 
        //public bool OldSyncMode { get; set; }   //stary rezim komunikace synchronni s vyuzitim await, bez datareceived handleru

        ///<summary> výchozí hodnota pro export do souboru </summary>
        public string DefaultExportFileName { get; set; }  
    }
}
