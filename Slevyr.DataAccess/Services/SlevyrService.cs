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
        private static readonly Logger ResetLogger = LogManager.GetLogger("ResetRf");
        private const int ReadAsyncTimeout = 1000;

        private static volatile object _lock = new object();

        private static SerialPortWraper _serialPort;

        private static Dictionary<int, UnitMonitor> _unitDictionary;

        private static RunConfig _runConfig = new RunConfig();

        //private static readonly ByteQueue ReceivedByteQueue = new ByteQueue();
        private static readonly ReadWriteBuffer ReceivedBuffer = new ReadWriteBuffer(1024);

        private static readonly BlockingCollection<byte[]> PackedCollection = new BlockingCollection<byte[]>();

        private static readonly ConcurrentQueue<UnitCommand> UnitCommandsQueue = new ConcurrentQueue<UnitCommand>();

        private static BackgroundWorker _sendBw;

        private static BackgroundWorker _packetBw;

        private static BackgroundWorker _dataReaderBw;

        public static readonly AutoResetEvent WaitEventPriorityCommandResult = new AutoResetEvent(false);

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

        //TODO je vystaveno jen kvuli staremu zpusobu komunikace, jak to bude mozne tak tohle zrusit
        public static SerialPortWraper SerialPort => _serialPort;

        #endregion

        #region init method


        public static void Stop()
        {
            StopPacketWorker();

            StopSendReceiveWorkers();

            Thread.Sleep(ReadAsyncTimeout * 3);

            ClosePort();
        }

        public static bool Start()
        {
            var res = OpenPort();

            if (res)
            {
                StartSendReceiveWorkers();
                StartPacketWorker();
            }

            return res;
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
                 m.LoadUnitConfigFromFile(m.Address, _runConfig.JsonDataFilePath);
            }

            if (runConfig.UseDataReceivedEvent)
            {
                _serialPort.DataReceived += SerialPortOnDataReceived;
                _serialPort.ErrorReceived += _serialPort_ErrorReceived;
            }

            Logger.Info("Unit count: " + _unitDictionary.Count);
        
            Logger.Info("-");
        }

        /// <summary>
        /// je vyvolan po prechodu smeny, napr. z nocni na ranni
        /// </summary>
        /// <param name="sender">objekt UnitStatus</param>
        /// <param name="smena">Smena, hodnota enumu je prikaz ktery se ma pouzit pro vycteni citacu po skonceni smeny, tzn. ta co prave skoncila</param>
        private static void UnitStatusOnPrechodSmeny(object sender, SmenyEnum smena)
        {
            UnitStatus us = sender as UnitStatus;

            int cmd = (int) smena;

            Logger.Info($"prechod smeny {smena} addr:{us.Addr} cmd:{cmd:x2}");

            //prikaz na vycteni hodnoty se posila po zpozdeni 2sec 
            //TODO parametr do config
            Task.Delay(2000).ContinueWith(t =>
            {
                var uc = new UnitCommand(() => _unitDictionary[us.Addr].SendReadStavCitacuKonecSmeny(cmd), "CmdReadStavCitacuKonecSmeny", us.Addr, cmd);
                UnitCommandsQueue.Enqueue(uc);
            });
        }

        private static void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Logger.Error($"--- Serial port error received: {e.EventType} ---" );
            ErrorsLogger.Error($"--- Serial port error received: {e.EventType} ---");
        }

        /// <summary>
        /// obsluha datareceived eventu - pouziva se jen v rezimu UseDataReceivedEvent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="serialDataReceivedEventArgs"></param>
        private static void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs serialDataReceivedEventArgs)
        {
            SerialPort sp = (SerialPort)sender;

            if (!sp.IsOpen) return;

            var len = sp.BytesToRead;

            Byte[] buf = new Byte[len];

            sp.Read(buf, 0, len);

            DataSendReceivedLogger.Debug($" <- {len:00}; {BitConverter.ToString(buf)}");

            ReceivedBuffer.Write(buf);

            while (ReceivedBuffer.Count >= 11)
            {
                Byte[] packet = ReceivedBuffer.Read(11);

                if (_serialPort.ReceivedBytesThreshold < 11)
                {
                    DataSendReceivedLogger.Debug($"        {BitConverter.ToString(packet)}");
                }

                DataSendReceivedLogger.Debug($" -- {ReceivedBuffer.Count:00}; {BitConverter.ToString(packet)}");

                //signatura potvrzeni odeslani
                //  _outBuff[0] == 4 && _outBuff[1] == 0 && _outBuff[2] == _address
                //signatura prichozich dat - response
                //  _outBuff[0] == 0 && _outBuff[1] == 0 && _outBuff[2] == _address &&  && _outBuff[2] == cmd

                var adr = packet[2];
                var cmd = packet[3];

                if (!_unitDictionary.ContainsKey(adr))
                {
                    Logger.Error($"unit [{adr}] not found");
                    ErrorsLogger.Error($"unit [{adr}] not found");
                    return;
                }

                var isSendConfirmation = packet[0] == 4 && packet[1] == 0 && adr > 0;
                if (isSendConfirmation)
                {                   
                    _unitDictionary[adr].WaitEventSendConfirm.Set();
                    TplLogger.Debug($"  WaitEventSendConfirm.Set - adr:[{adr:x2}]");
                    return;
                }
                
                var isResult = packet[0] == 0 && packet[1] == 0 && adr > 0 && cmd > 0;
                if (isResult)
                {
                    //oznamim jednotce ze byla prijata response na prikaz cmd
                    _unitDictionary[adr].ResponseReceived(cmd);
                    TplLogger.Debug($"  Response received cmd:{cmd:x2} from [{adr:x2}]");

                    if (_runConfig.IsWaitCommandResult)
                    {
                        _unitDictionary[adr].WaitEventCommandResult.Set();   // TODO rozlisit podle typu prikazu ?
                        TplLogger.Debug($"  WaitEventCommandResult.Set - adr:[{adr:x2}]");
                    }

                    switch (cmd)
                    {
                        case UnitMonitor.CmdReadStavCitacu:
                        case UnitMonitor.CmdReadCasPosledniOkNg:
                        case UnitMonitor.CmdReadRozdilKusu:
                        case UnitMonitor.CmdReadDefektivita:
                        case UnitMonitor.CmdReadHodnotuPrumCykluOkNg:
                        case UnitMonitor.CmdReadStavCitacuRanniSmena:
                        case UnitMonitor.CmdReadStavCitacuOdpoledniSmena:
                        case UnitMonitor.CmdReadStavCitacuNocniSmena:
                            //zaradim ke zpracovani ktere probiha v PacketBw
                            PackedCollection.Add(packet);
                            break;
                    }                                  
                }
            }           
        }

        static CancellationTokenSource _readAsyncCancellationTokenSource;
        private static bool _resetRfInProgress;

        private static async void DatareadWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            OpenPort();
            if (!CheckIsPortOpen()) return;
            _readAsyncCancellationTokenSource = new CancellationTokenSource();
            _serialPort.DiscardInBuffer();

            while (true)
            {
                //DataSendReceivedLogger.Debug(".");
                if (_dataReaderBw.CancellationPending)
                {
                    e.Cancel = true;
                    Logger.Info("*** datareader worker canceled ***");
                    return;
                }

                byte[] packet = null;

                try
                {
                    var task = SerialPort.ReadAsync(UnitMonitorBasic.BuffLength, _readAsyncCancellationTokenSource.Token);
                    await task;
                    if (!task.IsCompleted)
                    {
                        Logger.Error("ReadAsync not completed");
                        continue;
                    }

                    packet = task.Result;                  
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    throw;
                }

                if (packet != null && packet.Length == UnitMonitorBasic.BuffLength)
                {
                    DataSendReceivedLogger.Debug($" <- {11:00}; {BitConverter.ToString(packet)}");

                    if (packet[0] == 0 && packet[1] == 0 && packet[2] == 0 && packet[3] == 0)
                    {
                        Logger.Error($"Null packet");
                        continue;
                    }


                    if (packet[0] == 4 && packet[1] == 0 && packet[2] == 0 && packet[3] == 0 && packet[4] == 0)  //testovaci packet
                    {
                        TplLogger.Debug($"test packet");
                        _unitDictionary.FirstOrDefault().Value.WaitEventSendConfirm.Set();
                        continue;
                    }

                    var adr = packet[2];
                    var cmd = packet[3];

                    if (!_unitDictionary.ContainsKey(adr))
                    {
                        Logger.Error($"unit [{adr}] not found");
                        ErrorsLogger.Error($"unit [{adr}] not found");
                        continue;
                    }

                    var isSendConfirmation = packet[0] == 4 && packet[1] == 0 && adr > 0;
                    if (isSendConfirmation)
                    {
                        _unitDictionary[adr].WaitEventSendConfirm.Set();
                        TplLogger.Debug($"  WaitEventSendConfirm.Set - adr:[{adr:x2}]");
                        continue;
                    }

                    var isResult = packet[0] == 0 && packet[1] == 0 && adr > 0 && cmd > 0;
                    if (isResult)
                    {
                        //oznamim jednotce ze byla prijata response na prikaz cmd
                        _unitDictionary[adr].ResponseReceived(cmd);
                        TplLogger.Debug($"  Response received cmd:{cmd:x2} from [{adr:x2}]");

                        if (_runConfig.IsWaitCommandResult)
                        {
                            _unitDictionary[adr].WaitEventCommandResult.Set(); // rozlisit podle typu prikazu ?
                            TplLogger.Debug($"  WaitEventCommandResult.Set - adr:[{adr:x2}]");
                        }

                        switch (cmd)
                        {
                            case UnitMonitor.CmdReadStavCitacu:
                            case UnitMonitor.CmdReadCasPosledniOkNg:
                            case UnitMonitor.CmdReadRozdilKusu:
                            case UnitMonitor.CmdReadDefektivita:
                            case UnitMonitor.CmdReadHodnotuPrumCykluOkNg:
                            case UnitMonitor.CmdReadStavCitacuRanniSmena:
                            case UnitMonitor.CmdReadStavCitacuOdpoledniSmena:
                            case UnitMonitor.CmdReadStavCitacuNocniSmena:
                                //zaradim ke zpracovani ktere probiha v PacketBw
                                PackedCollection.Add(packet);
                                break;
                        }
                    }                    
                }
                //DataSendReceivedLogger.Debug("+");
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
                return false;
                //throw;
            }
        }

        public static bool ClosePort()
        {
            Logger.Debug("+");

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
            Logger.Debug("-");
            return !_serialPort.IsOpen;
        }

        public static void SendResetRf()
        {
            _resetRfInProgress = true;
            var uc = new UnitCommand(ResetRf, "ResetRF", -1, -1);
            UnitCommandsQueue.Enqueue(uc);
        }

        private static bool ResetRf()
        {
            Logger.Info("+");
            lock (_lock)
            {
                Logger.Info("lock");

                _dataReaderBw?.CancelAsync();
                _readAsyncCancellationTokenSource.Cancel();

                Thread.Sleep(ReadAsyncTimeout * 2);   //musim pockat na timeout nacitani z portu, aby worker zpracoval prikaz k ukonceni

                //_sendBw?.CancelAsync();
                //Thread.Sleep(5000);  //musime pockat na timeout potvrzeni odeslani

                _serialPort.DtrEnable = true;
                DataSendReceivedLogger.Info("DtrEnable=true");

                Thread.Sleep(1000);

                _serialPort.DtrEnable = true;
                DataSendReceivedLogger.Info("DtrEnable=true");

                Thread.Sleep(1000);

                _serialPort.DtrEnable = false;
                DataSendReceivedLogger.Info("DtrEnable=false");

                Thread.Sleep(500);

                SlevyrService.WaitEventPriorityCommandResult.Set();

                Task.Delay(2000).ContinueWith(t =>
                {
                    _dataReaderBw.RunWorkerAsync();
                    _resetRfInProgress = false;
                });                
            }

            //try
            //{
            //    ClosePort();
            //    Thread.Sleep(500);

            //    OpenPort();
            //}
            //catch (Exception ex)
            //{
            //    Logger.Error(ex);
            //    throw;
            //}

            Logger.Info("-");
            return true;
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

        public static IEnumerable<UnitTabule> GetAllUdrzba()
        {
            Logger.Debug("+");

            var res = _unitDictionary.Values.Where(u => u.UnitStatus.Tabule.MachineStatus == MachineStateEnum.Porucha ||
                //u.UnitStatus.Tabule.MachineStatus == MachineStateEnum.Stop || 
                u.UnitStatus.Tabule.MachineStatus == MachineStateEnum.Servis).Select(um => um.UnitStatus.Tabule).ToList();
            return res;
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

                SqlliteDao.AddUnitState(addr, _unitDictionary[addr].UnitStatus);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return _unitDictionary[addr].UnitStatus;
        }


        public static bool NastavStatus(byte addr, bool writeProtectEEprom, byte minOk, byte minNg, bool bootloaderOn, byte parovanyLed,
            byte rozliseniCidel, byte pracovniJasLed)
        {
            Logger.Debug($"addr:{addr} writeProtectEEprom:{writeProtectEEprom} minOK:{minOk} minNG:{minNg} parovanyLED:{parovanyLed}");

            if (_runConfig.IsMockupMode) return true;

            byte writeProtectEEpromVal = (byte)(writeProtectEEprom ? 0 : 1);
            byte bootloaderOnVal = (byte)(bootloaderOn ? 0 : 1);
            //return _unitDictionary[addr].SendSetZaklNastaveni(writeProtectEEpromVal, minOk, minNg, bootloaderOnVal, parovanyLed, rozliseniCidel, pracovniJasLed);
            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetZaklNastaveni(writeProtectEEpromVal, minOk, minNg, bootloaderOnVal, parovanyLed, rozliseniCidel, pracovniJasLed), "SendSetZaklNastaveni", addr, UnitMonitor.CmdZaklNastaveni);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }


        public static bool NastavCileSmen(byte addr, char varianta, short cil1, short cil2, short cil3)
        {
            Logger.Debug($"addr:{addr} var:{varianta} cil1:{cil1} cil2:{cil2} cil3:{cil3}");

            if (_runConfig.IsMockupMode) return true;

            //return _unitDictionary[addr].SendSetCileSmen(varianta, cil1, cil2, cil3);
            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetCileSmen(varianta, cil1, cil2, cil3), "SendSetCileSmen", addr, UnitMonitor.CmdSetCilSmen);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        public static bool NastavPrestavkyA(byte addr, TimeSpan prest1, TimeSpan prest2, TimeSpan prest3)
        {
            Logger.Debug($"addr:{addr} prest1:{prest1} prest2:{prest2} prest3:{prest3}");

            if (_runConfig.IsMockupMode) return true;

            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetPrestavkyA(prest1, prest2, prest3), "SendSetPrestavkyA", addr, UnitMonitor.CmdSetZacPrestav);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        public static bool NastavPrestavkyB(byte addr, TimeSpan p1s1, TimeSpan p1s2, TimeSpan p2po)
        {
            Logger.Debug($"addr:{addr} p1s1:{p1s1} p1s2:{p1s2} p2po:{p2po}");

            if (_runConfig.IsMockupMode) return true;

            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetPrestavkyB(p1s1, p1s2, p2po), "SendSetPrestavkyB", addr, UnitMonitor.CmdSetZacPrestav);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        public static bool NastavOkNg(byte addr, short ok, short ng)
        {
            Logger.Debug($"addr:{addr} ok:{ok} ng:{ng}");

            if (_runConfig.IsMockupMode) return true;

            //return _unitDictionary[addr].SendSetCitace(ok, ng);
            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetCitace(ok, ng), "SendSetCitace", addr, UnitMonitor.CmdSetHodnotyCitacu);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        public static void NastavAktualniCas(int addr)
        {
            Logger.Debug("");

            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetCas(DateTime.Now), "NastavAktualniCas", addr, UnitMonitor.CmdSetDatumDen);
            UnitCommandsQueue.Enqueue(uc);           
        }

        public static bool NastavDefektivitu(byte addr, char varianta, float def1, float def2, float def3)
        {
            Logger.Debug($"addr:{addr} varianta:{varianta} def1:{def1} def2:{def2} def3:{def3}");

            if (_runConfig.IsMockupMode) return true;
            
            Logger.Debug($"cil1:{def1}");
            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetDefektivita(varianta, def1, def2, def3), "SendSetDefektivita", addr, UnitMonitor.CmdSetDefSmen);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        public static bool NastavVariantuSmeny(byte addr, byte asciiByte)
        {
            Logger.Debug($"addr:{addr}");

            if (_runConfig.IsMockupMode) return true;

            var uc = new UnitCommand(() => _unitDictionary[addr].SendSetSmennost(asciiByte), "SendSetSmennost", addr, UnitMonitor.CmdSetSmennost);
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

            var uc = new UnitCommand(() => _unitDictionary[addr].SendReadStavCitacu(), "SendReadStavCitacu", addr, UnitMonitor.CmdReadStavCitacu);
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

            var uc = new UnitCommand(() => _unitDictionary[addr].SendReadCasOkNg(), "SendReadCasOkNg", addr, UnitMonitor.CmdReadCasPosledniOkNg);
            UnitCommandsQueue.Enqueue(uc);

            return true;
        }

        #endregion

        #region save/load/get unitConfig

        public static void SaveUnitConfig(UnitConfig unitCfg)
        {
            Logger.Debug($"addr:{unitCfg.Addr}");

            _unitDictionary[unitCfg.Addr].UnitConfig = unitCfg;
            unitCfg.SaveToFile(_runConfig.JsonDataFilePath);                        
        }

        public static UnitConfig GetUnitConfig(byte addr)
        {
            Logger.Debug($"addr:{addr}");

            //nacteno je jiz pri startu
            //_unitDictionary[addr].LoadUnitConfigFromFile(addr, _runConfig.JsonDataFilePath);
            return _unitDictionary[addr].UnitConfig;          
        }

        #endregion

        #region send a packet worker

        public static void StartSendReceiveWorkers()
        {
            if (!_isSendWorkerStarted)
            {
                if (!_runConfig.UseDataReceivedEvent)
                {
                    _dataReaderBw = new BackgroundWorker {WorkerReportsProgress = true,WorkerSupportsCancellation = true};
                    _dataReaderBw.DoWork += DatareadWorkerDoWork;
                    _dataReaderBw.RunWorkerAsync();
                    Logger.Info("*** datareader worker started ***");
                }

                _isSendWorkerStarted = true;
                _sendBw = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
                _sendBw.DoWork += SendWorkerDoWork;
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
                _packetBw.DoWork += PacketWorkerDoWork;
                _packetBw.RunWorkerAsync();
                Logger.Info("*** packet worker started ***");
            }
        }

        public static void StopSendReceiveWorkers()
        {
            _sendBw?.CancelAsync();
            _dataReaderBw?.CancelAsync();
            _readAsyncCancellationTokenSource.Cancel();
        }

        private static void StopPacketWorker()
        {
            _packetBw?.CancelAsync();
        }

        private static void SendWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            OpenPort();
            if (!CheckIsPortOpen()) return;

            Stopwatch stopwatch = null;
            int cycleForResetRf=0;

            while (true)
            {
                Logger.Info($"worker cycle {++_sendWorkerCycleCnt}");

                if (_resetRfInProgress)
                {
                    Thread.Sleep(1000);
                    continue;
                }
               
                if (_runConfig.IsAutoResetRF)
                {
                    var rfOk = _unitDictionary.FirstOrDefault().Value.TestCommand();   //provedu test odesláním testovacího paketu
                    if (!rfOk)
                    {
                        Logger.Info($"*** Reset RF ***");
                        SendResetRf();
                    }
                }

                if (_runConfig.IsScheduledResetRF && cycleForResetRf++ >= _runConfig.CycleForScheduledResetRf)
                {
                    Logger.Info($"*** scheduled Reset RF (cycle={cycleForResetRf})***");
                    ResetLogger.Info($"Reset RF (cycle={cycleForResetRf})");
                    cycleForResetRf = 0;
                    SendResetRf();
                }

                if (_sendBw.CancellationPending)
                {
                    Logger.Info("*** send worker canceled ***");
                    e.Cancel = true;
                    _isSendWorkerStarted = false;
                    return;
                }

                try
                {
                    UnitCommand unitCommand;

                    //pokud jsou prikazy cekajici na zpracovani tak se provedou prednostne
                    //TODO zajistit aby se prednostni prikaz zpracovaval vzdy jen jeden v jednom okamziku (per jednotku ?) 
                    while (UnitCommandsQueue.TryDequeue(out unitCommand))
                    {
                        Logger.Debug($"Priority command {unitCommand.Cmd} {unitCommand.Description} dequeue");

                        WaitEventPriorityCommandResult.Reset(); //protoze result mohl prijit necekane po timout-u a mohl byt tudiz ve stavu signaled
                        unitCommand.Run();

                        var resultReceived = WaitEventPriorityCommandResult.WaitOne(_runConfig.PriorityCommandTimeOut);
                        var r = (resultReceived) ? "received" : "expired";
                        TplLogger.Debug($" Priority Command {unitCommand.Cmd:x2} : result {r}"); //({duration.Milliseconds} ms)");

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
                        if (!CheckIsPortOpen()) continue;

                        if (_sendBw.CancellationPending)
                        {
                            Logger.Info("*** send worker canceled ***");
                            e.Cancel = true;
                            _isSendWorkerStarted = false;
                            return;
                        }

                        bool res = false;

                        if (_runConfig.IsWaitCommandResult)  //synchronizace nejen potvrzeni odeslani prikazu ale i prijeti vysledku
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
                                    _unitDictionary[addr].UnitStatus.Tabule.MachineStatus = MachineStateEnum.NotAvailable;
                                }
                            }
                            else
                            {
                                res = _unitDictionary[addr].ObtainStatusAsync();
                            }
                        }

                        if (!res)
                        {
                            _unitDictionary[addr].UnitStatus.Tabule.MachineStatus = MachineStateEnum.NotAvailable;
                        }

                        //Logger.Debug($"+send worker sleep: {_runConfig.WorkerSleepPeriod}");
                        //Thread.Sleep(_runConfig.WorkerSleepPeriod);  //pauza pred odeslanim prikazu na dalsi jednotku
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
        private static void PacketWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            SqlliteDao.OpenConnection(true, _runConfig.DbFilePath);

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
                            //udalost prichazi kdyz dochazi ke zmene stavu a cmd urcuje ktera smena prace skoncila. 
                            //zacatek nasledujici smeny je zaroven koncem predchozi                           
                            _unitDictionary[addr].DoReadStavCitacuKonecSmeny(packet);
                            SqlliteDao.AddUnitKonecSmenyState(addr, cmd, _unitDictionary[addr].UnitStatus,
                                _unitDictionary[addr].UnitConfig.Zacatek1SmenyTime, _unitDictionary[addr].UnitConfig.Cil3Smeny);
                            break;
                        case UnitMonitor.CmdReadStavCitacuOdpoledniSmena:
                            _unitDictionary[addr].DoReadStavCitacuKonecSmeny(packet);
                            SqlliteDao.AddUnitKonecSmenyState(addr, cmd, _unitDictionary[addr].UnitStatus,
                                _unitDictionary[addr].UnitConfig.Zacatek3SmenyTime, _unitDictionary[addr].UnitConfig.Cil2Smeny);
                            break;
                        case UnitMonitor.CmdReadStavCitacuRanniSmena:
                            _unitDictionary[addr].DoReadStavCitacuKonecSmeny(packet);
                            SqlliteDao.AddUnitKonecSmenyState(addr, cmd, _unitDictionary[addr].UnitStatus,
                                _unitDictionary[addr].UnitConfig.Zacatek2SmenyTime, _unitDictionary[addr].UnitConfig.Cil1Smeny);
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
                  _serialPort.DiscardOutBuffer(); 
            }
        }

        public static void DiscardInBuffer()
        {
            lock (_lock)
            {
                if (_serialPort.IsOpen)
                    _serialPort.DiscardInBuffer();
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
                _serialPort.Write(inBuff, buffLength);
                //await _serialPort.WriteAsync(inBuff, buffLength);
            }
            DataSendReceivedLogger.Debug($"->  {buffLength}; {BitConverter.ToString(inBuff)}");
        }

      
    }
}
