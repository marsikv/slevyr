using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Model;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitMonitor
    {
        #region fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        const int BuffLength = 11;

        static RunConfig _runConfig;

        private readonly byte _address;
        private byte[] _inBuff;
        private byte[] _outBuff;
        private byte _cmd;
        private SerialPortWraper _sp;
        private bool _isMockupMode;
        private static object lockobj = new object();


        public UnitStatus UnitStatus { get; set; }

        public UnitConfig UnitConfig { get; set; }

        #endregion

        #region const pro prikazy

        const byte CmdZaklNastaveni = 3	; //Zakladni nastaveni
        const byte CmdSetHodnotyCitacu = 4	; //Zapise hodnoty do citacu
        //const byte CmdSet = 5	; //Vyvola programovaci mod adresy
        const byte CmdSetParamRF = 6	; //Nastavi parametry RF
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
        const byte CmdReadStavCitacu = 96; //Vrati stav citacu
        const byte CmdReadHodnotuPoslCykluOk = 97; //vrati hodnotu posledniho cyklu OK
        const byte CmdReadHodnotuPoslCykluNg = 98; //vrati hodnotu posledniho cyklu NG
        const byte CmdReadHodnotuPrumCykluOk = 99; //vrati hodnotu prumerneho cyklu OK
        const byte CmdReadHodnotuPrumCykluNg = 100; //vrati hodnotu prumerneho cyklu NG
        const byte CmdReadTeplotu1Cidla = 101; //vrati teplotu z prvniho cidla DS18B20
        const byte CmdReadTeplotu2Cidla = 102; //vrati teplotu z druheho cidla DS18B20
        const byte CmdReadTeplotu3Cidla = 103; //vrati teplotu z tretiho cidla DS18B20
        const byte CmdReadTeplotu4Cidla = 104; //vrati teplotu ze ctvrteho cidla DS18B20
        const byte CmdReadTeplotu5Cidla = 105; //vrati teplotu z pateho cidla DS18B20
        const byte CmdReadRozdilusu = 106; //vraci rozdil kusu
        const byte CmdReadDefektivita = 107; //vraci defektivitu

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

        private bool CheckSendOk()
        {
            var res = (_outBuff != null) && ((_outBuff[0] == 4 || _outBuff[0] == 0) && _outBuff[1] == 0 && _outBuff[2] == _address);
            if (!res)
            {
                UnitStatus.SendError = true;
                UnitStatus.LastSendErrorDescription = $"cmd=" + _cmd;
            }
            Logger.Debug($"res={res}");
            return res;
        }

        private bool CheckResponseOk()
        {            
            var res = (_outBuff != null) && (_outBuff[0] == 0 && _outBuff[1] == 0 && _outBuff[2] == _address && (_outBuff[3] == _cmd || _cmd != 20)); //doplneno podle popisu z mailu 28.8. - neni nutne kontrolovat
            if (!res)
            {
                UnitStatus.SendError = true;
                UnitStatus.LastSendErrorDescription = $"cmd=" + _cmd;
            }
            Logger.Debug($"res={res}");
            return res;
        }

        private void DiscardBuffers()
        {
            Logger.Debug("");
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
        }

        /// <summary>
        /// posle prikaz na port, parametry jsou jen pro logovani cisla pokusu
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <returns></returns>
        private bool SendCommandBasic(int a1, int a2)
        {
            bool res;
            Logger.Debug($"+ attempt:{a1}.{a2}");
            //lock (lockobj)
            //{
            if (!_sp.IsOpen) return false;

            try
            {
                //odeslat pripraveny command s parametry
                //_sp.Write(_inBuff, 0, BuffLength);

                var wtask = _sp.WriteAsync(_inBuff, BuffLength);
                wtask.Wait(_runConfig.SendCommandTimeOut);

                Thread.Sleep(7);

                Logger.Debug(" -w");

                //kontrola odeslání
                var task = _sp.ReadAsync(11);

                task.Wait(_runConfig.SendCommandTimeOut);

                if (!task.IsCompleted)
                {
                    Logger.Debug(" -timeout");
                    res = false;
                }
                else
                {
                    Logger.Debug(" -ok");
                    _outBuff = task.Result;
                    res = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                res = false;
            }

            res = res && CheckSendOk();
            Logger.Debug($"-");

            return res;
        }

        //odesle prikaz, v _inBuffer musi byt prikaz nachystany
        //provede kontrolu odeslani
        private bool SendCommand(int a)
        {
            Logger.Debug("+");

            bool sendOk = false;

            lock (lockobj)
            {
                sendOk = SendCommandBasic(a,1);

                if (!sendOk)
                {
                    Thread.Sleep(_runConfig.RelaxTime);
                    DiscardBuffers();
                    sendOk = SendCommandBasic(a,2);
                }

                if (!sendOk)
                {
                    Thread.Sleep(_runConfig.RelaxTime);
                    DiscardBuffers();
                    sendOk = SendCommandBasic(a,3);
                }

                if (!sendOk)
                {
                    DiscardBuffers();
                }
            }

            Logger.Debug($"- res:{sendOk}");        
            return sendOk;
        }

        private bool ReceiveResults(int a)
        {
            bool res = false;
            Logger.Debug($"+ attempt:{a}");

            if (!_sp.IsOpen) return false;

            lock (lockobj)
            {
                //precte vysledky
                try
                {
                    var task = _sp.ReadAsync(11);

                    task.Wait(_runConfig.ReadResultTimeOut);
                    if (!task.IsCompleted)
                    {
                        Logger.Debug(" -timeout");
                    }
                    else
                    {
                        Logger.Debug(" -ok");
                        _outBuff = task.Result;
                        res = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    res = false;
                }
            }

            res = res && CheckResponseOk();

            Thread.Sleep(_runConfig.RelaxTime);  //cekame po precteni vysledku pred tim nez se posle dalsi pozadavek

            Logger.Debug($"-");
            return res;
        }


        /// <summary>
        /// posle pozadavek a precte vysledek.
        /// provadi dva pokusy
        /// </summary>
        /// <returns></returns>
        private bool SendReceiveResults()
        {
            bool res = false;

            Logger.Debug("+");

            lock (lockobj)
            {

                if (SendCommand(1))
                {
                    Thread.Sleep(_runConfig.RelaxTime);

                    res = ReceiveResults(1);

                    if (!res)
                    {
                        Thread.Sleep(_runConfig.RelaxTime);
                        DiscardBuffers();
                        if (SendCommand(2))
                        {
                            Thread.Sleep(_runConfig.RelaxTime);
                            res = ReceiveResults(2);
                            if (!res)
                            {
                                Thread.Sleep(_runConfig.RelaxTime);
                                DiscardBuffers();
                            }
                        }
                    }

                }

            }

            Logger.Debug($"- res:{res}");

            return res;
        }
        

        private void _sp_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region public methods

        public bool SetSmennost(char varianta)
        {
            PrepareCommand(CmdSetSmennost);

            _inBuff[4] = (byte)varianta;

            UnitConfig.TypSmennosti = varianta.ToString();

            var res = SendReceiveResults();

            return res;
        }
       
        public bool SetCileSmen(char varianta, short cil1, short cil2, short cil3)
        {
            Logger.Debug("+");
            PrepareCommand(CmdSetCilSmen);

            _inBuff[4] = (byte)varianta;

            UnitConfig.Cil1Smeny = cil1;
            UnitConfig.Cil2Smeny = cil2;
            UnitConfig.Cil3Smeny = cil3;

            Helper.FromShort(cil1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(cil2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(cil3, out _inBuff[9], out _inBuff[10]);

            var res = SendReceiveResults();

            Logger.Debug("-");
            return res;
        }

        public bool SetDefektivita(char varianta, short def1, short def2, short def3)
        {
            PrepareCommand(CmdSetDefSmen);

            _inBuff[4] = (byte)varianta;

            UnitConfig.Def1Smeny = def1;
            UnitConfig.Def2Smeny = def2;
            UnitConfig.Def3Smeny = def3;

            Helper.FromShort(def1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(def2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(def3, out _inBuff[9], out _inBuff[10]);

            return SendCommand(1);

        }

        //TODO - v jakych jednotkach se zadavaji prestavky ?
        public bool SetPrestavky(char varianta, TimeSpan prest1, TimeSpan prest2, TimeSpan prest3)
        {
            PrepareCommand(CmdSetZacPrestav);

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

            return SendCommand(1);
        }

        public bool SetCas(DateTime dt)
        {
            Logger.Debug($"+ unit {_address}");

            PrepareCommand(CmdSetDatumDen);

            _inBuff[4] = (byte)dt.Hour;
            _inBuff[5] = (byte)dt.Minute;
            _inBuff[6] = (byte)dt.Second;
            _inBuff[7] = (byte)dt.Day;
            _inBuff[8] = (byte)dt.Month;
            _inBuff[9] = (byte)(dt.Year-2000);
            _inBuff[10] = (byte)dt.DayOfWeek;

            var res = SendCommand(1); 

            Logger.Debug($"- {res} unit {_address}");

            return res;
        }

        public bool SetJasLcd(byte jas)
        {
            bool res;
            PrepareCommand(CmdSetJasLed);

            _inBuff[4] = jas;

            return SendCommand(1);
        }

        public bool SetCitace(short ok, short ng)
        {
            bool res;
            PrepareCommand(CmdSetHodnotyCitacu);

            Helper.FromShort(ok, out _inBuff[4], out _inBuff[5]);
            Helper.FromShort(ng, out _inBuff[6], out _inBuff[7]);

            return SendCommand(1);
        }

        public bool Reset()
        {
            PrepareCommand(CmdResetJednotky);

            return SendCommand(1);

            //TODO otestovat

            /*
            if (SendCommand(1))
            {
                //načtení výsledku
                return ReceiveResults();
                //nevraci ve vysledku cislo cmd, test neni platny
                //return CheckResponseOk();
            }
            else
            {
                return false;
            }
            */
        }

        public bool Set6f()
        {
            PrepareCommand(0x6f);

            return SendCommand(1);

            //TODO otestovat

            /*
            if (SendCommand())
            {
                //načtení výsledku
                return ReceiveResults();
                //nevraci ve vysledku cislo cmd, test neni platny
                //return CheckResponseOk();
            }
            else
            {
                return false;
            }
            */
        }

        public bool SetHandshake(byte handshake, byte prumTyp)
        {
            PrepareCommand(CmdSetParamRF);

            _inBuff[4] = handshake;
            _inBuff[5] = prumTyp;

            var res = SendCommand(1);


            if (res)
            {
                UnitStatus.Handshake = (handshake == 0);

                if (UnitStatus.Handshake)
                {
                    //nastavim handler na prijem zprav z portu
                    //_sp.DataReceived += _sp_DataReceived;
                    //TODO spustit timer pro nacitani stavu prubezne
                }
                else
                {
                    //TODO handler by se mel asi okamzite odebrat pri kazdem prikazu ktery prijde
                    //odeberu handler
                    //_sp.DataReceived -= _sp_DataReceived;

                    //ukoncit timer
                }
            }
            return res;
        }

        public bool SetStatus(byte writeProtectEEprom, byte minOK, byte minNG, byte bootloaderOn, byte parovanyLED, byte rozliseniCidel, byte pracovniJasLed)
        {
            PrepareCommand(CmdZaklNastaveni);

            _inBuff[4] = writeProtectEEprom;
            _inBuff[5] = minOK;
            _inBuff[6] = minNG;
            _inBuff[7] = bootloaderOn;
            _inBuff[8] = parovanyLED;
            _inBuff[9] = rozliseniCidel;
            _inBuff[10] = pracovniJasLed;

            return SendCommand(1);
        }

        public bool ReadZaklNastaveni(out byte minOk, out byte minNg, out byte adrLocal, out byte verzeSw1, out byte verzeSw2, out byte verzeSw3)
        {
            PrepareCommand(CmdReadZakSysNast);

            minOk = 0;
            minNg = 0;
            adrLocal = 0;
            verzeSw1 = verzeSw2 = verzeSw3 = 0;

            var res = SendReceiveResults();

            if (res)
            {
                //načtení výsledku
                minOk = _outBuff[4];
                minNg = _outBuff[5];
                adrLocal = _outBuff[6];
                verzeSw1 = _outBuff[7];
                verzeSw2 = _outBuff[8];
                verzeSw3 = _outBuff[9];
            }

            return res;
        }

        public bool ReadStavCitacu(out int ok, out int ng)
        {
            Logger.Debug($"+ unit {_address}");

            if (_isMockupMode)
            {
                ok = (short)DateTime.Now.Minute;  //minuta poslouzi jako hodnota ok
                ng = (short)((ok + 1) / 2);
                UnitStatus.Ok = ok;
                UnitStatus.Ng = ng;
                UnitStatus.OkNgTime = DateTime.Now;
                return true;
            }

            PrepareCommand(CmdReadStavCitacu);

            ok = 0;
            ng = 0;

            //var sendOk = SendCommand();
            var res = SendReceiveResults();

            if (res)
            {
                //načtení výsledku
               
                ok = Helper.ToShort(_outBuff[4], _outBuff[5]);
                ng = Helper.ToShort(_outBuff[6], _outBuff[7]);
                short machineStatus = _outBuff[8];
                short shutdownTime = Helper.ToShort(_outBuff[9], _outBuff[10]);

                UnitStatus.Ok = ok;
                UnitStatus.Ng = ng;
                UnitStatus.MachineStatus = (MachineStateEnum)machineStatus;
                UnitStatus.MachineShutdownTime = shutdownTime;

                UnitStatus.OkNgTime = DateTime.Now;                   
            }
            else
            {
                UnitStatus.OkNgTime = DateTime.MaxValue;
                UnitStatus.ErrorTime = DateTime.Now;
                UnitStatus.MachineStatus = MachineStateEnum.Neznamy; 
            }

            UnitStatus.IsOkNg = res;

            Logger.Debug($"- {res} machineState {UnitStatus.MachineStatus}/unit {_address}");

            return res;
        }

        public bool ReadStavCitacu()
        {
            int ok;
            int ng;
            return ReadStavCitacu(out ok, out ng);
        }

        public bool ReadCasOK(out Single value)
        {
            Logger.Debug($"+ unit {_address}");

            if (_isMockupMode)
            {
                value = (Single)DateTime.Now.Hour * 2;  //hod. poslouzi jako hodnota casu ok
                UnitStatus.CasOk = value;
                UnitStatus.CasOkTime = DateTime.Now;
                return true;
            }


            PrepareCommand(CmdReadHodnotuPoslCykluOk);

            value = 0;

            var res = SendReceiveResults();

            if (res)
            {
                value = Helper.ToSingle(_outBuff, 7);
                UnitStatus.CasOk = value;
                UnitStatus.CasOkTime = DateTime.Now;
            }

            UnitStatus.IsCasOk = res;

            Logger.Debug($"- unit {_address}");

            return res;
        }

        public bool ReadCasNG(out Single value)
        {
            Logger.Debug($"+ unit {_address}");

            if (_isMockupMode)
            {
                value = (Single)(DateTime.Now.Hour * 1.5);  //hod. poslouzi jako hodnota casu ng
                UnitStatus.CasNg = value;
                UnitStatus.CasNgTime = DateTime.Now;
                return true;
            }

            PrepareCommand(CmdReadHodnotuPoslCykluNg);

            value = 0;

            var res = SendReceiveResults();

            if (res)
            {
                value = Helper.ToSingle(_outBuff, 7);
                UnitStatus.CasNg = value;
                UnitStatus.CasNgTime = DateTime.Now;             
            }

            UnitStatus.IsCasNg = res;

            Logger.Debug($"- unit {_address}");

            return res;
        }

        public bool ReadRozdilKusu(out short value)
        {
            PrepareCommand(CmdReadRozdilusu);

            value = 0;

            var res = SendReceiveResults();

            if (res)
            {
                    value = Helper.ToShort(_outBuff[8], _outBuff[9]);
                    UnitStatus.RozdilKusu = value;
                    UnitStatus.RozdilKusuTime = DateTime.Now;                
              
            }

            UnitStatus.IsRozdilKusu = res;

            return res;
        }

        public bool ReadDefektivita(out Single value)
        {
            PrepareCommand(CmdReadDefektivita);

            value = 0;

            var res = SendReceiveResults();

            if (res)
            {
                value = Helper.ToSingle(_outBuff, 7);
                UnitStatus.Defektivita = value;
                UnitStatus.DefektivitaTime = DateTime.Now;
            }

            UnitStatus.IsDefektivita = res;

            return res;
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