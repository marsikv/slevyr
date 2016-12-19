using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.DAO;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitMonitor : UnitMonitorBasic
    {
        #region fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger ErrorsLogger = LogManager.GetLogger("Errors");
        private static readonly Logger TplLogger = LogManager.GetLogger("Tpl");

        public UnitStatus UnitStatus { get; set; }

        public UnitConfig UnitConfig { get; set; }

        private volatile object _lock = new object();

        #endregion

        #region const pro prikazy

        const byte CmdZaklNastaveni = 3	; //Zakladni nastaveni
        const byte CmdSetHodnotyCitacu = 4	; //Zapise hodnoty do citacu
        //const byte CmdSet = 5	; //Vyvola programovaci mod adresy
        const byte CmdSetParamRf = 6	; //Nastavi parametry RF
        const byte CmdResetJednotky = 7	; //Reset jednotky
        const byte CmdZapsatEeprom= 15; //Zapise do eeprom od adresy 5 bytu

        const byte CmdSetSmennost = 16; //nastavi variantu smeny
        const byte CmdSetDatumDen = 17; //nastavi cas datum a den
        const byte CmdSetCilSmen = 18;  //nastavi cile smen
        const byte CmdSetDefSmen = 19;  //nastavi cile defektivity smen
        const byte CmdSetJasLed = 20;   //nastavi jas LED panelu
        const byte CmdSetZacPrestav = 21;   //nastavi zacatky prestavek

        const byte CmdReadVerSwRFT = 1; //vrati verzi sw RFT modulu + panel
        const byte CmdReadZakSysNast = 9; //vrati zakladni systemove nastaveni
        //const byte Get = 10	; //vrati sadu A systemovych nastaveni
        //const byte Get = 11	; //vrati sadu B systemovych nastaveni
        //const byte Get = 12	; //vrati sadu C systemovych nastaveni
        //const byte Get = 13	; //vrati sadu D systemovych nastaveni
        //const byte Get = 14	; //Precte 5 bytu z eeprom od adresy
        //const byte Get = 26	; //Vrati hodnoty citace 1 2 3
        //const byte Get = 27	; //Vrati hodnoty citace 4 5 6
        //const byte Get = 28	; //Vrati hodnoty citace 7 8
        public const byte CmdReadStavCitacu = 96; //Vrati stav citacu
        public const byte CmdReadCasPosledniOkNg = 97; //vrati hodnotu posledniho cyklu OK
        public const byte CmdReadHodnotuPrumCykluOkNg = 98; //vrati hodnotu prumerneho cyklu OK
        //public const byte CmdReadHodnotuPoslCykluNg = 98; //vrati hodnotu posledniho cyklu NG
        //public const byte CmdReadHodnotuPrumCykluNg = 100; //vrati hodnotu prumerneho cyklu NG
        public const byte CmdReadTeplotu1Cidla = 101; //vrati teplotu z prvniho cidla DS18B20
        public const byte CmdReadTeplotu2Cidla = 102; //vrati teplotu z druheho cidla DS18B20
        public const byte CmdReadTeplotu3Cidla = 103; //vrati teplotu z tretiho cidla DS18B20
        public const byte CmdReadTeplotu4Cidla = 104; //vrati teplotu ze ctvrteho cidla DS18B20
        public const byte CmdReadTeplotu5Cidla = 105; //vrati teplotu z pateho cidla DS18B20
        public const byte CmdReadRozdilKusu = 106; //vraci rozdil kusu
        public const byte CmdReadDefektivita = 107; //vraci defektivitu

        readonly byte[] _obtainStatusSequence;
        int _obtainStatusIndex = 0;

        #endregion

        #region ctor

      
        public UnitMonitor(byte address, SerialPortWraper serialPort, RunConfig runConfig) : base(address,serialPort,runConfig)
        {
            
            UnitStatus = new UnitStatus { SendError = false };

            if (this.RunConfig.IsReadOkNgTime)
            {
                _obtainStatusSequence = new byte[] {
                     CmdReadStavCitacu,
                     CmdReadCasPosledniOkNg,
                };
            }
            else
            {
                _obtainStatusSequence = new byte[] {
                    CmdReadStavCitacu
                };
            }
        }

        #endregion

        #region properties

       

        /// <summary>
        /// cas kdy byl prikaz odeslan (send)
        /// </summary>
        public DateTime CurrentCmdStartTime { get; private set; }

        /// <summary>
        /// Probiha zracovani prikazu, tzn. ceka se na receive data parovane na tento cmd
        /// </summary>
        public bool IsCommandPending { get; set; }

        /// <summary>
        /// Kod odeslaneho prikazu
        /// </summary>
        public byte CurrentCmd { get; set; }

        public CancellationTokenSource UpdateStatusTokenSource { get; private set; }
        public long LastId { get; set; }

        #endregion

        #region public methods

        public bool SetSmennost(char varianta)
        {
            Logger.Info($"+ unit {Address}");

            UnitConfig.TypSmennosti = varianta.ToString();

            var res = SendCommand(CmdSetSmennost, (byte)varianta);

            //TODO
            //pockat XX ms na potvrzeni ?  

            return res;
        }
       
        public bool SetCileSmen(char varianta, short cil1, short cil2, short cil3)
        {
            Logger.Info($"+ unit {Address}");

            UnitConfig.Cil1Smeny = cil1;
            UnitConfig.Cil2Smeny = cil2;
            UnitConfig.Cil3Smeny = cil3;

            return SendCommand(CmdSetCilSmen, (byte)varianta, cil1, cil2, cil3); 
        }

        public bool SetDefektivita(char varianta, short def1, short def2, short def3)
        {
            Logger.Info($"+ unit {Address}");
           
            UnitConfig.Def1Smeny = def1;
            UnitConfig.Def2Smeny = def2;
            UnitConfig.Def3Smeny = def3;

            return SendCommand(CmdSetDefSmen, (byte)varianta, (short)(def1 * 10.0), (short)(def2 * 10.0), (short)(def3 * 10.0));

        }

        //TODO - v jakych jednotkach se zadavaji prestavky ?
        public bool SetPrestavky(char varianta, TimeSpan prest1, TimeSpan prest2, TimeSpan prest3)
        {
            Logger.Info($"+ unit {Address}");
          
            UnitConfig.Prestavka1Smeny = prest1.ToString();  //TODO ukladat TimeSpan, ne string
            UnitConfig.Prestavka2Smeny = prest2.ToString();
            UnitConfig.Prestavka3Smeny = prest3.ToString();
            
            return SendCommand(CmdSetZacPrestav, (byte)varianta, prest1, prest2, prest3);
        }

       

        public bool SetCas(DateTime dt)
        {
            Logger.Info($"+ unit {Address}");

            return SendCommand(CmdSetDatumDen, dt); 
        }

        public bool SetJasLcd(byte jas)
        {
            Logger.Info($"+ unit {Address}");
 
            return SendCommand(CmdSetJasLed,jas);
        }

        public bool SetCitace(short ok, short ng)
        {
            Logger.Info($"+ unit {Address}");

            return SendCommand(CmdSetHodnotyCitacu,ok,ng,true) || SendCommand(CmdSetHodnotyCitacu, ok, ng, true) || SendCommand(CmdSetHodnotyCitacu, ok, ng, true);
        }

        public bool Reset()
        {
            Logger.Info($"+ unit {Address}");
            return SendCommand(CmdResetJednotky);
        }

        public bool Set6f()
        {
            Logger.Info($"+ unit {Address}");
            return SendCommand(0x6f);
      
        }

        public bool SetHandshake(byte handshake, byte prumTyp)
        {
            Logger.Info($"+ unit {Address}");

            return SendCommand(CmdSetParamRf, handshake, prumTyp);
        }

        public bool SetStatus(byte writeProtectEEprom, byte minOK, byte minNG, byte bootloaderOn, byte parovanyLED, byte rozliseniCidel, byte pracovniJasLed)
        {
            Logger.Info($"+ unit {Address}");

            return SendCommand(CmdZaklNastaveni, writeProtectEEprom,  minOK,  minNG,  bootloaderOn,  parovanyLED,  rozliseniCidel,  pracovniJasLed);
        }

        public bool SendReadZaklNastaveni()
        {
            Logger.Info($"+ unit {Address}");
            return SendCommandBasic(CmdReadZakSysNast);
        }

        public void DoReadZaklNastaveni(byte[] buff)
        {
            UnitStatus.MinOk = buff[4];
            UnitStatus.MinNg = buff[5];
            //adrLocal = _outBuff[6];
            UnitStatus.VerzeSw1 = buff[7];
            UnitStatus.VerzeSw2 = buff[8];
            UnitStatus.VerzeSw3 = buff[9];
        }

        public bool SendStatusCommand()
        {
            SetCommandIsPending(true); //na false se nastavuje z jineho vlakna
            CurrentCmdStartTime = DateTime.Now;
            CurrentCmd = _obtainStatusSequence[_obtainStatusIndex++];
            TplLogger.Debug($"send command {CurrentCmd} on [{Address}] - start");
            if (_obtainStatusIndex >= _obtainStatusSequence.Length) _obtainStatusIndex = 0;
            var res = SendCommand(CurrentCmd, true) || SendCommand(CurrentCmd, true) || SendCommand(CurrentCmd, true);
            TplLogger.Debug($"send command {CurrentCmd} on [{Address}] - {res}");
            if (!res)
            {
                ErrorsLogger.Error($"send command { CurrentCmd} on [{ Address}] - failed 3 times");
                SetCommandIsPending(false);
            }
            return res;
        }

        public bool SendReadStavCitacu()
        {
            Logger.Info($"+ unit {Address}");

            //if (_isMockupMode)
            //{
            //    okVal = (short)DateTime.Now.Minute;  //minuta poslouzi jako hodnota ok
            //    ngVal = (short)((okVal + 1) / 2);
            //    UnitStatus.Ok = okVal;
            //    UnitStatus.Ng = ngVal;
            //    UnitStatus.OkNgTime = DateTime.Now;
            //    return true;
            //}

            return SendCommand(CmdReadStavCitacu); ;
        }

        public void DoReadStavCitacu(byte[] buff)
        {
            /*
             * Pro příkaz 0x60 je odpověď = 0x00 0x00 ADR 0x60 LSB MSB LSB MSB LSB MSB 0xXX

                Počet vyrobených OK kusů = LSB+(256*MSB) , Kusy
                Počet vyrobených NG kusů = LSB+(256*MSB) , Kusy
                Stop time = LSB+(256*MSB) , Sekundy
                Stav stroje = XX
             */
            var okVal = Helper.ToShort(buff[4], buff[5]);
            var ngVal = Helper.ToShort(buff[6], buff[7]);
            var stopTime = Helper.ToShort(buff[8], buff[9]);
            short machineStatus = buff[10];

            UnitStatus.Ok = okVal;
            UnitStatus.Ng = ngVal;
            UnitStatus.MachineStatus = (MachineStateEnum)machineStatus;
            UnitStatus.MachineStopTime = stopTime;

            UnitStatus.OkNgTime = DateTime.Now;
            UnitStatus.IsOkNg = true;

            Logger.Info($"okVal:{okVal} ngVal:{ngVal} machineStatus:{machineStatus} unit: {Address}");
        }


        public bool SendReadCasOkNg()
        {
            Logger.Info($"+ unit {Address}");

            //if (_isMockupMode)
            //{
            //    value = (Single)DateTime.Now.Hour * 2;  //hod. poslouzi jako hodnota casu ok
            //    UnitStatus.CasOk = value;
            //    UnitStatus.CasOkNgTime = DateTime.Now;
            //    return true;
            //}

            return SendCommand(CmdReadCasPosledniOkNg);
        }

        
        public void DoReadCasOkNg(byte[] buff)
        {
            /*
             * 
             * Pro příkaz 0x61 je odpověď = 0x00 0x00 ADR 0x61 LSB MSB LSB MSB 0x00 0x00 0xXX
                čas posledního OK kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
                čas posledního NG kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
                Metoda měření cyklu = XX  , 0 = čas posledního cyklu (výchozí); >0 čas od posledního cyklu
             */
            var casOk10 = Helper.ToShort(buff[4], buff[5]); 
            var casNg10 = Helper.ToShort(buff[6], buff[7]); 

            UnitStatus.CasOk = (float) ( casOk10 / 10.0);
            UnitStatus.CasNg = (float)(casNg10 / 10.0);

            UnitStatus.CasOkNgTime = DateTime.Now;
            UnitStatus.IsCasOkNg = true;
            Logger.Debug($"unit {Address}");
        }

        public void DoReadCasPoslednihoCyklu(byte[] buff)
        {
            /*
            Pro příkaz 0x62 je odpověď = 0x00 0x00 ADR 0x62 LSB MSB LSB MSB 0x00 0x00 0xXX

                Průměrný čas OK kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
                Průměrný čas NG kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
                Metoda výpočtu průměru = XX  , 0 = výpočet vzhledem k délce směny (výchozí); >0 výpočet z uběhlého času směny
             */
            var casOk10 = Helper.ToShort(buff[4], buff[5]);
            var casNg10 = Helper.ToShort(buff[6], buff[7]);

            UnitStatus.AvgCasOk = (float)(casOk10 / 10.0);
            UnitStatus.AvgCasNg = (float)(casNg10 / 10.0);

            UnitStatus.CasOkNgTime = DateTime.Now;
            UnitStatus.IsCasOkNg = true;
            Logger.Debug($"unit {Address}");
        }

        public bool SendReadRozdilKusu()
        {
            Logger.Info($"+ unit {Address}");

            return SendCommand(CmdReadRozdilKusu);
        }

        public void DoReadRozdilKusu(byte[] buff)
        {
            var value = Helper.ToShort(buff[8], buff[9]);
            UnitStatus.RozdilKusu = value;
            UnitStatus.RozdilKusuTime = DateTime.Now;
            UnitStatus.IsRozdilKusu = true;
        }

        public bool ReadDefektivita()
        {
            Logger.Info($"+ unit {Address}");

            return SendCommandBasic(CmdReadDefektivita);
        }

        public void DoReadDefectivita(byte[] buff)
        {
            var value = Helper.ToSingle(buff, 7);
            UnitStatus.Defektivita = value;
            UnitStatus.DefektivitaTime = DateTime.Now;
            UnitStatus.IsDefektivita = true;
        }

        #endregion

        public void LoadUnitConfigFromFile(byte addr, string dataFilePath)
        {
            UnitConfig = new UnitConfig();
            UnitConfig.LoadFromFile(addr, dataFilePath);
        }

        public void RecalcTabule()
        {
            UnitStatus.RecalcTabule(UnitConfig);
        }

        public void SetCommandIsPending(bool val)
        {
            lock (_lock)
            {
                IsCommandPending = val;
            }
        }

        public void ResponseReceived(byte cmd)
        {
            lock (_lock)
            {
                if (cmd == CurrentCmd)
                {
                    IsCommandPending = false;
                }
            }
        }
    }
}