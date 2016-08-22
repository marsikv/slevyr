using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.LayoutRenderers.Wrappers;
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

        /*private bool SendCommand()
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
        }*/

        private async Task<bool> SendCommand()
        {
            Logger.Debug("+");
            //lock (lockobj)
            //{
                if (!_sp.IsOpen) return false;

                //odeslat pripraveny command s parametry
                _sp.Write(_inBuff, 0, BuffLength);
                Logger.Debug(" -w");
                //kontrola odeslání
                _outBuff = await _sp.ReadAsync(11);
                Logger.Debug(" -r");
            //}

            Thread.Sleep(RelaxTime);

            Logger.Debug(" -CheckSendOk");

            var res = CheckSendOk();
            Logger.Debug($"- {res}");
            return res;
        }

        private async Task<bool> ReceiveResults()
        {
            Logger.Debug("+");
            //lock (lockobj)
            //{
            if (!_sp.IsOpen) return false;

            //precte vysledky
            _outBuff = await _sp.ReadAsync(11);
            Logger.Debug(" -r");
            Thread.Sleep(RelaxTime);
            //}

            Logger.Debug("-");

            return true;
        }

        private void _sp_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region public methods

        public async Task<bool> SetSmennost(char varianta)
        {
            PrepareInput(16);

            _inBuff[4] = (byte)varianta;

            UnitStatus.Smennost = varianta;

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }
       
        public async Task<bool> SetCileSmen(char varianta, short cil1, short cil2, short cil3)
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

            if (await SendCommand())
            {
                //res = true;
                //načtení výsledku
                await ReceiveResults();
                res = CheckResponseOk();
            }
            else
            {
                res = false;
            }
            Logger.Debug("-");
            return res;
        }

        public async Task<bool> SetDefektivita(char varianta, short def1, short def2, short def3)
        {
            PrepareInput(19);

            _inBuff[4] = (byte)varianta;

            UnitStatus.CilDef1smeny = def1;
            UnitStatus.CilDef2smeny = def2;
            UnitStatus.CilDef3smeny = def3;

            Helper.FromShort(def1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(def2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(def3, out _inBuff[9], out _inBuff[10]);

            if (await SendCommand())
            {
                //načtení výsledku

                await ReceiveResults();
                return CheckResponseOk();                
            }
            else
            {
                return false;
            }
        }

        //TODO - v jakych jednotkach se zadavaji prestavky ?
        public async Task<bool> SetPrestavky(char varianta, short prest1, short prest2, short prest3)
        {

            PrepareInput(21);

            _inBuff[4] = (byte)varianta;

            UnitStatus.Prestav1smeny = prest1;
            UnitStatus.Prestav2smeny = prest2;
            UnitStatus.Prestav3smeny = prest3;

            Helper.FromShort(prest1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(prest2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(prest3, out _inBuff[9], out _inBuff[10]);

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> SetCas(DateTime dt)
        {            
            PrepareInput(17);

            _inBuff[4] = (byte)dt.Hour;
            _inBuff[5] = (byte)dt.Minute;
            _inBuff[6] = (byte)dt.Second;
            _inBuff[7] = (byte)dt.Day;
            _inBuff[8] = (byte)dt.Month;
            _inBuff[9] = (byte)(dt.Year-2000);
            _inBuff[10] = (byte)dt.DayOfWeek;

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> SetJasLcd(byte jas)
        {
            PrepareInput(20);

            _inBuff[4] = jas;

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> SetCitace(short ok, short ng)
        {
            PrepareInput(4);

            Helper.FromShort(ok, out _inBuff[4], out _inBuff[5]);
            Helper.FromShort(ng, out _inBuff[6], out _inBuff[7]);

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> Reset()
        {
            PrepareInput(7);

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                return true;  //nevraci ve vysledku cislo cmd, test neni platny
                //return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> Set6f()
        {
            PrepareInput(0x6f);

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                return true;  //nevraci ve vysledku cislo cmd, test neni platny
                //return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> SetHandshake(byte handshake, byte prumTyp)
        {
            PrepareInput(6);

            _inBuff[4] = handshake;
            _inBuff[5] = prumTyp;

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();

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

        public async Task<bool> SetStatus(byte writeProtectEEprom, byte minOK, byte minNG, byte bootloaderOn, byte parovanyLED, byte rozliseniCidel, byte pracovniJasLed)
        {
            PrepareInput(3);

            _inBuff[4] = writeProtectEEprom;
            _inBuff[5] = minOK;
            _inBuff[6] = minNG;
            _inBuff[7] = bootloaderOn;
            _inBuff[8] = parovanyLED;
            _inBuff[9] = rozliseniCidel;
            _inBuff[10] = pracovniJasLed;

            if (await SendCommand())
            {
                await ReceiveResults();
                return CheckResponseOk();
            }
            else
            {
                return false;
            }
        }

        public async Task<ZaklNastaveniDto> ReadZaklNastaveni()
        {
            PrepareInput(9);
            var val = new ZaklNastaveniDto();
           
            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                var res = CheckResponseOk();
                val.MinOk    = _outBuff[4];
                val.MinNg    = _outBuff[5];
                val.AdrLocal = _outBuff[6];
                val.VerzeSw1 = _outBuff[7];
                val.VerzeSw2 = _outBuff[8];
                val.VerzeSw3 = _outBuff[9];
                return val;
            }
            else
            {
                return null;
            }
        }

        public async Task<StavCitacuDto> ReadStavCitacu()
        {
            //TODO udelat konstanty na cisla prikazu
            PrepareInput(96);

            var val = new StavCitacuDto();

            var res = false;

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                res = CheckResponseOk();
                if (res)
                {
                    val.Ok = Helper.ToShort(_outBuff[4], _outBuff[5]);
                    val.Ng = Helper.ToShort(_outBuff[6], _outBuff[7]);
                }
            }

            UnitStatus.Ok = val.Ok;
            UnitStatus.Ng = val.Ng;
            UnitStatus.OkNgTime = DateTime.Now;
            UnitStatus.IsOkNg = res;

            return val;
        }

        public async Task<bool> RefreshStavCitacu()
        {
            var val = await ReadStavCitacu();
            return val != null;
        }

        public async Task<Single> ReadCasOK()
        {
            PrepareInput(97);

            Single value = Single.NaN;

            var res = false;

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                res = CheckResponseOk();
                if (res)
                {
                    value = Helper.ToSingle(_outBuff, 7);
                }
            }

            UnitStatus.CasOk = value;
            UnitStatus.CasOkTime = DateTime.Now;
            UnitStatus.IsCasOk = res;

            return value;
        }

        public async Task<bool> RefreshCas()
        {
            var rOk = await ReadCasOK();
            var rNg = await ReadCasNG();

            return !Single.IsNaN(rOk) && !Single.IsNaN(rNg);
        }

        public async Task<Single> ReadCasNG()
        {
            PrepareInput(98);

            Single value = 0;

            var res = false;

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                res = CheckResponseOk();
                if (res)
                {
                    value = Helper.ToSingle(_outBuff, 7);
                }
            }

            UnitStatus.CasNg = value;
            UnitStatus.CasNgTime = DateTime.Now;
            UnitStatus.IsCasNg = res;

            return value;
        }

        public async Task<short> ReadRozdilKusu()
        {
            PrepareInput(106);

            short value = 0;

            var res = false;

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                res = CheckResponseOk();
                value = Helper.ToShort(_outBuff[8], _outBuff[9]);
            }

            UnitStatus.RozdilKusu = value;
            UnitStatus.RozdilKusuTime = DateTime.Now;
            UnitStatus.IsRozdilKusu = res;

            return value;
        }

        public async Task<Single> ReadDefektivita()
        {
            PrepareInput(107);

            Single value = 0;

            var res = false;

            if (await SendCommand())
            {
                //načtení výsledku
                await ReceiveResults();
                res = CheckResponseOk();

                value = Helper.ToSingle(_outBuff, 7);
            }

            UnitStatus.Defektivita = value;
            UnitStatus.DefektivitaTime = DateTime.Now;
            UnitStatus.IsDefektivita = res;

            return value;
        }

        #endregion
    }
}