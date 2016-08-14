using System;
using System.Threading;
using System.Threading.Tasks;
using SledovaniVyroby.SerialPortWraper;

namespace SledovaniVyroby.SledovaniVyrobyService
{
    public class PlantMonitor
    {
        #region fields

        const int BuffLength = 11;
        const int MaxTimeToWait = 1000;  //cas v [ms]

        byte _cmdAdress;
        private byte[] _inBuff;
        private byte[] _outBuff;
        private SerialPortWraper.SerialPortWraper _sp;

        #endregion

        #region ctor

        public PlantMonitor(byte cmdAdress)
        {
            _cmdAdress = cmdAdress;
            _inBuff = new byte[BuffLength];
            _inBuff[2] = cmdAdress;
        }

        #endregion

        public SerialPortWraper.SerialPortWraper SerialPort
        {
            get { return _sp; }
            set { _sp = value; }
        }

        #region private methods

        private void PrepareInput(byte cmd)
        {
            Array.Clear(_inBuff, 0, _inBuff.Length);
            _inBuff[2] = _cmdAdress;
            _inBuff[3] = cmd;
        }

        private bool CheckSendOk()
        {
            return (_outBuff[0] == 4 && _outBuff[1] == 0 && _outBuff[2] == _cmdAdress);
        }

        private bool CheckResponseOk(byte cmd)
        {
            return (_outBuff[0] == 0 && _outBuff[1] == 0 && _outBuff[2] == _cmdAdress && _outBuff[3] == cmd);
        }

        #endregion

        public bool NastavSmennost16(char varianta)
        {
            PrepareInput(16);

            _inBuff[4] = (byte)varianta;

            _sp.Write(_inBuff, 0, BuffLength);

            //kontrola odeslání
            var task = _sp.ReadAsync(11);
            task.Wait(MaxTimeToWait);
            _outBuff = task.Result;
            
            if (CheckSendOk())
            {
                //načtení výsledku
                //var res = await _sp.ReadAsync(11);
                task = _sp.ReadAsync(11);
                task.Wait(MaxTimeToWait);

                return CheckResponseOk(16);
            }
            else
            {
                return false;
            }
        }
       

        public async Task<byte[]> NastavCileSmen18(char varianta, short cil1, short cil2, short cil3)
        {
            PrepareInput(18);

            _inBuff[4] = (byte)varianta; 

            Helper.FromShort(cil1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(cil2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(cil3, out _inBuff[9], out _inBuff[10]);

            _sp.Write(_inBuff, 0, BuffLength);

            //kontrola odeslání
            _outBuff = await _sp.ReadAsync(11);


            //zkontroluju na prvni byte hotnotu 4
            bool sendOk = CheckSendOk();

            if (sendOk)
            {
                //načtení výsledku
                var res =  await _sp.ReadAsync(11);

                return res;
            }
            else
            {
                return null;
            }
        }

        public async Task<byte[]> ZapsatCitace4(short ok, short ng)
        {
            PrepareInput(4);

            Helper.FromShort(ok, out _inBuff[4], out _inBuff[5]);
            Helper.FromShort(ng, out _inBuff[6], out _inBuff[7]);

            _sp.Write(_inBuff, 0, BuffLength);

            _outBuff = await _sp.ReadAsync(11);

            return await _sp.ReadAsync(11);
        }

        public async Task<byte[]> Reset7()
        {
            PrepareInput(7);

            _sp.Write(_inBuff, 0, BuffLength);

            _outBuff = await _sp.ReadAsync(11);

            return await _sp.ReadAsync(11);
        }

        public async Task<byte[]> VratitZaklNastaveni9()
        {
            PrepareInput(9);

            _sp.Write(_inBuff, 0, BuffLength);

            _outBuff = await _sp.ReadAsync(11);

            return await _sp.ReadAsync(11);
        }

        public async Task<byte[]> VratitStavCitacu96()
        {
            PrepareInput(96);

            _sp.Write(_inBuff, 0, BuffLength);

            _outBuff = await _sp.ReadAsync(11);

            return await _sp.ReadAsync(11);
        }

    }
}