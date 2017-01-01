using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.DAO;
using Slevyr.DataAccess.Model;

namespace Slevyr.DataAccess.Services
{
    public static class SlevyrService
    {
        #region Fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger DataSendReceivedLogger = LogManager.GetLogger("DataReceived");
        private static readonly Logger ErrorsLogger = LogManager.GetLogger("Errors");
        private static readonly Logger TplLogger = LogManager.GetLogger("Tpl");

        private static volatile object _lock = new object();

        private static SerialPortWraper _serialPort;

        private static Dictionary<int, UnitMonitor> _unitDictionary;

        private static RunConfig _runConfig = new RunConfig();

        private static readonly ByteQueue ReceivedByteQueue = new ByteQueue();

        private static readonly BlockingCollection<byte[]> PackedCollection = new BlockingCollection<byte[]>();

        private static readonly ConcurrentQueue<UnitCommand> UnitCommandsQueue = new ConcurrentQueue<UnitCommand>();

        private static BackgroundWorker _sendBw;

        private static BackgroundWorker _packetBw;

        private static bool _isSendWorkerStarted;
        private static bool _isPacketWorkerStarted;
        private static int _sendWorkerCycleCnt;

        #endregion

        #region Properties

        public static RunConfig Config
        {
            get { return _runConfig; }
            set { _runConfig = value; }
        }

        public static bool SerialPortIsOpen => _serialPort.IsOpen;

        private static IEnumerable<int> UnitAddresses => _runConfig.UnitAddrs;

        public static int UnitCount => _unitDictionary.Count;

        #endregion

        #region init method


        public static void Stop()
        {
            StopPacketWorker();

            StopSendWorker();

            ClosePort();
        }

        public static void Start()
        {
            OpenPort();

            StartSendWorker();

            StartPacketWorker();
        }


        public static void Init(SerialPortConfig portCfg, RunConfig runConfig)
        {
            Logger.Info("+");

            _serialPort = new SerialPortWraper(portCfg);

            _runConfig = runConfig;

            _unitDictionary = new Dictionary<int, UnitMonitor>();
            foreach (var a in runConfig.UnitAddrs)
            {
                var um = new UnitMonitor((byte) a, _runConfig);
                um.UnitStatus.PrechodSmeny += UnitStatusOnPrechodSmeny;
                _unitDictionary.Add(a, um);
            }

            foreach (var m in _unitDictionary.Values)
            {
                 m.LoadUnitConfigFromFile(m.Address, _runConfig.DataFilePath);
            }

            _serialPort.DataReceived += SerialPortOnDataReceived;

            _serialPort.ErrorReceived += _serialPort_ErrorReceived;

            Logger.Info("Unit count: " + _unitDictionary.Count);
        
            Logger.Info("-");
        }

        /// <summary>
        /// je vyvolan po prechodu smeny, napr. z nocni na ranni
        /// </summary>
        /// <param name="sender">objekt UnitStatus</param>
        /// <param name="smena">Smena, hodnota enumu je prikaz ktery se ma pouzit pro vycteni citacu po skonceni smeny</param>
        private static void UnitStatusOnPrechodSmeny(object sender, SmenyEnum smena)
        {
            UnitStatus us = sender as UnitStatus;

            int cmd = (int) smena;

            Logger.Info($"prechod smeny addr:{us.Addr} cmd:{cmd:x2}");

            //prikaz na vycteni hodnoty se posila po zpozdeni 2sec 
            //TODO parametr do config
            Task.Delay(2000).ContinueWith(t =>
            {
                var uc = new UnitCommand(() => _unitDictionary[us.Addr].SendCommand((byte)cmd), "CmdReadStavCitacuKonecSmeny", us.Addr);
                UnitCommandsQueue.Enqueue(uc);
            });
        }


        private static void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Logger.Error($"--- Serial port error received: {e.EventType} ---" );
            ErrorsLogger.Error($"--- Serial port error received: {e.EventType} ---");
        }

        private static void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs serialDataReceivedEventArgs)
        {
            SerialPort sp = (SerialPort)sender;

            if (!sp.IsOpen) return;

            var len = sp.BytesToRead;

            Byte[] buf = new Byte[len];

            sp.Read(buf, 0, len);

            DataSendReceivedLogger.Debug($" <- {len:00}; {BitConverter.ToString(buf)}");

            ReceivedByteQueue.Enqueue(buf,0,len);

            if (ReceivedByteQueue.Length >= 11)
            {
                Byte[] packet = new Byte[11];
                ReceivedByteQueue.Dequeue(packet, 0, 11);

                if (_serialPort.ReceivedBytesThreshold < 11)
                {
                    DataSendReceivedLogger.Debug($"        {BitConverter.ToString(packet)}");
                }

                //signatura potvrzeni odeslani
                //  _outBuff[0] == 4 && _outBuff[1] == 0 && _outBuff[2] == _address
                //signatura prichozich dat - response
                //  _outBuff[0] == 0 && _outBuff[1] == 0 && _outBuff[2] == _address &&  && _outBuff[2] == cmd

                var adr = packet[2];

                if (!_unitDictionary.ContainsKey(adr))
                {
                    Logger.Error($"unit [{adr}] not found");
                    ErrorsLogger.Error($"unit [{adr}] not found");
                    return;
                }

                var isSendConfirmation = packet[0] == 4 && packet[1] == 0 && packet[2] > 0;
                if (isSendConfirmation)
                {                   
                    _unitDictionary[adr].WaitEventSendConfirm.Set();
                    TplLogger.Debug($"  WaitEventSendConfirm.Set - adr:[{adr:x2}]");
                    return;
                }

                var cmd = packet[3];

                var isResult = packet[0] == 0 && packet[1] == 0 && packet[2] > 0 && cmd > 0;
                if (isResult)
                {
                    //oznamim jednotce ze byla prijata response na prikaz cmd
                    _unitDictionary[adr].ResponseReceived(cmd);
                    TplLogger.Debug($"  Response received cmd:{cmd:x2} from [{adr:x2}]");

                    if (_runConfig.IsWaitCommandResult)
                    {
                        _unitDictionary[adr].WaitEventCommandResult.Set();   // rozlisit podle typu prikazu ?
                        TplLogger.Debug($"  WaitEventCommandResult.Set - adr:[{adr:x2}]");
                    }

                    switch (cmd)
                    {
                        case UnitMonitor.CmdReadStavCitacu:
                        case UnitMonitor.CmdReadCasPosledniOkNg:
                        case UnitMonitor.CmdReadRozdilKusu:
                        case UnitMonitor.CmdReadDefektivita:
                        case UnitMonitor.CmdReadHodnotuPrumCykluOkNg:
                            //zaradim ke zpracovani ktere probiha v PacketBw
                            PackedCollection.Add(packet);
                            break;
                    }                                  
                }
            }           
        }

        #endregion

        #region open close port

        public static bool OpenPort()
        {
            Logger.Info("+ Open port {_serialPort.PortName} baud rate: {_serialPort.BaudRate}");
            try
            {
                if (_runConfig.IsMockupMode) return true;

                if (!CheckIsPortOpen())
                {
                    lock (_lock)
                    {
                        _serialPort.Open();
                    }
                }

                Logger.Info($"Port open: {_serialPort.IsOpen}");

                return _serialPort.IsOpen;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public static bool ClosePort()
        {
            Logger.Info("+");

            if (_runConfig.IsMockupMode) return true;
            if (CheckIsPortOpen())
            {
                lock (_lock)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                    _serialPort.Close();
                }
            }
            Logger.Info("-");
            return !_serialPort.IsOpen;
        }

        #endregion

        #region UnitStatus operations

        public static UnitStatus GetUnitStatus(byte addr)
        {
            Logger.Debug($"+ {addr}");

            return _unitDictionary[addr].UnitStatus;
        }

        public static IEnumerable<UnitTabule> GetAllTabule()
        {
            Logger.Debug("+");

            return _unitDictionary.Values.Select(um => um.UnitStatus.Tabule).ToList();
        }


        /// <summary>
        /// prepocitat pro zobrazeni tabule
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static UnitStatus UpdateUnitStatus(byte addr)
        {

            try
            {
                _unitDictionary[addr].RecalcTabule();

                _unitDictionary[addr].UnitStatus.LastCheckTime = DateTime.Now;

                /* hodne stary zapis do CSV
                string casOkStr = (_runConfig.IsReadOkNgTime)
                    ? casOk.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                string casNgStr = (_runConfig.IsReadOkNgTime)
                    ? casNg.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                UnitsLogger.Info($"4;{addr};{ok};{ng};{casOkStr};{casNgStr};{(int)_unitDictionary[addr].UnitStatus.MachineStatus}");
                */

                /* stary zapis do CSV
                StringBuilder sb = new StringBuilder();
                sb.Append($"4;{_unitDictionary[addr].UnitConfig.UnitName};{addr}"); //az po 4
                sb.Append($";{_unitDictionary[addr].UnitStatus.CilKusuTabule}"); //5
                sb.Append($";{ok}"); //6
                sb.Append(_runConfig.IsReadOkNgTime ? $";{casOk}" : ";"); //7
                sb.Append(ok != 0 ? $";{_unitDictionary[addr].UnitStatus.UbehlyCasSmenySec / (float)ok:F}" : ";");
                sb.Append($";{_unitDictionary[addr].UnitStatus.CilDefectTabule:F}"); //9
                sb.Append($";{ng}"); //10
                sb.Append(_runConfig.IsReadOkNgTime ? $";{casNg}" : ";"); //11
                sb.Append(ng != 0 ? $";{_unitDictionary[addr].UnitStatus.UbehlyCasSmenySec / (float)ng:F}" : ";"); //12
                sb.Append($";{_unitDictionary[addr].UnitStatus.RozdilTabuleTxt}"); //13
                sb.Append($";{_unitDictionary[addr].UnitStatus.AktualDefectTabuleTxt}"); //14
                sb.Append($";{_unitDictionary[addr].UnitStatus.MachineStatus}"); //15
                sb.Append($";{Convert.ToInt32(_unitDictionary[addr].UnitStatus.IsPrestavkaTabule)}"); //16
                UnitsLogger.Info(sb.ToString);
                */

                SqlliteDao.AddUnitState(addr, _unitDictionary[addr].UnitStatus);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return _unitDictionary[addr].UnitStatus;
        }


        public static bool NastavStatus(byte addr, bool writeProtectEEprom, byte minOK, byte minNG, bool bootloaderOn, byte parovanyLED,
            byte rozliseniCidel, byte pracovniJasLed)
        {
            Logger.Debug($"addr:{addr} writeProtectEEprom:{writeProtectEEprom} minOK:{minOK} minNG:{minNG} parovanyLED:{parovanyLED}");

            if (_runConfig.IsMockupMode) return true;

            byte writeProtectEEpromVal = (byte)(writeProtectEEprom ? 0 : 1);
            byte bootloaderOnVal = (byte)(bootloaderOn ? 0 : 1);
            //return _unitDictionary[addr].SendSetZaklNastaveni(writeProtectEEpromVal, minOK, minNG, bootloaderOnVal, parovanyLED, rozliseniCidel, pracovniJasLed);
            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetZaklNastaveni(writeProtectEEpromVal, minOK, minNG, bootloaderOnVal, parovanyLED, rozliseniCidel, pracovniJasLed), "SendSetZaklNastaveni", addr);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }


        public static bool NastavCileSmen(byte addr, char varianta, short cil1, short cil2, short cil3)
        {
            Logger.Debug($"addr:{addr} var:{varianta} cil1:{cil1} cil2:{cil2} cil3:{cil3}");

            if (_runConfig.IsMockupMode) return true;

            //return _unitDictionary[addr].SendSetCileSmen(varianta, cil1, cil2, cil3);
            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetCileSmen(varianta, cil1, cil2, cil3), "SendSetCileSmen", addr);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        public static bool NastavPrestavky(byte addr, char varianta, TimeSpan prest1, TimeSpan prest2, TimeSpan prest3)
        {
            Logger.Debug($"addr:{addr} var:{varianta} prest1:{prest1} prest2:{prest2} prest3:{prest3}");

            if (_runConfig.IsMockupMode) return true;

            //return _unitDictionary[addr].SendSetPrestavky(varianta, prest1, prest2, prest3);
            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetPrestavky(varianta, prest1, prest2, prest3), "SendSetPrestavky", addr);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        public static bool NastavOkNg(byte addr, short ok, short ng)
        {
            Logger.Debug($"addr:{addr} ok:{ok} ng:{ng}");

            if (_runConfig.IsMockupMode) return true;

            //return _unitDictionary[addr].SendSetCitace(ok, ng);
            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetCitace(ok, ng), "SendSetCitace", addr);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        public static void NastavAktualniCas(int addr)
        {
            Logger.Debug("");

            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetCas(DateTime.Now), "NastavAktualniCas", addr);
            UnitCommandsQueue.Enqueue(uc);           
        }


        public static bool NastavDefektivitu(byte addr, char varianta, short def1Val, short def2Val, short def3Val)
        {
            Logger.Debug($"addr:{addr} varianta:{varianta} def1:{def1Val} def2:{def2Val} def3:{def3Val}");

            if (_runConfig.IsMockupMode) return true;
            
            Logger.Debug($"cil1:{def1Val}");
            //return _unitDictionary[addr].SendSetDefektivita(varianta, def1Val, def2Val, def3Val );
            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetDefektivita(varianta, def1Val, def2Val, def3Val), "SendSetDefektivita", addr);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }


        public static bool NastavHandshake(byte addr, bool value)
        {
            Logger.Debug($"addr:{addr} val:{value}");

            if (_runConfig.IsMockupMode) return true;

            var um = _unitDictionary[addr];

            var res = um.SendSetHandshake(value ? (byte)1 : (byte)255, 0);

            return res;
        }


        public static bool SendCtiStavCitacu(byte addr)
        {
            Logger.Debug("+");

            if (_runConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().Ok++;
                Mock.MockUnitStatus().Ng++;
                return true;
            }

            //return _unitDictionary[addr].SendReadStavCitacu();

            var uc = new UnitCommand(() => _unitDictionary[addr].SendReadStavCitacu(), "SendReadStavCitacu", addr);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }


        public static bool SendCtiCyklusOkNg(byte addr)
        {
            Logger.Debug("+");

            if (_runConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().CasOkNgTime = DateTime.Now;
                return true;
            }
          
            //return _unitDictionary[addr].SendReadCasOkNg() ;

            var uc = new UnitCommand(() => _unitDictionary[addr].SendReadCasOkNg(), "SendReadCasOkNg", addr);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        #endregion

        #region save/load/get unitConfig

        public static void SaveUnitConfig(UnitConfig unitCfg)
        {
            Logger.Debug($"addr:{unitCfg.Addr}");

            _unitDictionary[unitCfg.Addr].UnitConfig = unitCfg;
            unitCfg.SaveToFile(_runConfig.DataFilePath);                        
        }

        public static UnitConfig GetUnitConfig(byte addr)
        {
            Logger.Debug($"addr:{addr}");

            //nacteno je jiz pri startu
            //_unitDictionary[addr].LoadUnitConfigFromFile(addr, _runConfig.DataFilePath);
            return _unitDictionary[addr].UnitConfig;          
        }

        #endregion

        #region send a packet worker

        public static void StartSendWorker()
        {
            if (!_isSendWorkerStarted)
            {
                _isSendWorkerStarted = true;
                _sendBw = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
                _sendBw.DoWork += SendBwDoWork;
                _sendBw.RunWorkerAsync();
                Logger.Info("*** send worker started ***");
            }
        }

        public static void StartPacketWorker()
        {
            if (!_isPacketWorkerStarted)
            {
                _isPacketWorkerStarted = true;
                _packetBw = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
                _packetBw.DoWork += PacketBwDoWork;
                _packetBw.RunWorkerAsync();
                Logger.Info("*** packet worker started ***");
            }
        }

        public static void StopSendWorker()
        {
            _sendBw.CancelAsync();
        }

        public static void StopPacketWorker()
        {
            _packetBw.CancelAsync();
        }

        private static void SendBwDoWork(object sender, DoWorkEventArgs e)
        {
            OpenPort();
            Stopwatch stopwatch = null;

            while (true)
            {
                Logger.Info($"worker cycle {++_sendWorkerCycleCnt}");

                try
                {
                    UnitCommand unitCommand;

                    //pokud jsou prikazy cekajici na zpracovani tak se provedou prednostne
                    while (UnitCommandsQueue.TryDequeue(out unitCommand))
                    {
                        Logger.Debug($"command {unitCommand.Description} dequeue");
                        unitCommand.Run();
                        Thread.Sleep(_runConfig.RelaxTime);
                        Logger.Debug($"command invoke res:{unitCommand.Result}");
                    }

                    if (stopwatch == null)
                    {
                        stopwatch = new Stopwatch();
                    }
                    else
                    {
                        long remainingDelay = _runConfig.MinCmdDelay - stopwatch.ElapsedMilliseconds;
                        if (remainingDelay > 0)
                        {
                            Logger.Debug($"+remaining min. delay: {remainingDelay}");
                            Thread.Sleep((int)remainingDelay);  //zbyva do min. pauzy pred odeslanim prikazu na jednotku
                        }
                        stopwatch.Reset();
                    }

                    stopwatch.Start(); //merim cas za ktery se provede odeslani pozadavku na vsechny jednotky

                    //provedu odeslani pozadavku na vsechny jednotky
                    foreach (var addr in UnitAddresses)
                    {
                        if (_sendBw.CancellationPending)
                        {
                            Logger.Info("*** send worker canceled ***");
                            _isSendWorkerStarted = false;
                            return;
                        }

                        bool res = false;

                        if (_runConfig.IsWaitCommandResult)
                        {
                            res = _unitDictionary[addr].ObtainStatusSync();
                        }
                        else
                        {
                            if (_unitDictionary[addr].IsCommandPending) 
                            {
                                TplLogger.Info($"..Command {_unitDictionary[addr].CurrentCmd:x2}  [{addr:x2}] is pending..");
                                if ((DateTime.Now - _unitDictionary[addr].CurrentCmdStartTime).TotalMilliseconds > _runConfig.ReadResultTimeOut)
                                {
                                    TplLogger.Error($"Timeout cmd:{_unitDictionary[addr].CurrentCmd:x2} [{addr:x2}]");
                                    _unitDictionary[addr].UpdateStatusTokenSource?.Cancel();
                                    _unitDictionary[addr].SetCommandIsPending(false);
                                    _unitDictionary[addr].UnitStatus.MachineStatus = MachineStateEnum.NotAvailable;
                                }
                            }
                            else
                            {
                                res = _unitDictionary[addr].ObtainStatusAsync();
                            }
                        }

                        if (!res)
                        {
                            _unitDictionary[addr].UnitStatus.MachineStatus = MachineStateEnum.NotAvailable;
                        }

                        Logger.Debug($"+send worker sleep: {_runConfig.WorkerSleepPeriod}");
                        Thread.Sleep(_runConfig.WorkerSleepPeriod);  //pauza pred odeslanim prikazu na dalsi jednotku - parametr
                    }

                    stopwatch.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }          
        }


        /// <summary>
        /// zde se uz pouze zpracovavaji ziskana response data z jednotek
        /// a to vcetne prepoctu tabule a ulozeni stavu jednotky do databaze
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PacketBwDoWork(object sender, DoWorkEventArgs e)
        {
            SqlliteDao.OpenConnection(true);

            while (true)
            {
                try
                {
                    if (_packetBw.CancellationPending)
                    {
                        Logger.Info("*** packet worker canceled ***");
                        SqlliteDao.CloseConnection();
                        return;
                    }

                    var packet =PackedCollection.Take();

                    byte addr = packet[2];
                    byte cmd = packet[3];

                    Logger.Debug($"addr:{addr} cmd:{cmd}");

                    switch (cmd)
                    {
                       case UnitMonitor.CmdReadStavCitacu:
                            _unitDictionary[addr].DoReadStavCitacu(packet);
                            _unitDictionary[addr].RecalcTabule();
                            _unitDictionary[addr].LastId = SqlliteDao.AddUnitState(addr, _unitDictionary[addr].UnitStatus);
                            break;
                        case UnitMonitor.CmdReadCasPosledniOkNg:
                            _unitDictionary[addr].DoReadCasOkNg(packet);
                            SqlliteDao.UpdateUnitStateCasOk(addr, _unitDictionary[addr].LastId, _unitDictionary[addr].UnitStatus);
                            //_unitDictionary[addr].SetCommandIsPending(false);
                            break;
                        case UnitMonitor.CmdReadRozdilKusu:
                            _unitDictionary[addr].DoReadRozdilKusu(packet);
                            break;
                        case UnitMonitor.CmdReadDefektivita:
                            _unitDictionary[addr].DoReadDefectivita(packet);
                            break;
                        case UnitMonitor.CmdReadHodnotuPrumCykluOkNg:
                            //_unitDictionary[addr].DoReadDefectivita(packet);
                            break;
                        case UnitMonitor.CmdReadStavCitacuNocniSmena:
                        case UnitMonitor.CmdReadStavCitacuOdpoledniSmena:
                        case UnitMonitor.CmdReadStavCitacuRanniSmena:
                            _unitDictionary[addr].DoReadStavCitacuKonecSmeny(packet);
                            SqlliteDao.AddUnitKonecSmenyState(addr, cmd, _unitDictionary[addr].UnitStatus);
                            break;
                    }                    

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        #endregion

        public static void DiscardOutBuffer()
        {
            lock (_lock)
            {
                if (_serialPort.IsOpen)
                  _serialPort.DiscardOutBuffer(); //zkusime - asi to neni potreba ale kdovi. Ale muze to degradovat vykon.
            }
        }

        public static bool CheckIsPortOpen()
        {
            bool res ;
            lock (_lock)
            {
                res = _serialPort.IsOpen;
            }
            return res;
        }

        public static void WriteToPort(byte[] inBuff, int buffLength)
        {
            lock (_lock)
            {
                _serialPort.Write(inBuff, 0, buffLength);
            }
            DataSendReceivedLogger.Debug($"->  {buffLength}; {BitConverter.ToString(inBuff)}");
        }
      
    }
}
