using System;
using System.Threading;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitMonitor
    {
        #region fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const int BuffLength = 11;

        private static RunConfig _runConfig;

        private readonly byte _address;
        private readonly byte[] _inBuff;
        private byte _cmd;
        private SerialPortWraper _sp;
        private bool _isMockupMode;


        public UnitStatus UnitStatus { get; set; }

        public UnitConfig UnitConfig { get; set; }

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
        public const byte CmdReadHodnotuPoslCykluOk = 97; //vrati hodnotu posledniho cyklu OK
        public const byte CmdReadHodnotuPoslCykluNg = 98; //vrati hodnotu posledniho cyklu NG
        public const byte CmdReadHodnotuPrumCykluOk = 99; //vrati hodnotu prumerneho cyklu OK
        public const byte CmdReadHodnotuPrumCykluNg = 100; //vrati hodnotu prumerneho cyklu NG
        public const byte CmdReadTeplotu1Cidla = 101; //vrati teplotu z prvniho cidla DS18B20
        public const byte CmdReadTeplotu2Cidla = 102; //vrati teplotu z druheho cidla DS18B20
        public const byte CmdReadTeplotu3Cidla = 103; //vrati teplotu z tretiho cidla DS18B20
        public const byte CmdReadTeplotu4Cidla = 104; //vrati teplotu ze ctvrteho cidla DS18B20
        public const byte CmdReadTeplotu5Cidla = 105; //vrati teplotu z pateho cidla DS18B20
        public const byte CmdReadRozdilKusu = 106; //vraci rozdil kusu
        public const byte CmdReadDefektivita = 107; //vraci defektivitu

        #endregion

        #region ctor

        public UnitMonitor(byte address)
        {
            _address = address;
            _inBuff = new byte[BuffLength];
            _inBuff[2] = address;
            UnitStatus = new UnitStatus { SendError = false};
        }
        
        public UnitMonitor(byte address, SerialPortWraper serialPort, RunConfig runConfig) : this(address)
        {
            SerialPort = serialPort;
            _runConfig = runConfig;
        }


        #endregion

        #region properties

        public SerialPortWraper SerialPort
        {
            get { return _sp; }
            set { _sp = value; }
        }

        public byte Address
        {
            get { return _address; }
        }

        public DateTime ReadStavCitacuStartTime { get; set; }

        public bool IsReadStavCitacuPending { get; set; }

        public DateTime ReadCasOkStartTime { get; set; }

        public bool IsSendReadCasOkPending { get; set; }

        public DateTime ReadCasNgStartTime { get; set; }

        public bool IsSendReadCasNgPending { get; set; }

        #endregion

        #region private methods

        //pripravime in buffer na predani příkazu
        private void PrepareCommand(byte cmd)
        {
            Array.Clear(_inBuff, 0, _inBuff.Length);
            _inBuff[2] = _address;
            _cmd = cmd;
            _inBuff[3] = cmd;
        }

        //private bool CheckSendOk()
        //{
        //    var res = (_outBuff != null) && ((_outBuff[0] == 4 || _outBuff[0] == 0) && _outBuff[1] == 0 && _outBuff[2] == _address);
        //    if (!res)
        //    {
        //        UnitStatus.SendError = true;
        //        UnitStatus.LastSendErrorDescription = $"cmd=" + _cmd;
        //    }
        //    Logger.Debug($"res={res}");
        //    return res;
        //}

        //private bool CheckResponseOk()
        //{            
        //    var res = (_outBuff != null) && (_outBuff[0] == 0 && _outBuff[1] == 0 && _outBuff[2] == _address && (_outBuff[3] == _cmd || _cmd != 20)); //doplneno podle popisu z mailu 28.8. - neni nutne kontrolovat
        //    if (!res)
        //    {
        //        UnitStatus.SendError = true;
        //        UnitStatus.LastSendErrorDescription = $"cmd=" + _cmd;
        //    }
        //    Logger.Debug($"res={res}");
        //    return res;
        //}

        private void DiscardBuffers()
        {
            Logger.Debug("");
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
        }


        /// <summary>
        /// posle prikaz na port
        /// </summary>
        /// <returns></returns>
        private bool SendCommand(byte cmd)
        {
            bool res;
            Logger.Debug("+");

            PrepareCommand(cmd);

            if (!_sp.IsOpen) return false;

            try
            {
                //odeslat pripraveny command s parametry
 
                var wtask = _sp.WriteAsync(_inBuff, BuffLength);
                wtask.Wait(_runConfig.SendCommandTimeOut);

                Thread.Sleep(7);

                Logger.Debug(" -w");

                //kontrola odeslání ?
                
                res = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                res = false;
            }

            Logger.Debug($"- {res}");

            return res;
        }
     

        //private bool ReceiveResults(int a)
        //{
        //    bool res = false;
        //    Logger.Debug($"+ attempt:{a}");

        //    if (!_sp.IsOpen) return false;

        //    lock (lockobj)
        //    {
        //        //precte vysledky
        //        try
        //        {
        //            var task = _sp.ReadAsync(11);

        //            task.Wait(_runConfig.ReadResultTimeOut);
        //            if (!task.IsCompleted)
        //            {
        //                Logger.Debug(" -timeout");
        //            }
        //            else
        //            {
        //                Logger.Debug(" -ok");
        //                _outBuff = task.Result;
        //                res = true;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error(ex);
        //            res = false;
        //        }
        //    }

        //    res = res && CheckResponseOk();

        //    Thread.Sleep(_runConfig.RelaxTime);  //cekame po precteni vysledku pred tim nez se posle dalsi pozadavek

        //    Logger.Debug($"-");
        //    return res;
        //}


        /// <summary>
        /// posle pozadavek a precte vysledek.
        /// provadi dva pokusy
        /// </summary>
        /// <returns></returns>
        //private bool SendAndReceive()
        //{
        //    bool res = false;

        //    Logger.Debug("+");

        //    lock (lockobj)
        //    {

        //        if (SendCommand())
        //        {
        //            Thread.Sleep(_runConfig.RelaxTime);

        //            res = ReceiveResults(1);                    

        //        }

        //    }

        //    Logger.Debug($"- res:{res}");

        //    return res;
        //}
        
        #endregion

        #region public methods

        public bool SetSmennost(char varianta)
        {
            _inBuff[4] = (byte)varianta;

            UnitConfig.TypSmennosti = varianta.ToString();

            var res = SendCommand(CmdSetSmennost);

            //TODO
            //pockat XX ms na potvrzeni ?  

            return res;
        }
       
        public bool SetCileSmen(char varianta, short cil1, short cil2, short cil3)
        {
            Logger.Debug("+");

            _inBuff[4] = (byte)varianta;

            UnitConfig.Cil1Smeny = cil1;
            UnitConfig.Cil2Smeny = cil2;
            UnitConfig.Cil3Smeny = cil3;

            Helper.FromShort(cil1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(cil2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(cil3, out _inBuff[9], out _inBuff[10]);

            var res = SendCommand(CmdSetCilSmen);

            Logger.Debug("-");
            return res;
        }

        public bool SetDefektivita(char varianta, short def1, short def2, short def3)
        {
            _inBuff[4] = (byte)varianta;

            UnitConfig.Def1Smeny = def1;
            UnitConfig.Def2Smeny = def2;
            UnitConfig.Def3Smeny = def3;

            Helper.FromShort((short)(def1 * 10.0), out _inBuff[5], out _inBuff[6]);
            Helper.FromShort((short)(def2 * 10.0), out _inBuff[7], out _inBuff[8]);
            Helper.FromShort((short)(def3 * 10.0), out _inBuff[9], out _inBuff[10]);

            return SendCommand(CmdSetDefSmen);

        }

        //TODO - v jakych jednotkach se zadavaji prestavky ?
        public bool SetPrestavky(char varianta, TimeSpan prest1, TimeSpan prest2, TimeSpan prest3)
        {
            _inBuff[4] = (byte)varianta;

            UnitConfig.Prestavka1Smeny = prest1.ToString();  //TODO ukladat TimeSpan, ne string
            UnitConfig.Prestavka2Smeny = prest2.ToString();
            UnitConfig.Prestavka3Smeny = prest3.ToString();

            _inBuff[5] = (byte)prest1.Hours;
            _inBuff[6] = (byte)prest1.Minutes;

            _inBuff[7] = (byte)prest2.Hours;
            _inBuff[8] = (byte)prest2.Minutes;

            _inBuff[9] = (byte)prest3.Hours;
            _inBuff[10] = (byte)prest3.Minutes;

            return SendCommand(CmdSetZacPrestav);
        }

        public bool SetCas(DateTime dt)
        {
            Logger.Debug($"+ unit {_address}");

            _inBuff[4] = (byte)dt.Hour;
            _inBuff[5] = (byte)dt.Minute;
            _inBuff[6] = (byte)dt.Second;
            _inBuff[7] = (byte)dt.Day;
            _inBuff[8] = (byte)dt.Month;
            _inBuff[9] = (byte)(dt.Year-2000);
            _inBuff[10] = (byte)dt.DayOfWeek;

            var res = SendCommand(CmdSetDatumDen); 

            Logger.Debug($"- {res} unit {_address}");

            return res;
        }

        public bool SetJasLcd(byte jas)
        {
            _inBuff[4] = jas;

            return SendCommand(CmdSetJasLed);
        }

        public bool SetCitace(short ok, short ng)
        {
            Helper.FromShort(ok, out _inBuff[4], out _inBuff[5]);
            Helper.FromShort(ng, out _inBuff[6], out _inBuff[7]);

            return SendCommand(CmdSetHodnotyCitacu);
        }

        public bool Reset()
        {
            return SendCommand(CmdResetJednotky);
        }

        public bool Set6f()
        {

            return SendCommand(0x6f);
      
        }

        public bool SetHandshake(byte handshake, byte prumTyp)
        {

            _inBuff[4] = handshake;
            _inBuff[5] = prumTyp;

            var res = SendCommand(CmdSetParamRf);

            return res;
        }

        public bool SetStatus(byte writeProtectEEprom, byte minOK, byte minNG, byte bootloaderOn, byte parovanyLED, byte rozliseniCidel, byte pracovniJasLed)
        {
            _inBuff[4] = writeProtectEEprom;
            _inBuff[5] = minOK;
            _inBuff[6] = minNG;
            _inBuff[7] = bootloaderOn;
            _inBuff[8] = parovanyLED;
            _inBuff[9] = rozliseniCidel;
            _inBuff[10] = pracovniJasLed;

            return SendCommand(CmdZaklNastaveni);
        }

        public bool SendReadZaklNastaveni()
        {

            var res = SendCommand(CmdReadZakSysNast);

            return res;
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

        public bool SendReadStavCitacu()
        {
            Logger.Debug($"+ unit {_address}");

            //if (_isMockupMode)
            //{
            //    okVal = (short)DateTime.Now.Minute;  //minuta poslouzi jako hodnota ok
            //    ngVal = (short)((okVal + 1) / 2);
            //    UnitStatus.Ok = okVal;
            //    UnitStatus.Ng = ngVal;
            //    UnitStatus.OkNgTime = DateTime.Now;
            //    return true;
            //}

            var res = SendCommand(CmdReadStavCitacu);

            if (res)
            {
                IsReadStavCitacuPending = true;
                ReadStavCitacuStartTime = DateTime.Now;
            }
            else
            {
                IsReadStavCitacuPending = false;
            }

            return res;
        }

        public void DoReadStavCitacu(byte[] buff)
        {
            var okVal = Helper.ToShort(buff[4], buff[5]);
            var ngVal = Helper.ToShort(buff[6], buff[7]);
            short machineStatus = buff[8];
            short shutdownTime = Helper.ToShort(buff[9], buff[10]);

            UnitStatus.Ok = okVal;
            UnitStatus.Ng = ngVal;
            UnitStatus.MachineStatus = (MachineStateEnum)machineStatus;
            UnitStatus.MachineShutdownTime = shutdownTime;

            UnitStatus.OkNgTime = DateTime.Now;
            UnitStatus.IsOkNg = true;

            IsReadStavCitacuPending = false;

            Logger.Info($"okVal:{okVal} ngVal:{ngVal} machineStatus:{machineStatus} unit: {_address}");
        }


        public bool SendReadCasOK()
        {
            Logger.Debug($"+ unit {_address}");

            //if (_isMockupMode)
            //{
            //    value = (Single)DateTime.Now.Hour * 2;  //hod. poslouzi jako hodnota casu ok
            //    UnitStatus.CasOk = value;
            //    UnitStatus.CasOkTime = DateTime.Now;
            //    return true;
            //}

            var res = SendCommand(CmdReadHodnotuPoslCykluOk);

            if (res)
            {
                IsSendReadCasOkPending = true;
                ReadCasOkStartTime = DateTime.Now;
            }
            else
            {
                IsSendReadCasOkPending = false;
            }

            return res;
        }

        public void DoReadCasOk(byte[] buff)
        {
            var value = Helper.ToSingle(buff, 7);
            UnitStatus.CasOk = value;
            UnitStatus.CasOkTime = DateTime.Now;
            UnitStatus.IsCasOk = true;
            IsSendReadCasOkPending = false;
            Logger.Debug($"unit {_address}");
        }

        public bool SendReadCasNG()
        {
            Logger.Debug($"+ unit {_address}");

            //if (_isMockupMode)
            //{
            //    value = (Single)(DateTime.Now.Hour * 1.5);  //hod. poslouzi jako hodnota casu ng
            //    UnitStatus.CasNg = value;
            //    UnitStatus.CasNgTime = DateTime.Now;
            //    return true;
            //}

            var res = SendCommand(CmdReadHodnotuPoslCykluNg);

            if (res)
            {
                IsSendReadCasNgPending = true;
                ReadCasNgStartTime = DateTime.Now;
            }
            else
            {
                IsSendReadCasNgPending = false;
            }

            return res;
        }

        public void DoReadCasNg(byte[] buff)
        {
            var value = Helper.ToSingle(buff, 7);
            UnitStatus.CasNg = value;
            UnitStatus.CasNgTime = DateTime.Now;
            UnitStatus.IsCasNg = true;
            IsSendReadCasNgPending = false;
            Logger.Debug($"unit {_address}");
        }

        public bool SendReadRozdilKusu()
        {
            var res = SendCommand(CmdReadRozdilKusu);

            return res;
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
            var res = SendCommand(CmdReadDefektivita);

            return res;
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
    }
}