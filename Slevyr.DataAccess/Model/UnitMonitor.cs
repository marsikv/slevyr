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
        const int MaxTimeToWait = 5000;  //cas v [ms]
        const int RelaxTime = 200;  //cas v [ms]

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
        
        public UnitMonitor(byte address, SerialPortWraper serialPort, bool isMockupMode) : this(address)
        {
            SerialPort = serialPort;
            _isMockupMode = isMockupMode;
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

        private void PrepareInput(byte cmd)
        {
            Array.Clear(_inBuff, 0, _inBuff.Length);
            _inBuff[2] = _address;
            _cmd = cmd;
            _inBuff[3] = cmd;
        }

        private bool CheckSendOk()
        {
            var res = (_outBuff[0] == 4 && _outBuff[1] == 0 && _outBuff[2] == _address);
            if (!res)
            {
                UnitStatus.SendError = true;
                UnitStatus.LastSendErrorDescription = $"cmd=" + _cmd;
            }
            return res;
        }

        private bool CheckResponseOk()
        {
            var res = (_outBuff[0] == 0 && _outBuff[1] == 0 && _outBuff[2] == _address && _outBuff[3] == _cmd);
            if (!res)
            {
                UnitStatus.SendError = true;
                UnitStatus.LastSendErrorDescription = $"cmd=" + _cmd;
            }
            return res;
        }

        private bool SendCommand()
        {
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
                task.Wait(MaxTimeToWait);
                Logger.Debug(" -w");
                _outBuff = task.Result;
                Logger.Debug(" -res");
            }

            Thread.Sleep(RelaxTime);

            Logger.Debug(" CheckSendOk");

            var res = CheckSendOk();
            Logger.Debug($"- {res}");
            return res;
        }

        private void ReceiveResults()
        {
            Logger.Debug("+");
            lock (lockobj)
            {
                if (!_sp.IsOpen) return;

                //precte vysledky
                var task = _sp.ReadAsync(11);
                Logger.Debug(" -r");
                task.Wait(MaxTimeToWait);
                Logger.Debug(" -w");
                _outBuff = task.Result;
                Logger.Debug(" -res");
                Thread.Sleep(RelaxTime);
                Logger.Debug(" -sleep");
            }

            Logger.Debug("-");
        }

        private void _sp_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region public methods

        public bool SetSmennost(char varianta)
        {
            PrepareInput(16);

            _inBuff[4] = (byte)varianta;

            UnitStatus.Smennost = varianta;

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
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
                res = true;
                //načtení výsledku
                //? ReceiveResults();
                //? res = CheckResponseOk();
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

                //ReceiveResults();
                //return CheckResponseOk();
                return true;
            }
            else
            {
                return false;
            }
        }

        //TODO - v jakych jednotkach se zadavaji prestavky ?
        public bool SetPrestavky(char varianta, short prest1, short prest2, short prest3)
        {

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
                ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public bool SetCas(DateTime dt)
        {            
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
                ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public bool SetJasLcd(byte jas)
        {
            PrepareInput(20);

            _inBuff[4] = jas;

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public bool SetCitace(short ok, short ng)
        {
            PrepareInput(4);

            Helper.FromShort(ok, out _inBuff[4], out _inBuff[5]);
            Helper.FromShort(ng, out _inBuff[6], out _inBuff[7]);

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public bool Reset()
        {
            PrepareInput(7);

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                return true;  //nevraci ve vysledku cislo cmd, test neni platny
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
                ReceiveResults();
                return true;  //nevraci ve vysledku cislo cmd, test neni platny
                //return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public bool SetHandshake(byte handshake, byte prumTyp)
        {
            PrepareInput(6);

            _inBuff[4] = handshake;
            _inBuff[5] = prumTyp;

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();

                var res = CheckResponseOk();

                UnitStatus.Handshake = (handshake == 0 && res);

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

                return res;
            }
            else
            {
                return false;
            }
        }

        public bool SetStatus(byte writeProtectEEprom, byte minOK, byte minNG, byte bootloaderOn, byte parovanyLED, byte rozliseniCidel, byte pracovniJasLed)
        {
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
                ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public bool ReadZaklNastaveni(out byte minOk, out byte minNg, out byte adrLocal, out byte verzeSw1, out byte verzeSw2, out byte verzeSw3)
        {
            PrepareInput(9);

            minOk = 0;
            minNg = 0;
            adrLocal = 0;
            verzeSw1 = verzeSw2 = verzeSw3 = 0;

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                var res = CheckResponseOk();
                minOk    = _outBuff[4];
                minNg    = _outBuff[5];
                adrLocal = _outBuff[6];
                verzeSw1 = _outBuff[7];
                verzeSw2 = _outBuff[8];
                verzeSw3 = _outBuff[9];
                return res;
            }
            else
            {
                return false;
            }
        }

        public bool ReadStavCitacu(out short ok, out short ng)
        {
            //TODO udelat konstanty na cisla prikazu
            PrepareInput(96);

            ok = 0;
            ng = 0;

            var res = false;

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                res = CheckResponseOk();
                if (res)
                {
                    ok = Helper.ToShort(_outBuff[4], _outBuff[5]);
                    ng = Helper.ToShort(_outBuff[6], _outBuff[7]);
                }
            }

            UnitStatus.Ok = ok;
            UnitStatus.Ng = ng;
            UnitStatus.OkNgTime = DateTime.Now;
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
            PrepareInput(97);

            value = 0;

            var res = false;

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                res = CheckResponseOk();
                if (res)
                {
                    value = Helper.ToSingle(_outBuff, 7);
                }
            }

            UnitStatus.CasOk = value;
            UnitStatus.CasOkTime = DateTime.Now;
            UnitStatus.IsCasOk = res;

            return res;
        }

        public bool ReadCasNG(out Single value)
        {
            PrepareInput(98);

            value = 0;

            var res = false;

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                res = CheckResponseOk();
                if (res)
                {
                    value = Helper.ToSingle(_outBuff, 7);
                }
            }

            UnitStatus.CasNg = value;
            UnitStatus.CasNgTime = DateTime.Now;
            UnitStatus.IsCasNg = res;

            return res;
        }

        public bool ReadRozdilKusu(out short value)
        {
            PrepareInput(106);

            value = 0;

            var res = false;

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                res = CheckResponseOk();
                value = Helper.ToShort(_outBuff[8], _outBuff[9]);
            }

            UnitStatus.RozdilKusu = value;
            UnitStatus.RozdilKusuTime = DateTime.Now;
            UnitStatus.IsRozdilKusu = res;

            return res;
        }

        public bool ReadDefektivita(out Single value)
        {
            PrepareInput(107);

            value = 0;

            var res = false;

            if (SendCommand())
            {
                //načtení výsledku
                ReceiveResults();
                res = CheckResponseOk();

                value = Helper.ToSingle(_outBuff, 7);
            }

            UnitStatus.Defektivita = value;
            UnitStatus.DefektivitaTime = DateTime.Now;
            UnitStatus.IsDefektivita = res;

            return res;
        }

        #endregion
    }
}