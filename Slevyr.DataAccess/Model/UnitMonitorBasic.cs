using System;
using System.Threading;
using NLog;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitMonitorBasic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger TplLogger = LogManager.GetLogger("Tpl");

        private readonly byte _address;
        public readonly byte[] _sendBuff;    //TODO public jen pro stary zpusob komunikace, zrusit hned jak to bude mozne

        private static RunConfig _runConfig;

        public const int BuffLength = 11;

        public byte Address => _address;

        public byte CurrentCmd { get; set; }


        protected RunConfig RunConfig => _runConfig;

        public readonly AutoResetEvent WaitEventCommandResult = new AutoResetEvent(false);
        public readonly AutoResetEvent WaitEventSendConfirm = new AutoResetEvent(false);

        protected UnitMonitorBasic(byte address,  RunConfig runConfig)
        {
            _runConfig = runConfig;

            _address = address;

            _sendBuff = new byte[BuffLength];
            _sendBuff[2] = address;
        }

        private void ClearSendBuffer()
        {
            Array.Clear(_sendBuff, 0, _sendBuff.Length);
            SlevyrService.DiscardOutBuffer();           
        }

        //pripravime send/in buffer na predani příkazu
        private void PrepareCommand(byte cmd)
        {
            _sendBuff[2] = _address;
            _sendBuff[3] = cmd;
        }

        protected void OldPrepareCommand(byte cmd)
        {
            CurrentCmd = cmd;
            Array.Clear(_sendBuff, 0, _sendBuff.Length);
            _sendBuff[2] = _address;
            _sendBuff[3] = cmd;
        }

        protected void DiscardBuffers()
        {
            Logger.Info("");
            SlevyrService.DiscardInBuffer();
            SlevyrService.DiscardOutBuffer();
        }

        public bool SendCommand(byte cmd, byte p1, bool checkSendConfirmation = true)
        {

            return SendCommandLambda(cmd, () => { _sendBuff[4] = p1;
                                              return true;
            }, checkSendConfirmation);            
        }


        public bool SendCommand(byte cmd, byte p1, byte p2, bool checkSendConfirmation = true)
        {
            return SendCommandLambda(cmd, () => {
                _sendBuff[4] = p1;
                _sendBuff[5] = p2;
                return true;
            }, checkSendConfirmation);            
        }

        public bool SendCommand(byte cmd, byte p1, byte p2, byte p3, byte p4, byte p5, byte p6, byte p7, bool checkSendConfirmation = true)
        {
            return SendCommandLambda(cmd, () => {
                _sendBuff[4] = p1;
                _sendBuff[5] = p2;
                _sendBuff[6] = p3;
                _sendBuff[7] = p4;
                _sendBuff[8] = p5;
                _sendBuff[9] = p6;
                _sendBuff[10] = p7;
                return true;
            }, checkSendConfirmation);            
        }

        public bool SendCommand(byte cmd, byte p1, short s1, short s2, short s3, bool checkSendConfirmation = true)
        {
            return SendCommandLambda(cmd, () =>
            {
                _sendBuff[4] = p1;
                Helper.FromShort(s1, out _sendBuff[5], out _sendBuff[6]);
                Helper.FromShort(s2, out _sendBuff[7], out _sendBuff[8]);
                Helper.FromShort(s3, out _sendBuff[9], out _sendBuff[10]);
                return true;
            }, checkSendConfirmation);
        }

        public bool SendCommand(byte cmd,  short s1, short s2, bool checkSendConfirmation = true)
        {
            return SendCommandLambda(cmd, () =>
            {
                Helper.FromShort(s1, out _sendBuff[4], out _sendBuff[5]); Helper.FromShort(s2, out _sendBuff[6], out _sendBuff[7]);
                return true;
            }, checkSendConfirmation);       
        }

        public bool SendCommand(byte cmd, byte p1, TimeSpan prest1, TimeSpan prest2, TimeSpan prest3, bool checkSendConfirmation = true)
        {
            return SendCommandLambda(cmd, () =>
            {
                _sendBuff[4] = p1;
                _sendBuff[5] = (byte)prest1.Hours;
                _sendBuff[6] = (byte)prest1.Minutes;

                _sendBuff[7] = (byte)prest2.Hours;
                _sendBuff[8] = (byte)prest2.Minutes;

                _sendBuff[9] = (byte)prest3.Hours;
                _sendBuff[10] = (byte)prest3.Minutes;
                return true;
            }, checkSendConfirmation);
        }

        public bool SendCommand(byte cmd, DateTime dt, bool checkSendConfirmation = true)
        {
            return SendCommandLambda(cmd, () =>
            {
                _sendBuff[4] = (byte)dt.Hour;
                _sendBuff[5] = (byte)dt.Minute;
                _sendBuff[6] = (byte)dt.Second;
                _sendBuff[7] = (byte)dt.Day;
                _sendBuff[8] = (byte)dt.Month;
                _sendBuff[9] = (byte)(dt.Year - 2000);
                _sendBuff[10] = (byte)dt.DayOfWeek;
                return true;
            }, checkSendConfirmation);
        }


        public bool SendCommand(byte cmd, bool checkSendConfirmation = true)
        {
            return SendCommandLambda(cmd, () => true, checkSendConfirmation);            
        }


        private bool SendCommandLambda(byte cmd, Func<bool> lambda, bool checkSendConfirmation)
        {
            int i = 1;
            bool res = false;
            while (!res)
            {
                ClearSendBuffer();

                lambda();

                res = SendCommandCore(cmd, checkSendConfirmation);

                if (!checkSendConfirmation || i++ >= RunConfig.SendAttempts) break;
            }
            return res;
        }

        /// <summary>
        /// posle prikaz na port
        /// </summary>
        /// <returns></returns>
        private bool SendCommandCore(byte cmd, bool checkSendConfirmation = false)
        {
            bool res = false;
            Logger.Debug($"+ cmd:{cmd}");

            PrepareCommand(cmd);

            if (!SlevyrService.CheckIsPortOpen()) return false;

            try
            {
                //odeslat pripraveny command s parametry
                SlevyrService.WriteToPort(_sendBuff, BuffLength);

                Thread.Sleep(10);

                Logger.Debug(" -w");

                if (checkSendConfirmation)
                {
                    WaitEventSendConfirm.Reset();
                    var sendConfirmReceived = WaitEventSendConfirm.WaitOne(_runConfig.SendCommandTimeOut);
                    var r = (sendConfirmReceived) ? "confirmed" : "expired";

                    TplLogger.Debug($"  SendCommand {cmd:x2} to [{_address:x2}] : send {r}");
                    res = sendConfirmReceived;
                }

                if (_runConfig.IsWaitCommandResult)
                {
                    WaitEventCommandResult.Reset(); //protoze result mohl prijit necekane po timout-u a mohl byt tudiz ve stavu signaled
                }

                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            Logger.Debug($"- cmd:{cmd} res:{res}");

            return res;
        }
    }
}