using System;
using System.Threading;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitMonitorBasic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger DataSendReceivedLogger = LogManager.GetLogger("DataReceived");
        private static readonly Logger TplLogger = LogManager.GetLogger("Tpl");

        private readonly byte _address;
        private readonly byte[] _inBuff;

        private static RunConfig _runConfig;

        private SerialPortWraper _sp;

        private const int BuffLength = 11;

        public byte Address => _address;

        private SerialPortWraper SerialPort
        {
            get { return _sp; }
            set { _sp = value; }
        }

        public RunConfig RunConfig => _runConfig;

        //public AutoResetEvent WaitEvent96 = new AutoResetEvent(false);
        //public AutoResetEvent WaitEvent97 = new AutoResetEvent(false);
        //public AutoResetEvent WaitEvent98 = new AutoResetEvent(false);
        public AutoResetEvent WaitEventSendConfirm = new AutoResetEvent(false);

        public UnitMonitorBasic(byte address, SerialPortWraper serialPort, RunConfig runConfig)
        {
            SerialPort = serialPort;
            _runConfig = runConfig;

            _address = address;

            _inBuff = new byte[BuffLength];
            _inBuff[2] = address;
        }

        private void ClearSendBuffer()
        {
            Array.Clear(_inBuff, 0, _inBuff.Length);
        }

        //pripravime in buffer na predani příkazu
        private void PrepareCommand(byte cmd)
        {
            _inBuff[2] = _address;
            _inBuff[3] = cmd;
        }


        private void DiscardBuffers()
        {
            Logger.Info("");
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
        }

        public bool SendCommand(byte cmd, byte p1, bool checkSendConfirmation = false)
        {
            ClearSendBuffer();

            _inBuff[4] = p1;

            return SendCommandBasic(cmd, checkSendConfirmation);
        }

        public bool SendCommand(byte cmd, byte p1, byte p2, bool checkSendConfirmation = false)
        {
            ClearSendBuffer();

            _inBuff[4] = p1;
            _inBuff[5] = p2;

            return SendCommandBasic(cmd, checkSendConfirmation);
        }

        public bool SendCommand(byte cmd, byte p1, byte p2, byte p3, byte p4, byte p5, byte p6, byte p7, bool checkSendConfirmation = false)
        {
            ClearSendBuffer();

            _inBuff[4] = p1;
            _inBuff[5] = p2;
            _inBuff[6] = p3;
            _inBuff[7] = p4;
            _inBuff[8] = p5;
            _inBuff[9] = p6;
            _inBuff[10] = p7;

            return SendCommandBasic(cmd, checkSendConfirmation);
        }

        public bool SendCommand(byte cmd, byte p1, short s1, short s2, short s3, bool checkSendConfirmation = false)
        {
            ClearSendBuffer();

            _inBuff[4] = p1;
            Helper.FromShort(s1, out _inBuff[5], out _inBuff[6]);
            Helper.FromShort(s2, out _inBuff[7], out _inBuff[8]);
            Helper.FromShort(s3, out _inBuff[9], out _inBuff[10]);

            return SendCommandBasic(cmd, checkSendConfirmation);
        }

        public bool SendCommand(byte cmd,  short s1, short s2, bool checkSendConfirmation = false)
        {
            ClearSendBuffer();

            Helper.FromShort(s1, out _inBuff[4], out _inBuff[5]);
            Helper.FromShort(s2, out _inBuff[6], out _inBuff[7]);

            return SendCommandBasic(cmd, checkSendConfirmation);
        }

        public bool SendCommand(byte cmd, byte p1, TimeSpan prest1, TimeSpan prest2, TimeSpan prest3)
        {
            ClearSendBuffer();

            _inBuff[4] = p1;
            _inBuff[5] = (byte)prest1.Hours;
            _inBuff[6] = (byte)prest1.Minutes;

            _inBuff[7] = (byte)prest2.Hours;
            _inBuff[8] = (byte)prest2.Minutes;

            _inBuff[9] = (byte)prest3.Hours;
            _inBuff[10] = (byte)prest3.Minutes;

            return SendCommandBasic(cmd);
        }

        public bool SendCommand(byte cmd, DateTime dt)
        {
            ClearSendBuffer();

            _inBuff[4] = (byte)dt.Hour;
            _inBuff[5] = (byte)dt.Minute;
            _inBuff[6] = (byte)dt.Second;
            _inBuff[7] = (byte)dt.Day;
            _inBuff[8] = (byte)dt.Month;
            _inBuff[9] = (byte)(dt.Year - 2000);
            _inBuff[10] = (byte)dt.DayOfWeek;

            return SendCommandBasic(cmd);
        }


        public bool SendCommand(byte cmd, bool checkSendConfirmation = false)
        {
            ClearSendBuffer();
            return SendCommandBasic(cmd, checkSendConfirmation);
        }

        /// <summary>
        /// posle prikaz na port
        /// </summary>
        /// <returns></returns>
        public bool SendCommandBasic(byte cmd, bool checkSendConfirmation = false)
        {
            bool res;
            Logger.Debug("+");

            PrepareCommand(cmd);

            if (!_sp.IsOpen) return false;

            try
            {
                //odeslat pripraveny command s parametry
                TplLogger.Debug($" SendCommand {cmd} to [{_address}] - start");

                _sp.Write(_inBuff, 0, BuffLength);
                DataSendReceivedLogger.Debug($"->  {BuffLength}; {BitConverter.ToString(_inBuff)}");

                Thread.Sleep(10);

                Logger.Debug(" -w");

                if (checkSendConfirmation)
                {
                    var sendConfirmReceived = WaitEventSendConfirm.WaitOne(_runConfig.SendCommandTimeOut);
                    TplLogger.Debug($" SendCommand {cmd} to [{_address}] - confirmed:{sendConfirmReceived}");
                    return sendConfirmReceived;
                }

                Thread.Sleep(10);

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
    }
}