using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using SledovaniVyroby.SerialPortWraper;
using Slevyr.DataAccess.Model;

namespace Slevyr.DataAccess.Services
{
    public static class SlevyrService
    {
        #region Fields
        static SerialPortWraper _serialPort;

        static IEnumerable<int> _unitAddrs;

        static Dictionary<int, UnitMonitor> _unitDictionary;

        static RunConfig _runConfig = new RunConfig();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static int SelectedUnit { get; set; }

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

        public static void Init(SerialPortConfig portCfg, RunConfig runConfig, IEnumerable<int> unitAddrs)
        {
            Logger.Info("+");

            _serialPort = new SerialPortWraper(portCfg);

            _runConfig = runConfig;

            _unitAddrs = unitAddrs;

            _unitDictionary = new Dictionary<int, UnitMonitor>();
            foreach (var a in unitAddrs)
            {
                _unitDictionary.Add(a, new UnitMonitor((byte)a, _serialPort, _runConfig.IsMockupMode));
            }

            Logger.Info("unit count: " + _unitDictionary.Count);

            Logger.Info($"Open port {_serialPort.PortName} baud rate: {_serialPort.BaudRate}");

            OpenPort();

            Logger.Info($"Port open: {_serialPort.IsOpen}");
        }

        #endregion

        public static IEnumerable<int> UnitAddresses => _unitAddrs;

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
            if (_serialPort.IsOpen) _serialPort.Close();
            return !_serialPort.IsOpen;
        }

        #endregion

        #region UnitStatus operations

        //public static UnitMonitor GetUnit(int Addr)
        //{
        //    return _unitDictionary[Addr];
        //}

        public static UnitStatus Status(byte addr)
        {
            Logger.Info("+");
            if (_runConfig.IsMockupMode) return Mock.MockUnitStatus();

            _unitDictionary[addr].UnitStatus.RecalcTabule();

            return _unitDictionary[addr].UnitStatus;
        }


        public static UnitStatus RefreshStatus(byte addr)
        {
            Logger.Info($"+ {addr}");
            if (_runConfig.IsMockupMode) return Mock.MockUnitStatus();

            short ok, ng;
            Single casOk, casNg;
            Logger.Info("   ReadStavCitacu");
            _unitDictionary[addr].ReadStavCitacu(out ok, out ng);
            Logger.Info($"   ok:{ok} ng:{ng}");
            Logger.Info("   ReadCasOK");
            _unitDictionary[addr].ReadCasOK(out casOk);
            Logger.Info($"   casOk:{casOk}");
            Logger.Info("   ReadCasNG");
            _unitDictionary[addr].ReadCasNG(out casNg);
            Logger.Info($"   casNg:{casNg}");

            //prepocitat pro zobrazeni tabule
            _unitDictionary[addr].UnitStatus.RecalcTabule();

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

        public static bool NastavPrestavky(byte addr, char varianta, short prest1, short prest2, short prest3)
        {
            Logger.Info($"addr:{addr} var:{varianta} prest1:{prest1} prest2:{prest2} prest3:{prest3}");

            if (_runConfig.IsMockupMode) return true;

            return _unitDictionary[addr].SetPrestavky(varianta, (short)prest1, (short)prest2, (short)prest3);
        }

        public static bool NastavOkNg(byte addr, short ok, short ng)
        {
            Logger.Info($"addr:{addr} ok:{ok} ng:{ng}");

            if (_runConfig.IsMockupMode) return true;

            return _unitDictionary[addr].SetCitace(ok, ng);
        }


        public static bool NastavAktualniCas(byte addr)
        {
            Logger.Info("+");

            if (_runConfig.IsMockupMode) return true;

            return _unitDictionary[addr].SetCas(new DateTime());
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

            return _unitDictionary[addr].RefreshStavCitacu();

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

        public static void SaveUnitConfig(byte addr,
             char varianta,  short cil1,  short cil2,  short cil3,
             string def1, string def2, string def3,
             int prest1,  int prest2,  int prest3,
             string writeProtectEEprom,  byte minOK,  byte minNG,  string bootloaderOn,  byte parovanyLED,
             byte rozliseniCidel,  byte pracovniJasLed)
        {
            Logger.Info($"addr:{addr}");

            UnitConfig unitConfig = new UnitConfig()
            {
                WriteProtectEEprom = writeProtectEEprom.Equals("True", StringComparison.InvariantCultureIgnoreCase) || writeProtectEEprom.Equals("ano", StringComparison.InvariantCultureIgnoreCase),
                MinOK = minOK,
                MinNG = minNG,
                BootloaderOn = bootloaderOn.Equals("True", StringComparison.InvariantCultureIgnoreCase) || bootloaderOn.Equals("ano", StringComparison.InvariantCultureIgnoreCase),
                ParovanyLED = parovanyLED,
                RozliseniCidel = rozliseniCidel,
                PracovniJasLed = pracovniJasLed,
                Addr = addr,
            };

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            Directory.CreateDirectory(_runConfig.DataFilePath);

            using (StreamWriter sw = new StreamWriter(Path.Combine(_runConfig.DataFilePath, $"unitCfg_{addr}.json")))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, unitConfig);
            }
            
        }

        #endregion

    }
}
