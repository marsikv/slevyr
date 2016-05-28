using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using SledovaniVyroby.SledovaniVyrobyService;
using SledovaniVyrobyWebAppHost.Model;
using SledovaniVyrobyWebAppHost.Properties;

namespace SledovaniVyrobyWebAppHost
{
    /*
    oprávnění pro různé uživatele, příkazy jsou v Hex: 
        Host: jen prohlížet
        Uživatel: měnit 10,11,12,13,14,15.
        Administrátor basic: + měnit 3,4,5,7.
        Administrátor advanced: + zbytek.


        nastavit cas 
    */

    public class SlevyController : ApiController
    {
        #region Fields
        static readonly SerialPortWraper SerialPort;

        static readonly Dictionary<int,UnitMonitor> UnitDictionary;

        static readonly RunConfig RunConfig = new RunConfig();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        //private static readonly Logger MefLogger = LogManager.GetLogger("MefLogger");

        #endregion

        #region ctor

        static SlevyController()
        {
            Logger.Info("+");

            //Nektere parametry nacitam z konfigurace

            var portCfg = new SerialPortConfig()
            {
                Port = Settings.Default.Port,
                BaudRate = Settings.Default.BaudRate,
                Parity = System.IO.Ports.Parity.None,
                DataBits = 8,
                StopBits = System.IO.Ports.StopBits.One,
                ReceiveLength = 11
            };

            SerialPort = new SerialPortWraper(portCfg);

            RunConfig.IsMockupMode = Settings.Default.MockupMode;
            RunConfig.IsRefreshTimerOn = Settings.Default.IsRefreshTimerOn;
            RunConfig.RefreshTimerPeriod = Settings.Default.RefreshTimerPeriod;

            //adresy jednotek jsou ve formatu delimited string
            //100;101
            UnitDictionary =
                Settings.Default.UnitAddrs.Split(';')
                //.Select(x => x.Split(':'))
                .ToDictionary(y => Int32.Parse(y), y => new UnitMonitor(Byte.Parse(y), SerialPort, RunConfig.IsMockupMode));

            Logger.Info("unit count: " + UnitDictionary.Count);
            //Console.WriteLine("unit count: " + UnitDictionary.Count);
        }

        #endregion

        #region RunConfig

        [HttpGet]
        public RunConfig GetConfig()
        {
            Logger.Info("+");
 
            return RunConfig;
        }

        [HttpGet]
        public bool SetConfig([FromUri] bool isMockupMode,[FromUri] bool isTimerOn, [FromUri] int timerPeriod)
        {
            Logger.Info($"isMockupMode: {isMockupMode}, isTimerOn: {isTimerOn},timerPeriod: {timerPeriod}");
            RunConfig.IsMockupMode = isMockupMode;
            RunConfig.IsRefreshTimerOn = isTimerOn;
            RunConfig.RefreshTimerPeriod = timerPeriod;
            return true;
        }

        #endregion

        #region open close port

        [HttpGet]
        public bool OpenPort()
        {
            Logger.Info("+");
            try
            {
                if (RunConfig.IsMockupMode) return true;
                if (!SerialPort.IsOpen) SerialPort.Open();
                return SerialPort.IsOpen;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        [HttpGet]
        public bool ClosePort()
        {
            Logger.Info("+");

            if (RunConfig.IsMockupMode) return true;
            if (SerialPort.IsOpen) SerialPort.Close();
            return !SerialPort.IsOpen;
        }

        #endregion

        #region UnitStatus operations

        [HttpGet]
        public UnitStatus Status([FromUri] byte addr)
        {
            Logger.Info("+");
            if (RunConfig.IsMockupMode) return Mock.MockUnitStatus();

            try
            {
                return UnitDictionary[addr].UnitStatus;
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public UnitStatus RefreshStatus([FromUri] byte addr)
        {
            Logger.Info($"+ {addr}");
            if (RunConfig.IsMockupMode) return Mock.MockUnitStatus();

            try
            {
                short ok, ng;
                Single casOk, casNg;
                Logger.Info( "   ReadStavCitacu");
                UnitDictionary[addr].ReadStavCitacu(out ok, out ng);
                Logger.Info($"   ok:{ok} ng:{ng}");
                Logger.Info( "   ReadCasOK");
                UnitDictionary[addr].ReadCasOK(out casOk);
                Logger.Info($"   casOk:{casOk}");
                Logger.Info( "   ReadCasNG");
                UnitDictionary[addr].ReadCasNG(out casNg);
                Logger.Info($"   casNg:{casNg}");

                //prepocitat pro zobrazeni tabule
                UnitDictionary[addr].UnitStatus.RecalcTabule();

                return UnitDictionary[addr].UnitStatus;
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavStatus([FromUri] byte addr, [FromUri] string writeProtectEEprom, [FromUri] byte minOK, [FromUri] byte minNG, [FromUri] string bootloaderOn, [FromUri] byte parovanyLED,
            [FromUri] byte rozliseniCidel, [FromUri] byte pracovniJasLed)
        {
            Logger.Info($"addr:{addr} writeProtectEEprom:{writeProtectEEprom} minOK:{minOK} minNG:{minNG} parovanyLED:{parovanyLED}");

            if (RunConfig.IsMockupMode) return true;

            try
            {
                byte writeProtectEEpromVal = (byte) (String.Equals(writeProtectEEprom, "ANO",
                    StringComparison.InvariantCultureIgnoreCase) ? 0 : 1);
                byte bootloaderOnVal = (byte)(String.Equals(bootloaderOn, "ANO",
                    StringComparison.InvariantCultureIgnoreCase) ? 0 : 1);
                return UnitDictionary[addr].SetStatus(writeProtectEEpromVal, minOK,minNG,bootloaderOnVal,parovanyLED,rozliseniCidel,pracovniJasLed);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavCileSmen([FromUri] byte addr, [FromUri] char varianta, [FromUri] short cil1, [FromUri] short cil2, [FromUri] short cil3)
        {
            Logger.Info($"addr:{addr} var:{varianta} cil1:{cil1} cil2:{cil2} cil3:{cil3}");

            if (RunConfig.IsMockupMode) return true;

            try
            {
                return UnitDictionary[addr].SetCileSmen(varianta,cil1,cil2,cil3);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavOkNg([FromUri] byte addr, [FromUri] short ok, [FromUri] short ng)
        {
            Logger.Info($"addr:{addr} ok:{ok} ng:{ng}");

            if (RunConfig.IsMockupMode) return true;

            try
            {
                return UnitDictionary[addr].SetCitace(ok, ng);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavAktualniCas([FromUri] byte addr)
        {
            Logger.Info("+");

            if (RunConfig.IsMockupMode) return true;

            try
            {
                return UnitDictionary[addr].SetCas(new DateTime());
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavDefektivitu([FromUri] byte addr, [FromUri]char varianta, [FromUri]string def1, [FromUri]string def2, [FromUri]string def3)
        {
            Logger.Info($"addr:{addr} varianta:{varianta} def1:{def1} def2:{def2} def3:{def3}");

            if (RunConfig.IsMockupMode) return true;

            double def1Val; 
            double def2Val;
            double def3Val;

            if (!(double.TryParse(def1, out def1Val) &&
            double.TryParse(def2, out def2Val) && double.TryParse(def3, out def3Val)))
            {
                throw new ArgumentException("Neplatná hodnota pro cíl");
            }

            try
            {
                Logger.Info($"cil1:{def1Val}");
                return UnitDictionary[addr].SetDefektivita(varianta, (short)(def1Val*10), (short)(def2Val * 10), (short)(def3Val * 10));
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [HttpGet]
        public bool NastavHandshake([FromUri] byte addr, [FromUri] bool value)
        {
            Logger.Info($"addr:{addr} val:{value}");

            if (RunConfig.IsMockupMode) return true;

            try
            {
                var um = UnitDictionary[addr];

                var res = um.SetHandshake(value ? (byte)1 : (byte)255, 0);

                if (res && value)
                {

                }

                return res;
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

        }

        [HttpGet]
        public bool CtiStavCitacu([FromUri] byte addr)
        {
            Logger.Info("+");

            if (RunConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().Ok++;
                Mock.MockUnitStatus().Ng++;
                return true;
            }

            try
            {
                return UnitDictionary[addr].RefreshStavCitacu();
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

        }

        [HttpGet]
        public bool CtiCyklusOkNg([FromUri] byte addr)
        {
            Logger.Info("+");

            if (RunConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().CasOkTime = DateTime.Now;
                Mock.MockUnitStatus().CasNgTime = DateTime.Now;
                return true;
            }

            try
            {
                float ok;
                float ng;
                return UnitDictionary[addr].ReadCasOK(out ok) && UnitDictionary[addr].ReadCasNG(out ng);
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

        }

        #endregion

    }
}
