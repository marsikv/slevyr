using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.DAO;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitMonitor
    {
        #region fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger ErrorsLogger = LogManager.GetLogger("Errors");
        private static readonly Logger DataSendReceivedLogger = LogManager.GetLogger("DataReceived");
        private static readonly Logger TplLogger = LogManager.GetLogger("Tpl");

        private const int BuffLength = 11;

        private static RunConfig _runConfig;

        private readonly byte _address;
        private readonly byte[] _inBuff;
        private byte _cmd;
        private SerialPortWraper _sp;

        private bool _isMockupMode;
        private bool _errorRecorded;
        private int _errorRecordedCnt;

        public UnitStatus UnitStatus { get; set; }

        public UnitConfig UnitConfig { get; set; }

        public AutoResetEvent WaitEvent96 = new AutoResetEvent(false);
        public AutoResetEvent WaitEvent97 = new AutoResetEvent(false);
        public AutoResetEvent WaitEvent98 = new AutoResetEvent(false);


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

        public DateTime UpdateStatusStartTime { get; private set; }

        public bool IsUpdateStatusPending { get; set; }

        CancellationTokenSource UpdateStatusTokenSource { get; set; }


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
            Logger.Info("");
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
        }


        /// <summary>
        /// posle prikaz na port
        /// </summary>
        /// <returns></returns>
        public bool SendCommand(byte cmd)
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

                DataSendReceivedLogger.Debug($"->  {BuffLength}; {BitConverter.ToString(_inBuff)}");

                Thread.Sleep(7);

                Logger.Debug(" -w");

                //provadet kontrola odeslání ?
                //tzn. _outBuff[0] == 4 && _outBuff[1] == 0 && _outBuff[2] == _address
                //- muselo by se implementovat casove omezenym cekanim na event ktery by generoval handler SerialPortOnDataReceived  


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
       
        #endregion

        #region public methods

        public bool SetSmennost(char varianta)
        {
            Logger.Info($"+ unit {_address}");

            _inBuff[4] = (byte)varianta;

            UnitConfig.TypSmennosti = varianta.ToString();

            var res = SendCommand(CmdSetSmennost);

            //TODO
            //pockat XX ms na potvrzeni ?  

            return res;
        }
       
        public bool SetCileSmen(char varianta, short cil1, short cil2, short cil3)
        {
            Logger.Info($"+ unit {_address}");

            _inBuff[4] = (byte)varianta;

            UnitConfig.Cil1Smeny = cil1;
            UnitConfig.Cil2Smeny = cil2;
            UnitConfig.Cil3Smeny = cil3;

            Helper.FromShort(cil1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(cil2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(cil3, out _inBuff[9], out _inBuff[10]);

            var res = SendCommand(CmdSetCilSmen);

            return res;
        }

        public bool SetDefektivita(char varianta, short def1, short def2, short def3)
        {
            Logger.Info($"+ unit {_address}");

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
            Logger.Info($"+ unit {_address}");

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
            Logger.Info($"+ unit {_address}");

            _inBuff[4] = (byte)dt.Hour;
            _inBuff[5] = (byte)dt.Minute;
            _inBuff[6] = (byte)dt.Second;
            _inBuff[7] = (byte)dt.Day;
            _inBuff[8] = (byte)dt.Month;
            _inBuff[9] = (byte)(dt.Year-2000);
            _inBuff[10] = (byte)dt.DayOfWeek;

            var res = SendCommand(CmdSetDatumDen); 

            return res;
        }

        public bool SetJasLcd(byte jas)
        {
            Logger.Info($"+ unit {_address}");

            _inBuff[4] = jas;

            return SendCommand(CmdSetJasLed);
        }

        public bool SetCitace(short ok, short ng)
        {
            Logger.Info($"+ unit {_address}");

            Helper.FromShort(ok, out _inBuff[4], out _inBuff[5]);
            Helper.FromShort(ng, out _inBuff[6], out _inBuff[7]);

            return SendCommand(CmdSetHodnotyCitacu);
        }

        public bool Reset()
        {
            Logger.Info($"+ unit {_address}");

            return SendCommand(CmdResetJednotky);
        }

        public bool Set6f()
        {
            Logger.Info($"+ unit {_address}");

            return SendCommand(0x6f);
      
        }

        public bool SetHandshake(byte handshake, byte prumTyp)
        {
            Logger.Info($"+ unit {_address}");

            _inBuff[4] = handshake;
            _inBuff[5] = prumTyp;

            var res = SendCommand(CmdSetParamRf);

            return res;
        }

        public bool SetStatus(byte writeProtectEEprom, byte minOK, byte minNG, byte bootloaderOn, byte parovanyLED, byte rozliseniCidel, byte pracovniJasLed)
        {
            Logger.Info($"+ unit {_address}");

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
            Logger.Info($"+ unit {_address}");

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

        public bool UpdateStatus()
        {
            IsUpdateStatusPending = true;
            UpdateStatusStartTime = DateTime.Now;
            //UpdateStatusTokenSource = new CancellationTokenSource(_runConfig.ReadResultTimeOut * 3);
            UpdateStatusTokenSource = new CancellationTokenSource();
            CancellationToken token = UpdateStatusTokenSource.Token;

            Task<bool> readHodnotuCykluNgTask = null;

            try
            {
                Task<bool> readStavCitacuTask = Task.Factory.StartNew(() =>
                   {
                       TplLogger.Debug("readStavCitacuTask - start");
                       var sendOk = SendCommand(CmdReadStavCitacu);
                       TplLogger.Debug("readStavCitacuTask - wait for 96");
                       token.ThrowIfCancellationRequested();
                       var respReceived = WaitEvent96.WaitOne(_runConfig.ReadResultTimeOut);

                       token.ThrowIfCancellationRequested();
                       if (!respReceived) TplLogger.Debug($"readHodnotuCykluOk - timeout");
                       var res = sendOk && respReceived && UnitStatus.MachineStatus == MachineStateEnum.Vyroba; //kdyz zjistim ze stroj nebezi nebo ma poruchu tak cteni casu neprovadet, 
                       TplLogger.Debug($"readStavCitacuTask - res:{res} machine:{UnitStatus.MachineStatus}");
                       return res;
                   }, token);
              
                if (_runConfig.IsReadOkNgTime)
                {
                    Task<bool> readHodnotuCykluOkTask = readStavCitacuTask.ContinueWith<bool>((ant) =>
                    {
                        TplLogger.Debug("readHodnotuCykluOkTask - start");
                        if (ant.Status == TaskStatus.Faulted)
                            throw ant.Exception.InnerException;

                        bool res = false;
                        if (ant.Result)
                        {
                            var sendOk = SendCommand(CmdReadHodnotuPoslCykluOk);
                            token.ThrowIfCancellationRequested();
                            TplLogger.Debug($"readHodnotuCykluOkTask - wait for 97 {(sendOk ? "":"- skip")}");
                            if (sendOk)
                            {
                                var respReceived = WaitEvent97.WaitOne(_runConfig.ReadResultTimeOut);
                                token.ThrowIfCancellationRequested();
                                if (!respReceived) TplLogger.Debug($"readHodnotuCykluOk - timeout");
                                res = respReceived;
                            }                            
                        }

                        TplLogger.Debug($"readHodnotuCykluOkTask - res: {res}");
                        return res;
                    }, token, TaskContinuationOptions.OnlyOnRanToCompletion,TaskScheduler.Current );

                    readHodnotuCykluNgTask = readHodnotuCykluOkTask.ContinueWith<bool>((ant) =>
                    {
                        TplLogger.Debug("readHodnotuCykluNgTask - start");
                        if (ant.Status == TaskStatus.Faulted)
                            throw ant.Exception.InnerException;

                        bool res = false;
                        if (ant.Result)
                        {
                            var sendOk = SendCommand(CmdReadHodnotuPoslCykluNg);
                            token.ThrowIfCancellationRequested();
                            TplLogger.Debug($"readHodnotuCykluNgTask - wait for 98 {(sendOk ? "" : "- skip")}");
                            if (sendOk)
                            {
                                var respReceived = WaitEvent98.WaitOne(_runConfig.ReadResultTimeOut);
                                token.ThrowIfCancellationRequested();
                                if (!respReceived) TplLogger.Debug($"readHodnotuCykluNg - timeout");
                                res = respReceived;
                            }                           
                        }

                        TplLogger.Debug($"readHodnotuCykluNgTask - res: {res}");
                        return res;
                    }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);


                    //Task<bool> recalcTabuleTask = readHodnotuCykluNgTask.ContinueWith<bool>((ant) =>
                    //{
                    //    TplLogger.Debug("recalcTabuleTask - start");
                    //    if (ant.Status == TaskStatus.Faulted)
                    //        throw ant.Exception.InnerException;

                    //    if (ant.Result)
                    //    {
                    //        RecalcTabule();
                    //        SqlliteDao.AddUnitState(_address, UnitStatus);
                    //    }

                    //    IsUpdateStatusPending = false;
                    //    TplLogger.Debug($"recalcTabuleTask - res: {ant.Result}");
                    //    return ant.Result;
                    //}, TaskContinuationOptions.OnlyOnRanToCompletion);
                }
                //else
                //{
                //    Task<bool> recalcTabuleTask = readStavCitacuTask.ContinueWith<bool>((ant) =>
                //    {
                //        bool res = true;

                //        TplLogger.Debug("recalcTabuleTask - start");

                //        if (ant.Status == TaskStatus.Faulted)
                //            throw ant.Exception.InnerException;

                //        RecalcTabule();

                //        SqlliteDao.AddUnitState(_address, UnitStatus);

                //        IsUpdateStatusPending = false;

                //        TplLogger.Debug($"recalcTabuleTask - res: {res}");

                //        return res;
                //    }, TaskContinuationOptions.OnlyOnRanToCompletion);
                //}

            }
            catch (AggregateException aggEx)
            {
                foreach (Exception ex in aggEx.InnerExceptions)
                {
                    Logger.Error("Caught exception '{0}'", ex.Message);
                    IsUpdateStatusPending = false;
                }
            }
            catch (Exception)
            {
                throw;
            }

            try
            {                
                readHodnotuCykluNgTask?.Wait();
            }
            catch (AggregateException e)
            {
                foreach (Exception ie in e.InnerExceptions)
                    Console.WriteLine("{0}: {1}", ie.GetType().Name,
                                      ie.Message);
            }
            finally
            {
                UpdateStatusTokenSource.Dispose();
            }

            return true;
        }


        public bool SendReadStavCitacu()
        {
            Logger.Info($"+ unit {_address}");

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

            Logger.Info($"okVal:{okVal} ngVal:{ngVal} machineStatus:{machineStatus} unit: {_address}");
        }


        public bool SendReadCasOK()
        {
            Logger.Info($"+ unit {_address}");

          

            //if (_isMockupMode)
            //{
            //    value = (Single)DateTime.Now.Hour * 2;  //hod. poslouzi jako hodnota casu ok
            //    UnitStatus.CasOk = value;
            //    UnitStatus.CasOkTime = DateTime.Now;
            //    return true;
            //}

            var res = SendCommand(CmdReadHodnotuPoslCykluOk);

          

            return res;
        }

        public void DoReadCasOk(byte[] buff)
        {
            var value = Helper.ToSingle(buff, 7);
            UnitStatus.CasOk = value;
            UnitStatus.CasOkTime = DateTime.Now;
            UnitStatus.IsCasOk = true;
            Logger.Debug($"unit {_address}");
        }

        public bool SendReadCasNG()
        {
            Logger.Info($"+ unit {_address}");

            


            //if (_isMockupMode)
            //{
            //    value = (Single)(DateTime.Now.Hour * 1.5);  //hod. poslouzi jako hodnota casu ng
            //    UnitStatus.CasNg = value;
            //    UnitStatus.CasNgTime = DateTime.Now;
            //    return true;
            //}

            var res = SendCommand(CmdReadHodnotuPoslCykluNg);


            return res;
        }

        public void DoReadCasNg(byte[] buff)
        {
            var value = Helper.ToSingle(buff, 7);
            UnitStatus.CasNg = value;
            UnitStatus.CasNgTime = DateTime.Now;
            UnitStatus.IsCasNg = true;
            Logger.Debug($"unit {_address}");
        }

        public bool SendReadRozdilKusu()
        {
            Logger.Info($"+ unit {_address}");

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
            Logger.Info($"+ unit {_address}");

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