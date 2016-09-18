using System.Collections.Generic;

namespace Slevyr.DataAccess.Model
{
    public class RunConfig
    {
        public bool IsMockupMode;
        public bool IsRefreshTimerOn;         //predava se do HTML strance "LinkaTabule" a aktivuje timer nacitani stavu
        public int RefreshTimerPeriod = 1000; //perioda [ms] predavana pro timer nacitani stavu pomoci JS v HTML strance "LinkaTabule"
        public int WorkerSleepPeriod = 5000;  //doba [ms] po kterou spi worker pred tim nez nacte stav dalsi jednotky
        public int RelaxTime = 300;           //cekame [ms] po precteni vysledku pred tim nez se posle dalsi pozadavek
        public int SendCommandTimeOut = 350;  //timeout [ms] pro nacitani potvrzeni odeslaneho prikazu
        public int ReadResultTimeOut = 5000;  //timeout [ms] pro nacitani dat ktere maji prijit po odeslanem prikazu
        public bool IsReadOkNgTime = true;    //urcuje zda se nacita posledni cas OK a NG
        //public int PortReadTimeout;
        public string DataFilePath;
        public IEnumerable<int> UnitAddrs;
    }
}
