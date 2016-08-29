﻿using System;
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

        readonly int _portReadTimeout = 5000;  //cas v [ms]
        readonly int _relaxTime = 200;  //cas v [ms]

        private byte _address;
        private byte[] _inBuff;
        private byte[] _outBuff;
        private byte _cmd;
        private SerialPortWraper _sp;
        private bool _isMockupMode;
        private static object lockobj = new object();


        public UnitStatus UnitStatus { get; set; }

        #endregion

        #region ctor

        public UnitMonitor(byte address)
        {
            _address = address;
            _inBuff = new byte[BuffLength];
            _inBuff[2] = address;
            UnitStatus = new UnitStatus { SendError = false};
        }
        
        public UnitMonitor(byte address, SerialPortWraper serialPort, bool isMockupMode, int portReadTimeout, int relaxTime) : this(address)
        {
            SerialPort = serialPort;
            _isMockupMode = isMockupMode;
            _relaxTime = relaxTime;
            _portReadTimeout = portReadTimeout;
        }


        #endregion

        #region properties

        public SerialPortWraper SerialPort
        {
            get { return _sp; }
            set { _sp = value; }
        }

        #endregion

        #region private methods

        //pripravime in buffer na predani příkazu
        private void PrepareInput(byte cmd)
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
            return res;
        }

        private void DiscardReceiveBuffer()
        {
            Logger.Debug(" +");
            _sp.DiscardInBuffer();
            Logger.Debug(" -");
        }

        //odesle prikaz, v _inBuffer musi byt prikaz nachystany
        //provede kontrolu odeslani
        private bool SendCommand()
        {
            bool res = false;
            Logger.Debug("+");
            lock (lockobj)
            {
                if (!_sp.IsOpen) return false;

                //odeslat pripraveny command s parametry
                _sp.Write(_inBuff, 0, BuffLength);
                Logger.Debug(" -w");
                //kontrola odeslání
                var task = _sp.ReadAsync(11);
                Logger.Debug(" -r");
                task.Wait(_portReadTimeout);
                if (!task.IsCompleted)
                {
                    Logger.Debug(" -timeout");                   
                    res = false;
                }
                else
                {
                    Logger.Debug(" -w");
                    _outBuff = task.Result;
                    Logger.Debug(" -res");
                    Thread.Sleep(_relaxTime);
                    Logger.Debug($" -relax {_relaxTime} ms");
                }
            }

            Logger.Debug(" CheckSendOk");
            res = CheckSendOk();
            Logger.Debug($"- {res}");

            if (!res)
            {
                DiscardReceiveBuffer();
            }

            return res;
        }

        private bool ReceiveResults()
        {
            bool res = false;
            Logger.Debug("+");
            lock (lockobj)
            {
                if (!_sp.IsOpen) return false;

                //precte vysledky
                try
                {
                    var task = _sp.ReadAsync(11);
                    Logger.Debug(" -r");
                    task.Wait(_portReadTimeout);
                    if (!task.IsCompleted)
                    {
                        Logger.Debug(" -timeout");
                    }
                    else
                    {
                        Logger.Debug(" -w");
                        _outBuff = task.Result;
                        Logger.Debug(" -res");
                        Thread.Sleep(_relaxTime);
                        Logger.Debug(" -sleep");
                        res = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    res = false;
                }
                
            }

            Logger.Debug("-");

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
            bool res;
            PrepareInput(16);

            _inBuff[4] = (byte)varianta;

            UnitStatus.Smennost = varianta;

            if (SendCommand())
            {
                //načtení výsledku
                
                res = ReceiveResults() && CheckResponseOk();
                if (!res) DiscardReceiveBuffer();
            }
            else
            {
                res = false;
            }

            return res;
        }
       
        public bool SetCileSmen(char varianta, short cil1, short cil2, short cil3)
        {
            bool res;
            Logger.Debug("+");
            PrepareInput(18);

            _inBuff[4] = (byte)varianta;

            UnitStatus.Cil1smeny = cil1 * 10;
            UnitStatus.Cil2smeny = cil2 * 10;
            UnitStatus.Cil3smeny = cil3 * 10;

            Helper.FromShort(cil1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(cil2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(cil3, out _inBuff[9], out _inBuff[10]);

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (!res) DiscardReceiveBuffer();
            }
            else
            {
                res = false;
            }
            Logger.Debug("-");
            return res;
        }

        public bool SetDefektivita(char varianta, short def1, short def2, short def3)
        {
            bool res;
            PrepareInput(19);

            _inBuff[4] = (byte)varianta;

            UnitStatus.CilDef1smeny = def1;
            UnitStatus.CilDef2smeny = def2;
            UnitStatus.CilDef3smeny = def3;

            Helper.FromShort(def1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(def2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(def3, out _inBuff[9], out _inBuff[10]);

            if (SendCommand())
            {
                //načtení výsledku

                res = ReceiveResults() && CheckResponseOk();
                if (!res) DiscardReceiveBuffer();
            }
            else
            {
                res = false;
            }

            return res;
        }

        //TODO - v jakych jednotkach se zadavaji prestavky ?
        public bool SetPrestavky(char varianta, short prest1, short prest2, short prest3)
        {
            bool res;
            PrepareInput(21);

            _inBuff[4] = (byte)varianta;

            UnitStatus.Prestav1smeny = prest1;
            UnitStatus.Prestav2smeny = prest2;
            UnitStatus.Prestav3smeny = prest3;

            Helper.FromShort(prest1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(prest2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(prest3, out _inBuff[9], out _inBuff[10]);

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (!res) DiscardReceiveBuffer();
            }
            else
            {
                res = false;
            }

            return res;
        }

        public bool SetCas(DateTime dt)
        {
            bool res;
            PrepareInput(17);

            _inBuff[4] = (byte)dt.Hour;
            _inBuff[5] = (byte)dt.Minute;
            _inBuff[6] = (byte)dt.Second;
            _inBuff[7] = (byte)dt.Day;
            _inBuff[8] = (byte)dt.Month;
            _inBuff[9] = (byte)(dt.Year-2000);
            _inBuff[10] = (byte)dt.DayOfWeek;

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (!res) DiscardReceiveBuffer();
            }
            else
            {
                res = false;
            }
            return res;
        }

        public bool SetJasLcd(byte jas)
        {
            bool res;
            PrepareInput(20);

            _inBuff[4] = jas;

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (!res) DiscardReceiveBuffer();
            }
            else
            {
                res = false;
            }
            return res;
        }

        public bool SetCitace(short ok, short ng)
        {
            bool res;
            PrepareInput(4);

            Helper.FromShort(ok, out _inBuff[4], out _inBuff[5]);
            Helper.FromShort(ng, out _inBuff[6], out _inBuff[7]);

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (!res) DiscardReceiveBuffer();
            }
            else
            {
                res = false;
            }
            return res;
        }

        public bool Reset()
        {
            PrepareInput(7);

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
        }

        public bool Set6f()
        {
            PrepareInput(0x6f);

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
        }

        public bool SetHandshake(byte handshake, byte prumTyp)
        {
            bool res;
            PrepareInput(6);

            _inBuff[4] = handshake;
            _inBuff[5] = prumTyp;

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();

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
                else
                {
                    DiscardReceiveBuffer();
                }

                return res;
            }
            else
            {
                return false;
            }
        }

        public bool SetStatus(byte writeProtectEEprom, byte minOK, byte minNG, byte bootloaderOn, byte parovanyLED, byte rozliseniCidel, byte pracovniJasLed)
        {
            bool res;
            PrepareInput(3);

            _inBuff[4] = writeProtectEEprom;
            _inBuff[5] = minOK;
            _inBuff[6] = minNG;
            _inBuff[7] = bootloaderOn;
            _inBuff[8] = parovanyLED;
            _inBuff[9] = rozliseniCidel;
            _inBuff[10] = pracovniJasLed;

            if (SendCommand())
            {
                res = ReceiveResults() && CheckResponseOk();
                if (!res) DiscardReceiveBuffer();
            }
            else
            {
                return false;
            }
            return res;
        }

        public bool ReadZaklNastaveni(out byte minOk, out byte minNg, out byte adrLocal, out byte verzeSw1, out byte verzeSw2, out byte verzeSw3)
        {
            bool res = false;
            PrepareInput(9);

            minOk = 0;
            minNg = 0;
            adrLocal = 0;
            verzeSw1 = verzeSw2 = verzeSw3 = 0;

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (res)
                {
                    minOk = _outBuff[4];
                    minNg = _outBuff[5];
                    adrLocal = _outBuff[6];
                    verzeSw1 = _outBuff[7];
                    verzeSw2 = _outBuff[8];
                    verzeSw3 = _outBuff[9];
                }
                else
                {
                    DiscardReceiveBuffer();
                }
            }

            return res;
        }

        public bool ReadStavCitacu(out short ok, out short ng)
        {
            bool res = false;
            //TODO udelat konstanty na cisla prikazu
            PrepareInput(96);

            ok = 0;
            ng = 0;

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (res)
                {
                    ok = Helper.ToShort(_outBuff[4], _outBuff[5]);
                    ng = Helper.ToShort(_outBuff[6], _outBuff[7]);
                    UnitStatus.Ok = ok;
                    UnitStatus.Ng = ng;
                    UnitStatus.OkNgTime = DateTime.Now;                   
                }
                else
                {
                    DiscardReceiveBuffer();
                }
            }

            UnitStatus.IsOkNg = res;

            return res;
        }

        public bool RefreshStavCitacu()
        {
            short ok;
            short ng;
            return ReadStavCitacu(out ok, out ng);
        }

        public bool ReadCasOK(out Single value)
        {
            bool res = false;

            PrepareInput(97);

            value = 0;


            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (res)
                {
                    value = Helper.ToSingle(_outBuff, 7);
                    UnitStatus.CasOk = value;
                    UnitStatus.CasOkTime = DateTime.Now;
                }
                else
                {
                    DiscardReceiveBuffer();
                }
            }

            UnitStatus.IsCasOk = res;

            return res;
        }

        public bool ReadCasNG(out Single value)
        {
            bool res = false;
            PrepareInput(98);

            value = 0;

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (res)
                {
                    value = Helper.ToSingle(_outBuff, 7);
                    UnitStatus.CasNg = value;
                    UnitStatus.CasNgTime = DateTime.Now;
                }
                else
                {
                    DiscardReceiveBuffer();
                }
            }

            UnitStatus.IsCasNg = res;

            return res;
        }

        public bool ReadRozdilKusu(out short value)
        {
            bool res = false;

            PrepareInput(106);

            value = 0;

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();
                if (res)
                {
                    value = Helper.ToShort(_outBuff[8], _outBuff[9]);
                    UnitStatus.RozdilKusu = value;
                    UnitStatus.RozdilKusuTime = DateTime.Now;
                }
                else
                {
                    DiscardReceiveBuffer();
                }
            }

            UnitStatus.IsRozdilKusu = res;

            return res;
        }

        public bool ReadDefektivita(out Single value)
        {
            bool res = false;

            PrepareInput(107);

            value = 0;

            if (SendCommand())
            {
                //načtení výsledku
                res = ReceiveResults() && CheckResponseOk();

                if (res)
                {
                    value = Helper.ToSingle(_outBuff, 7);
                    UnitStatus.Defektivita = value;
                    UnitStatus.DefektivitaTime = DateTime.Now;
                }
                else
                {
                    DiscardReceiveBuffer();
                }
            }

            UnitStatus.IsDefektivita = res;

            return res;
        }

        #endregion
    }
}