using System;
using System.Threading;
using NLog;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitMonitor : UnitMonitorBasic
    {
        #region fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger ErrorsLogger = LogManager.GetLogger("Errors");
        private static readonly Logger TplLogger = LogManager.GetLogger("Tpl");
        private static readonly Logger MaintenanceLogger = LogManager.GetLogger("Maintenance");

        public UnitStatus UnitStatus { get; set; }

        public UnitConfig UnitConfig { get; set; }

        private volatile object _lock = new object();

        private static bool _errorRecorded;
        private static int _errorRecordedCnt;

        #endregion

        #region const pro prikazy

        public const byte CmdZaklNastaveni = 3	; //Zakladni nastaveni
        public const byte CmdSetHodnotyCitacu = 4	; //Zapise hodnoty do citacu
        //public const byte CmdSet = 5	; //Vyvola programovaci mod adresy
        public const byte CmdSetParamRf = 6	; //Nastavi parametry RF
        public const byte CmdResetJednotky = 7	; //Reset jednotky
        public const byte CmdZapsatEeprom= 15; //Zapise do eeprom od adresy 5 bytu         
        public const byte CmdSetSmennost = 16; //nastavi variantu smeny
        public const byte CmdSetDatumDen = 17; //nastavi cas datum a den
        public const byte CmdSetCilSmen = 18;  //nastavi cile smen
        public const byte CmdSetDefSmen = 19;  //nastavi cile defektivity smen
        public const byte CmdSetJasLed = 20;   //nastavi jas LED panelu
        public const byte CmdSetZacPrestav = 21;   //nastavi zacatky prestavek

        public const byte CmdReadVerSwRFT = 1; //vrati verzi sw RFT modulu + panel
        public const byte CmdReadZakSysNast = 9; //vrati zakladni systemove nastaveni
        //const byte Get = 10	; //vrati sadu A systemovych nastaveni
        //const byte Get = 11	; //vrati sadu B systemovych nastaveni
        //const byte Get = 12	; //vrati sadu C systemovych nastaveni
        //const byte Get = 13	; //vrati sadu D systemovych nastaveni
        //const byte Get = 14	; //Precte 5 bytu z eeprom od adresy
        //const byte Get = 26	; //Vrati hodnoty citace 1 2 3
        //const byte Get = 27	; //Vrati hodnoty citace 4 5 6
        //const byte Get = 28	; //Vrati hodnoty citace 7 8
        public const byte CmdReadStavCitacu = 96; //Vrati stav citacu
        public const byte CmdReadCasPosledniOkNg = 97; //vrati hodnotu posledniho cyklu OK
        public const byte CmdReadHodnotuPrumCykluOkNg = 98; //vrati hodnotu prumerneho cyklu OK
        //public const byte CmdReadHodnotuPoslCykluNg = 98; //vrati hodnotu posledniho cyklu NG
        //public const byte CmdReadHodnotuPrumCykluNg = 100; //vrati hodnotu prumerneho cyklu NG
        public const byte CmdReadTeplotu1Cidla = 101; //vrati teplotu z prvniho cidla DS18B20
        public const byte CmdReadTeplotu2Cidla = 102; //vrati teplotu z druheho cidla DS18B20
        public const byte CmdReadTeplotu3Cidla = 103; //vrati teplotu z tretiho cidla DS18B20
        public const byte CmdReadTeplotu4Cidla = 104; //vrati teplotu ze ctvrteho cidla DS18B20
        public const byte CmdReadTeplotu5Cidla = 105; //vrati teplotu z pateho cidla DS18B20
        public const byte CmdReadRozdilKusu = 106; //vraci rozdil kusu
        public const byte CmdReadDefektivita = 107; //vraci defektivitu
        public const byte CmdReadStavCitacuRanniSmena = 0x6c;     //vraci konecny stav citacu, pracuje stejne jako 0x60 (96)
        public const byte CmdReadStavCitacuOdpoledniSmena = 0x6d; //
        public const byte CmdReadStavCitacuNocniSmena = 0x6e;     //

        public const byte CmdTestPacket = 0;     //specialni prikaz pro odeslani testovaciho packetu

        readonly byte[] _obtainStatusSequence;
        int _obtainStatusIndex = 0;

        #endregion

        #region ctor
      
        public UnitMonitor(byte address,  RunConfig runConfig) : base(address,runConfig)
        {
            
            UnitStatus = new UnitStatus { Addr = address};

            if (this.RunConfig.IsReadOkNgTime)
            {
                _obtainStatusSequence = new byte[] {
                     CmdReadStavCitacu,
                     CmdReadCasPosledniOkNg,
                };
            }
            else
            {
                _obtainStatusSequence = new byte[] {
                    CmdReadStavCitacu
                };
            }
        }

        #endregion

        #region properties

        /// <summary>
        /// cas kdy byl prikaz odeslan (send)
        /// </summary>
        public DateTime CurrentCmdStartTime { get; private set; }

        /// <summary>
        /// Probiha zracovani prikazu, tzn. ceka se na receive data parovane na tento cmd
        /// </summary>
        public bool IsCommandPending { get; private set; }

        /// <summary>
        /// Kod odeslaneho prikazu
        /// </summary>
        public byte CurrentCmd { get; private set; }

        public CancellationTokenSource UpdateStatusTokenSource { get; private set; }
        public long LastId { get; set; }

        #endregion

        #region public methods - send command

        public bool SendSetSmennost(byte varianta)
        {
            Logger.Info($"+ unit {Address}");

            UnitConfig.TypSmennosti = char.ToString((char) varianta);

            var res = SendCommand(CmdSetSmennost, varianta);

            return res;
        }
       
        public bool SendSetCileSmen(char varianta, short cil1, short cil2, short cil3)
        {
            Logger.Info($"+ unit {Address}");

            UnitConfig.Cil1Smeny = cil1;
            UnitConfig.Cil2Smeny = cil2;
            UnitConfig.Cil3Smeny = cil3;

            return SendCommand(CmdSetCilSmen, (byte)varianta, cil1, cil2, cil3); 
        }

        public bool SendSetDefektivita(char varianta, float def1, float def2, float def3)
        {
            Logger.Info($"+ unit {Address}");
           
            UnitConfig.Def1Smeny = def1 ;  
            UnitConfig.Def2Smeny = def2 ;
            UnitConfig.Def3Smeny = def3 ;

            return SendCommand(CmdSetDefSmen, (byte)varianta, (short)(def1 * 100.0), (short)(def2 * 100.0), (short)(def3 * 100.0));
        }

        //TODO - v jakych jednotkach se zadavaji prestavky ?
        public bool SendSetPrestavkyA(TimeSpan prest1, TimeSpan prest2, TimeSpan prest3)
        {
            Logger.Info($"+ unit {Address}");

            UnitConfig.TypSmennosti = "A"; 
            UnitConfig.Prestavka1Smeny = prest1.ToString();  //TODO ukladat TimeSpan, ne string
            UnitConfig.Prestavka2Smeny = prest2.ToString();
            UnitConfig.Prestavka3Smeny = prest3.ToString();

            UnitConfig.Prestavka1Smeny1 = null;
            UnitConfig.Prestavka1Smeny2 = null;
            UnitConfig.Prestavka2Po = null;
            UnitConfig.Prestavka2Smeny1Time = TimeSpan.Zero;
            UnitConfig.Prestavka2Smeny2Time = TimeSpan.Zero;

            return SendCommand(CmdSetZacPrestav, (byte)'A', prest1, prest2, prest3);
        }

        public bool SendSetPrestavkyB(TimeSpan p1s1, TimeSpan p1s2, TimeSpan p2po)
        {
            Logger.Info($"+ unit {Address}");

            UnitConfig.TypSmennosti = "B";
            UnitConfig.Prestavka1Smeny = null;  
            UnitConfig.Prestavka2Smeny = null;
            UnitConfig.Prestavka3Smeny = null;
            UnitConfig.Prestavka1Smeny1 = p1s1.ToString();  //TODO ukladat TimeSpan, ne string;
            UnitConfig.Prestavka1Smeny2 = p1s2.ToString();
            UnitConfig.Prestavka2Po = p2po.ToString();
            UnitConfig.Prestavka2Smeny1Time = p1s1 + p2po;
            UnitConfig.Prestavka2Smeny2Time = p1s2 + p2po; 

            return SendCommand(CmdSetZacPrestav, (byte)'B', p1s1, p1s2, p2po);
        }

        public bool SendSetCas(DateTime dt)
        {
            Logger.Info($"+ addr:{Address}");

            return SendCommand(CmdSetDatumDen, dt); 
        }

        public bool SendSetJasLcd(byte jas)
        {
            Logger.Info($"+ addr:{Address}");
 
            return SendCommand(CmdSetJasLed,jas);
        }

        public bool SendSetCitace(short ok, short ng)
        {
            Logger.Info($"+ addr:{Address}");
            return SendCommand(CmdSetHodnotyCitacu, ok, ng);
        }

        public bool SendReset()
        {
            Logger.Info($"+ addr:{Address}");
            return SendCommand(CmdResetJednotky);
        }


        public bool SendSetHandshake(byte handshake, byte prumTyp)
        {
            Logger.Info($"+ addr:{Address}");

            return SendCommand(CmdSetParamRf, handshake, prumTyp);
        }

        public bool SendSetZaklNastaveni(byte writeProtectEEprom, byte minOk, byte minNg, byte bootloaderOn, byte parovanyLed, byte rozliseniCidel, byte pracovniJasLed)
        {
            Logger.Info($"+ addr:{Address}");

            return SendCommand(CmdZaklNastaveni, writeProtectEEprom,  minOk,  minNg,  bootloaderOn,  parovanyLed,  rozliseniCidel,  pracovniJasLed);
        }

        public bool SendReadZaklNastaveni()
        {
            Logger.Info($"+ unit {Address}");
            return SendCommand(CmdReadZakSysNast);
        }

        public bool SendReadStavCitacu()
        {
            Logger.Info($"+ unit {Address}");

            //if (_isMockupMode)
            //{
            //    okVal = (short)DateTime.Now.Minute;  //minuta poslouzi jako hodnota ok
            //    ngVal = (short)((okVal + 1) / 2);
            //    UnitStatus.Ok = okVal;
            //    UnitStatus.Ng = ngVal;
            //    UnitStatus.OkNgTime = DateTime.Now;
            //    return true;
            //}

            return SendCommand(CmdReadStavCitacu); 
        }

        public bool SendReadCasOkNg()
        {
            Logger.Info($"+ unit {Address}");

            //if (_isMockupMode)
            //{
            //    value = (Single)DateTime.Now.Hour * 2;  //hod. poslouzi jako hodnota casu ok
            //    UnitStatus.CasOk = value;
            //    UnitStatus.CasOkNgTime = DateTime.Now;
            //    return true;
            //}

            return SendCommand(CmdReadCasPosledniOkNg);
        }

        public bool SendReadRozdilKusu()
        {
            Logger.Info($"+ unit {Address}");

            return SendCommand(CmdReadRozdilKusu);
        }

        public bool SendReadDefektivita()
        {
            Logger.Info($"+ unit {Address}");

            return SendCommand(CmdReadDefektivita);
        }

        public bool SendReadStavCitacuKonecSmeny(int cmd)
        {
            Logger.Info($"+ unit {Address} cmd: {cmd}");

            return SendCommand((byte)cmd);  //to je specificke protoze prikaz se lisi podle smeny
        }


        #endregion

        #region public methods - read response handlers

        public void DoReadStavCitacu(byte[] buff)
        {
            Logger.Debug("");
            /*
             * Pro příkaz 0x60 je odpověď = 0x00 0x00 ADR 0x60 LSB MSB LSB MSB LSB MSB 0xXX

                Počet vyrobených OK kusů = LSB+(256*MSB) , Kusy
                Počet vyrobených NG kusů = LSB+(256*MSB) , Kusy
                Stop time = LSB+(256*MSB) , Sekundy
                Stav stroje = XX
             */
            var okVal = Helper.ToShort(buff[4], buff[5]);
            var ngVal = Helper.ToShort(buff[6], buff[7]);
            var stopDuration = Helper.ToShort(buff[8], buff[9]);
            short machineStatusInt = buff[10];

            MachineStateEnum machineStatus = (MachineStateEnum)machineStatusInt;

            LogMachineStatusForMaintenance(machineStatus);

            UnitStatus.SetStopTime(machineStatus);

            UnitStatus.Ok = okVal;
            UnitStatus.Ng = ngVal;
            UnitStatus.Tabule.MachineStatus = machineStatus;
            UnitStatus.Tabule.MachineStopDuration = stopDuration;

            UnitStatus.OkNgTime = DateTime.Now;
            UnitStatus.IsOkNg = true;

            Logger.Info($"okVal:{okVal} ngVal:{ngVal} machineStatus:{machineStatusInt} unit: {Address}");
        }

        public void DoReadCasOkNg(byte[] buff)
        {
            /*
             * 
             * Pro příkaz 0x61 je odpověď = 0x00 0x00 ADR 0x61 LSB MSB LSB MSB 0x00 0x00 0xXX
                čas posledního OK kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
                čas posledního NG kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
                Metoda měření cyklu = XX  , 0 = čas posledního cyklu (výchozí); >0 čas od posledního cyklu
             */
            Logger.Debug("");

            var casOk10 = Helper.ToShort(buff[4], buff[5]); 
            var casNg10 = Helper.ToShort(buff[6], buff[7]); 

            UnitStatus.CasOk = (float) ( casOk10 / 10.0);
            UnitStatus.CasNg = (float)(casNg10 / 10.0);

            UnitStatus.CasOkNgTime = DateTime.Now;
            UnitStatus.IsCasOkNg = true;
            Logger.Debug($"unit {Address}");
        }

        public void DoReadCasPoslednihoCyklu(byte[] buff)
        {
            /*
            Pro příkaz 0x62 je odpověď = 0x00 0x00 ADR 0x62 LSB MSB LSB MSB 0x00 0x00 0xXX

                Průměrný čas OK kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
                Průměrný čas NG kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
                Metoda výpočtu průměru = XX  , 0 = výpočet vzhledem k délce směny (výchozí); >0 výpočet z uběhlého času směny
             */
            Logger.Debug("");

            var casOk10 = Helper.ToShort(buff[4], buff[5]);
            var casNg10 = Helper.ToShort(buff[6], buff[7]);

            UnitStatus.AvgCasOk = (float)(casOk10 / 10.0);
            UnitStatus.AvgCasNg = (float)(casNg10 / 10.0);

            UnitStatus.CasOkNgTime = DateTime.Now;
            UnitStatus.IsCasOkNg = true;
            Logger.Debug($"unit {Address}");
        }

        public void DoReadZaklNastaveni(byte[] buff)
        {
            UnitStatus.MinOk = buff[4];
            UnitStatus.MinNg = buff[5];
            UnitStatus.VerzeSw1 = buff[7];
            UnitStatus.VerzeSw2 = buff[8];
            UnitStatus.VerzeSw3 = buff[9];
        }

        public void DoReadRozdilKusu(byte[] buff)
        {
            Logger.Debug("");

            var value = Helper.ToShort(buff[8], buff[9]);
            UnitStatus.RozdilKusu = value;
            UnitStatus.RozdilKusuTime = DateTime.Now;
            UnitStatus.IsRozdilKusu = true;
        }

        public void DoReadDefectivita(byte[] buff)
        {
            Logger.Debug("");

            var value = Helper.ToSingle(buff, 7);
            UnitStatus.Defektivita = value;
            UnitStatus.DefektivitaTime = DateTime.Now;
            UnitStatus.IsDefektivita = true;
        }

        public void DoReadStavCitacuKonecSmeny(byte[] buff)
        {
            Logger.Debug("");

            //byte addr = buff[2];
            //byte cmd = buff[3];

            var okVal = Helper.ToShort(buff[4], buff[5]);
            var ngVal = Helper.ToShort(buff[6], buff[7]);
            var stopDuration = Helper.ToShort(buff[8], buff[9]);
            short machineStatusInt = buff[10];

            MachineStateEnum machineStatus = (MachineStateEnum)machineStatusInt;

            //LogMachineStatusForMaintenance(machineStatus); TODO overit u P.

            UnitStatus.SetStopTime(machineStatus);

            UnitStatus.Ok = okVal;
            UnitStatus.Ng = ngVal;
            UnitStatus.Tabule.MachineStatus = machineStatus;
            UnitStatus.Tabule.MachineStopDuration = stopDuration;

            Logger.Info($"okVal:{okVal} ngVal:{ngVal} machineStatus:{machineStatus} unit: {Address}");
        }

        #endregion

        private void LogMachineStatusForMaintenance(MachineStateEnum machineStatus)
        {
             if (machineStatus != UnitStatus.Tabule.MachineStatus)
            {
                string duration = string.Empty;
                if (UnitStatus.Tabule.MachineStopTime.HasValue)
                {
                    TimeSpan diff = (DateTime.Now - UnitStatus.Tabule.MachineStopTime.Value);
                    duration = $"({diff})";
                }
                MaintenanceLogger.Info($"linka {Address}: {UnitStatus.Tabule.MachineStatus} => {machineStatus} {duration}");
            }
        }


        #region public methods - Obtain unit status

        /// <summary>
        /// Ziska status jednotky, provadi se postupnou sekvencí samostatně spouštěných příkazů dle _obtainStatusSequence
        /// Na vysledek se neceka, pouze na potvrzeni odeslani prikazu
        /// Nastaví pro jednotku IsCommandPending  
        /// </summary>
        /// <returns></returns>
        public bool ObtainStatusAsync()
        {
            SetCommandIsPending(true); //na false se nastavuje z jineho vlakna
            CurrentCmdStartTime = DateTime.Now;

            CurrentCmd = _obtainStatusSequence[_obtainStatusIndex++];

            TplLogger.Debug($"+Obtain status [{Address:x2}]");
            TplLogger.Debug($" Send command {CurrentCmd:x2} to [{Address:x2}]");
            if (_obtainStatusIndex >= _obtainStatusSequence.Length) _obtainStatusIndex = 0;

            var res = SendCommand(CurrentCmd);
            if (res)
            {
                TimeSpan duration = DateTime.Now - CurrentCmdStartTime;
                TplLogger.Debug($" Send command {CurrentCmd:x2} to [{Address:x2}] : OK ({duration.Milliseconds} ms)");
                _errorRecordedCnt = 0;
            }
            else
            {
                _errorRecordedCnt++;
                TimeSpan duration = DateTime.Now - CurrentCmdStartTime;
                var s = $" Send command {CurrentCmd:x2} to [{Address:x2}] : Failed {RunConfig.SendAttempts} times ({duration.Milliseconds} ms), errors:{_errorRecordedCnt++}";
                TplLogger.Debug(s);
                ErrorsLogger.Error(s);
                SetCommandIsPending(false);
            }
            TplLogger.Debug($"-Obtain status [{Address:x2}] res:{res}");
            return res;
        }

        /// <summary>
        /// Ziska status jednotky, provadi se postupnou sekvencí příkazů CmdReadStavCitacu, CmdReadCasPosledniOkNg
        /// Probiha synchronne s cekanim nejen na potvrzeni odeslani prikazu ale i na vysledek prikazu
        /// </summary>
        /// <returns></returns>
        public bool ObtainStatusSync()
        {
            CurrentCmdStartTime = DateTime.Now;
         
            CurrentCmd = CmdReadStavCitacu;

            TplLogger.Debug($"+Obtain status [{Address:x2}]");

            TplLogger.Debug($" Send command {CurrentCmd:x2} to [{Address:x2}]");

            var res = SendCommand(CurrentCmd);
            if (res)
            {
                TimeSpan duration = DateTime.Now - CurrentCmdStartTime;
                TplLogger.Debug($" Send command {CurrentCmd:x2} to [{Address:x2}] : OK ({duration.Milliseconds} ms)");

                if (RunConfig.IsWaitCommandResult)
                {
                    var resultReceived = WaitEventCommandResult.WaitOne(RunConfig.ReadResultTimeOut);
                    duration = DateTime.Now - CurrentCmdStartTime;
                    var r = (resultReceived) ? "received" : "expired";
                    TplLogger.Debug($" Command {CurrentCmd:x2} to [{Address:x2}] : result {r} ({duration.Milliseconds} ms)");
                    res = resultReceived;
                }
            }
            else
            {
                TimeSpan duration = DateTime.Now - CurrentCmdStartTime;
                var s = $" Send command {CurrentCmd:x2} to [{Address:x2}] : Failed {RunConfig.SendAttempts} times ({duration.Milliseconds} ms)";
                TplLogger.Debug(s);
                ErrorsLogger.Error(s);
                _errorRecordedCnt++;

                //poslat testovaci paket pomoci SendCommand, pokud neprojde tak vime ze je zaseknuty RF modul a je potreba provest reset

            }

            Thread.Sleep(RunConfig.RelaxTime);

            if (res)
            {
                CurrentCmdStartTime = DateTime.Now;
                CurrentCmd = CmdReadCasPosledniOkNg;

                TplLogger.Debug($" Send command {CurrentCmd:x2} to [{Address:x2}]");

                res = SendCommand(CurrentCmd);

                if (res)
                {
                    TimeSpan duration = DateTime.Now - CurrentCmdStartTime;
                    TplLogger.Debug($" Send command {CurrentCmd:x2} to [{Address:x2}] : OK ({duration.Milliseconds} ms)");

                    if (RunConfig.IsWaitCommandResult)
                    {
                        var resultReceived = WaitEventCommandResult.WaitOne(RunConfig.ReadResultTimeOut);
                        duration = DateTime.Now - CurrentCmdStartTime;
                        var r = (resultReceived) ? "received" : "expired";
                        TplLogger.Debug($" Command {CurrentCmd:x2} to [{Address:x2}] : result {r} ({duration.Milliseconds} ms)");
                        res = resultReceived;
                    }
                }
                else
                {
                    TimeSpan duration = DateTime.Now - CurrentCmdStartTime;
                    var s = $" Send command {CurrentCmd:x2} to [{Address:x2}] : Failed {RunConfig.SendAttempts} times ({duration.Milliseconds} ms)";
                    TplLogger.Debug(s);
                    ErrorsLogger.Error(s);
                    _errorRecordedCnt++;
                }

                Thread.Sleep(RunConfig.RelaxTime);
            }

            TplLogger.Debug($"-Obtain status [{Address:x2}] res:{res}");
            return res;
        }

        public void RecalcTabule()
        {
            if (UnitConfig.IsTypSmennostiA)
            {
                UnitStatus.RecalcTabuleA(UnitConfig);                
            }
            else
            {
                UnitStatus.RecalcTabuleB(UnitConfig);
            }
        }

        #endregion

        #region public methods - other

        public void LoadUnitConfigFromFile(byte addr, string dataFilePath)
        {
            UnitConfig = new UnitConfig();
            UnitConfig.LoadFromFile(addr, dataFilePath);
            UnitStatus.Tabule.LinkaName = UnitConfig.UnitName;
            UnitStatus.Tabule.Addr = addr;
        }

        public void SetCommandIsPending(bool val)
        {
            lock (_lock)
            {
                IsCommandPending = val;
            }
        }

        public void ResponseReceived(byte cmd)
        {
            lock (_lock)
            {
                if (cmd == CurrentCmd)
                {
                    IsCommandPending = false;
                }
            }
        }

        #endregion
      
    }
}