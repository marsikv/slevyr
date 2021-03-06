﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.DAO;
using Slevyr.DataAccess.Model;

namespace Slevyr.DataAccess.Services
{
    public static class SlevyrService
    {
        #region Fields
        static SerialPortWraper _serialPort;

        static Dictionary<int, UnitMonitor> _unitDictionary;

        static RunConfig _runConfig = new RunConfig();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger UnitsLogger = LogManager.GetLogger("Units");
        private static readonly Logger Units2Logger = LogManager.GetLogger("Units2");
        private static readonly Logger ErrorsLogger = LogManager.GetLogger("Errors");

        private static BackgroundWorker _bw;
        private static bool _isWorkerStarted;
        private static int _workerCycleCnt;

        public static int SelectedUnit { get; set; }

        private static ConcurrentQueue<UnitCommand> _unitCommandsQueue = new ConcurrentQueue<UnitCommand>();
        private static bool _ErrorRecorded;
        private static int _ErrorRecordedCnt;

        #endregion

        #region Properties

        public static RunConfig Config
        {
            get { return _runConfig; }
            set { _runConfig = value; }
        }

        public static bool SerialPortIsOpen => _serialPort.IsOpen;

        #endregion

        #region init method

        public static void Init(SerialPortConfig portCfg, RunConfig runConfig)
        {
            Logger.Info("+");

            _serialPort = new SerialPortWraper(portCfg);

            _runConfig = runConfig;

            _unitDictionary = new Dictionary<int, UnitMonitor>();
            foreach (var a in runConfig.UnitAddrs)
            {
                _unitDictionary.Add(a, new UnitMonitor((byte)a, _serialPort, _runConfig));
            }

            foreach (var m in _unitDictionary.Values)
            {
                 m.LoadUnitConfigFromFile(m.Address, _runConfig.DataFilePath);
            }

            Logger.Info("unit count: " + _unitDictionary.Count);

            Logger.Info($"Open port {_serialPort.PortName} baud rate: {_serialPort.BaudRate}");

            OpenPort();

            Logger.Info($"Port open: {_serialPort.IsOpen}");
        }

        #endregion

        public static IEnumerable<int> UnitAddresses => _runConfig.UnitAddrs;

        public static int UnitCount => _unitDictionary.Count;

        #region open close port

        public static bool OpenPort()
        {
            Logger.Info("+");
            try
            {
                if (_runConfig.IsMockupMode) return true;
                if (!_serialPort.IsOpen) _serialPort.Open();
                return _serialPort.IsOpen;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
                //return false;
            }
        }

        public static bool ClosePort()
        {
            Logger.Info("+");

            if (_runConfig.IsMockupMode) return true;
            if (_serialPort.IsOpen)
            {
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                _serialPort.Close();
            }
            return !_serialPort.IsOpen;
        }

        #endregion

        #region UnitStatus operations

        public static UnitStatus Status(byte addr)
        {
            Logger.Info($"+ {addr}");

            return _unitDictionary[addr].UnitStatus;
        }

        /// <summary>
        /// Získá status jednoty načtením stavu čítačů a výpočtem parametrů zobrazovaných na tabuli
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static UnitStatus ObtainStatus(byte addr)
        {
            Logger.Info($"+ *** unit {addr}");

            if (_runConfig.IsMockupMode) return _unitDictionary[addr].UnitStatus;

            int ok, ng;
            Single casOk=-1, casNg=-1;
            var res = _unitDictionary[addr].ReadStavCitacu(out ok, out ng);
            var s = res ? "" : "err";
            Logger.Info($">ok:{ok} ng:{ng} " + s);

            if (!res)
            {
                _ErrorRecordedCnt++;
                if (!_ErrorRecorded)
                {
                    ErrorsLogger.Error($"ReadStavCitacu;{addr};total errors:{_ErrorRecordedCnt}");
                    _ErrorRecorded = true;
                }
            }
            else
            {
                if (_ErrorRecorded)
                {
                    ErrorsLogger.Info($"ReadStavCitacu;{addr};recovered");
                    _ErrorRecorded = false;
                }
            }

            if (res && _runConfig.IsReadOkNgTime)
            {
                _unitDictionary[addr].ReadCasOK(out casOk);
                Logger.Info($">casOk:{casOk}");

                _unitDictionary[addr].ReadCasNG(out casNg);
                Logger.Info($">casNg:{casNg}");
            }

            if (res)
            {
                //prepocitat pro zobrazeni tabule
                try
                {
                    _unitDictionary[addr].RecalcTabule();

                    _unitDictionary[addr].UnitStatus.LastCheckTime = DateTime.Now;

                    string casOkStr = (_runConfig.IsReadOkNgTime)
                        ? casOk.ToString(CultureInfo.InvariantCulture)
                        : string.Empty;
                    string casNgStr = (_runConfig.IsReadOkNgTime)
                        ? casNg.ToString(CultureInfo.InvariantCulture)
                        : string.Empty;

                    UnitsLogger.Info($"4;{addr};{ok};{ng};{casOkStr};{casNgStr};{(int)_unitDictionary[addr].UnitStatus.MachineStatus}");

                    StringBuilder sb = new StringBuilder();
                    sb.Append($"4;{_unitDictionary[addr].UnitConfig.UnitName};{addr}"); //az po 4
                    sb.Append($";{_unitDictionary[addr].UnitStatus.CilKusuTabule}"); //5
                    sb.Append($";{ok}"); //6
                    sb.Append(_runConfig.IsReadOkNgTime ? $";{_unitDictionary[addr].UnitStatus.CasOk}" : ";"); //7
                    sb.Append(ok != 0 ? $";{_unitDictionary[addr].UnitStatus.UbehlyCasSmenySec / (float)ok:F}" : ";");
                    sb.Append($";{_unitDictionary[addr].UnitStatus.CilDefectTabule:F}"); //9
                    sb.Append($";{ng}"); //10
                    sb.Append(_runConfig.IsReadOkNgTime ? $";{_unitDictionary[addr].UnitStatus.CasNg}" : ";"); //11
                    sb.Append(ng != 0 ? $";{_unitDictionary[addr].UnitStatus.UbehlyCasSmenySec / (float)ng:F}" : ";"); //12
                    sb.Append($";{_unitDictionary[addr].UnitStatus.RozdilTabuleTxt}"); //13
                    sb.Append($";{_unitDictionary[addr].UnitStatus.AktualDefectTabuleTxt}"); //14
                    sb.Append($";{_unitDictionary[addr].UnitStatus.MachineStatus}"); //15
                    sb.Append($";{Convert.ToInt32(_unitDictionary[addr].UnitStatus.IsPrestavkaTabule)}"); //16
                    Units2Logger.Info(sb.ToString);

                    SqlliteDao.AddUnitState(addr, _unitDictionary[addr].UnitStatus);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            else
            {
                if (_runConfig.IsWriteEmptyToLog) UnitsLogger.Info($" 4;{addr};;;;;");
            }

            Logger.Info($"-");

            return _unitDictionary[addr].UnitStatus;
        }


        public static bool NastavStatus(byte addr, bool writeProtectEEprom, byte minOK, byte minNG, bool bootloaderOn, byte parovanyLED,
            byte rozliseniCidel, byte pracovniJasLed)
        {
            Logger.Info($"addr:{addr} writeProtectEEprom:{writeProtectEEprom} minOK:{minOK} minNG:{minNG} parovanyLED:{parovanyLED}");

            if (_runConfig.IsMockupMode) return true;

            byte writeProtectEEpromVal = (byte)(writeProtectEEprom ? 0 : 1);
            byte bootloaderOnVal = (byte)(bootloaderOn ? 0 : 1);
            return _unitDictionary[addr].SetStatus(writeProtectEEpromVal, minOK, minNG, bootloaderOnVal, parovanyLED, rozliseniCidel, pracovniJasLed);
        }


        public static bool NastavCileSmen(byte addr, char varianta, short cil1, short cil2, short cil3)
        {
            Logger.Info($"addr:{addr} var:{varianta} cil1:{cil1} cil2:{cil2} cil3:{cil3}");

            if (_runConfig.IsMockupMode) return true;

            return _unitDictionary[addr].SetCileSmen(varianta, cil1, cil2, cil3);
        }

        public static bool NastavPrestavky(byte addr, char varianta, TimeSpan prest1, TimeSpan prest2, TimeSpan prest3)
        {
            Logger.Info($"addr:{addr} var:{varianta} prest1:{prest1} prest2:{prest2} prest3:{prest3}");

            if (_runConfig.IsMockupMode) return true;

            return _unitDictionary[addr].SetPrestavky(varianta, prest1, prest2, prest3);
        }

        public static bool NastavOkNg(byte addr, short ok, short ng)
        {
            Logger.Info($"addr:{addr} ok:{ok} ng:{ng}");

            if (_runConfig.IsMockupMode) return true;

            return _unitDictionary[addr].SetCitace(ok, ng);
        }


        public static bool NastavAktualniCas(int addr)
        {
            Logger.Info($"addr:{ addr}");

            if (_runConfig.IsMockupMode) return true;

            Func<bool> a = () => _unitDictionary[addr].SetCas(DateTime.Now);

            return _unitDictionary[addr].SetCas(DateTime.Now);
        }

        public static void NastavAktualniCasQueued(int addr)
        {
            Logger.Info("");

            var uc = new UnitCommand(() => _unitDictionary[addr].SetCas(DateTime.Now), "NastavAktualniCas", addr);
            _unitCommandsQueue.Enqueue(uc);           
        }


        public static bool NastavDefektivitu(byte addr, char varianta, short def1Val, short def2Val, short def3Val)
        {
            Logger.Info($"addr:{addr} varianta:{varianta} def1:{def1Val} def2:{def2Val} def3:{def3Val}");

            if (_runConfig.IsMockupMode) return true;
            
            Logger.Info($"cil1:{def1Val}");
            return _unitDictionary[addr].SetDefektivita(varianta, def1Val, def2Val, def3Val );
        }


        public static bool NastavHandshake(byte addr, bool value)
        {
            Logger.Info($"addr:{addr} val:{value}");

            if (_runConfig.IsMockupMode) return true;

            var um = _unitDictionary[addr];

            var res = um.SetHandshake(value ? (byte)1 : (byte)255, 0);

            return res;
        }


        public static bool CtiStavCitacu(byte addr)
        {
            Logger.Info("+");

            if (_runConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().Ok++;
                Mock.MockUnitStatus().Ng++;
                return true;
            }

            return _unitDictionary[addr].ReadStavCitacu();

        }


        public static bool CtiCyklusOkNg(byte addr)
        {
            Logger.Info("+");

            if (_runConfig.IsMockupMode)
            {
                Mock.MockUnitStatus().CasOkTime = DateTime.Now;
                Mock.MockUnitStatus().CasNgTime = DateTime.Now;
                return true;
            }

            float ok;
            float ng;
            return _unitDictionary[addr].ReadCasOK(out ok) && _unitDictionary[addr].ReadCasNG(out ng);

        }

        #endregion

        #region save/load

        public static void SaveUnitConfig(UnitConfig unitCfg)
        {
            Logger.Info($"addr:{unitCfg.Addr}");

            _unitDictionary[unitCfg.Addr].UnitConfig = unitCfg;
            unitCfg.SaveToFile(_runConfig.DataFilePath);                        
        }

        #endregion

        public static UnitConfig GetUnitConfig(byte addr)
        {
            Logger.Info($"addr:{addr}");

            //nacteno je jiz pri startu
            //_unitDictionary[addr].LoadUnitConfigFromFile(addr, _runConfig.DataFilePath);
            return _unitDictionary[addr].UnitConfig;          
        }

        public static void StartWorker()
        {
            if (_isWorkerStarted) return;

            _isWorkerStarted = true;
            _bw = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            _bw.DoWork += _bw_DoWork;
            _bw.RunWorkerAsync();

            Logger.Info("***  worker started ***");
        }

        public static void StopWorker()
        {
            _bw.CancelAsync();
        }

        private static void _bw_DoWork(object sender, DoWorkEventArgs e)
        {

            OpenPort();

            SqlliteDao.OpenConnection(true);

            while (true)
            {
                Logger.Info($"worker cycle {++_workerCycleCnt}");

                try
                {
                    UnitCommand unitCommand;

                    //pokud jsou prikazy cekajici na zpracovani tak se provedou prednostne
                    while (_unitCommandsQueue.TryDequeue(out unitCommand))
                    {
                        Logger.Debug($"command {unitCommand.Description} dequeue");
                        unitCommand.Run();
                        Thread.Sleep(_runConfig.RelaxTime);
                        Logger.Debug($"command invoke res:{unitCommand.Result}");
                    }

                    //provedu nacitani ze vsech jednotek v cyklu
                    foreach (var addr in UnitAddresses)
                    {
                        if (_bw.CancellationPending)
                        {
                            Logger.Info("*** read worker canceled ***");
                            _isWorkerStarted = false;
                            SqlliteDao.CloseConnection();
                            return;
                        }

                        ObtainStatus((byte)addr);

                        Logger.Debug($"+worker sleep: {_runConfig.WorkerSleepPeriod}");
                        Thread.Sleep(_runConfig.WorkerSleepPeriod);  //pauza pred ctenim dalsi jednotky - parametr
                        Logger.Debug("-worker sleep");
                    }

                    //ClosePort();  //- muze byt problem ze prijimani dat neste neskoncilo

                    //pokud o vynuceni uvolneni zdroju
                    //Thread.Sleep(100);

                    //GC.Collect();
                    //GC.WaitForPendingFinalizers();

                    //Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
           

        }


    }
}
