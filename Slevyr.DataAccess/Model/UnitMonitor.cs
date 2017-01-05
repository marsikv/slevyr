﻿using System;
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

        public UnitStatus UnitStatus { get; set; }

        public UnitConfig UnitConfig { get; set; }

        private volatile object _lock = new object();

        private static bool _errorRecorded;
        private static int _errorRecordedCnt;

        #endregion

        #region const pro prikazy

        const byte CmdZaklNastaveni = 3	; //Zakladni nastaveni
        const byte CmdSetHodnotyCitacu = 4	; //Zapise hodnoty do citacu
        //const byte CmdSet = 5	; //Vyvola programovaci mod adresy
        const byte CmdSetParamRf = 6	; //Nastavi parametry RF
        const byte CmdResetJednotky = 7	; //Reset jednotky
        const byte CmdZapsatEeprom= 15; //Zapise do eeprom od adresy 5 bytu

        const byte CmdSetSmennost = 16; //nastavi variantu smeny
        const byte CmdSetDatumDen = 17; //nastavi cas datum a den
        const byte CmdSetCilSmen = 18;  //nastavi cile smen
        const byte CmdSetDefSmen = 19;  //nastavi cile defektivity smen
        const byte CmdSetJasLed = 20;   //nastavi jas LED panelu
        const byte CmdSetZacPrestav = 21;   //nastavi zacatky prestavek

        const byte CmdReadVerSwRFT = 1; //vrati verzi sw RFT modulu + panel
        const byte CmdReadZakSysNast = 9; //vrati zakladni systemove nastaveni
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

        public bool SendSetDefektivita(char varianta, short def1, short def2, short def3)
        {
            Logger.Info($"+ unit {Address}");
           
            UnitConfig.Def1Smeny = def1;
            UnitConfig.Def2Smeny = def2;
            UnitConfig.Def3Smeny = def3;

            //return SendCommand(CmdSetDefSmen, (byte)varianta, (short)(def1 * 10.0), (short)(def2 * 10.0), (short)(def3 * 10.0));
            return SendCommand(CmdSetDefSmen, (byte)varianta, def1, def2, def3);
        }

        //TODO - v jakych jednotkach se zadavaji prestavky ?
        public bool SendSetPrestavky(char varianta, TimeSpan prest1, TimeSpan prest2, TimeSpan prest3)
        {
            Logger.Info($"+ unit {Address}");
          
            UnitConfig.Prestavka1Smeny = prest1.ToString();  //TODO ukladat TimeSpan, ne string
            UnitConfig.Prestavka2Smeny = prest2.ToString();
            UnitConfig.Prestavka3Smeny = prest3.ToString();
            
            return SendCommand(CmdSetZacPrestav, (byte)varianta, prest1, prest2, prest3);
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
            var stopTime = Helper.ToShort(buff[8], buff[9]);
            short machineStatus = buff[10];

            UnitStatus.Ok = okVal;
            UnitStatus.Ng = ngVal;
            UnitStatus.MachineStatus = (MachineStateEnum)machineStatus;
            UnitStatus.MachineStopTime = stopTime;

            UnitStatus.OkNgTime = DateTime.Now;
            UnitStatus.IsOkNg = true;

            Logger.Info($"okVal:{okVal} ngVal:{ngVal} machineStatus:{machineStatus} unit: {Address}");
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

            byte addr = buff[2];
            byte cmd = buff[3];

            var okVal = Helper.ToShort(buff[4], buff[5]);
            var ngVal = Helper.ToShort(buff[6], buff[7]);
            var stopTime = Helper.ToShort(buff[8], buff[9]);
            short machineStatus = buff[10];

            UnitStatus.LastOk = okVal;
            UnitStatus.LastNg = ngVal;
            UnitStatus.LastMachineStatus = (MachineStateEnum)machineStatus;
            UnitStatus.LastMachineStopTime = stopTime;


            Logger.Info($"okVal:{okVal} ngVal:{ngVal} machineStatus:{machineStatus} unit: {Address}");
        }

        #endregion

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
            }
            else
            {
                TimeSpan duration = DateTime.Now - CurrentCmdStartTime;
                var s = $" Send command {CurrentCmd:x2} to [{Address:x2}] : Failed {RunConfig.SendAttempts} times ({duration.Milliseconds} ms)";
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
                }

                Thread.Sleep(RunConfig.RelaxTime);
            }

            TplLogger.Debug($"-Obtain status [{Address:x2}] res:{res}");
            return res;
        }

        public void RecalcTabule()
        {
            UnitStatus.RecalcTabule(UnitConfig);
        }

        #endregion

        #region public methods - other

        public void LoadUnitConfigFromFile(byte addr, string dataFilePath)
        {
            UnitConfig = new UnitConfig();
            UnitConfig.LoadFromFile(addr, dataFilePath);
            UnitStatus.Tabule.LinkaName = UnitConfig.UnitName;
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

        #region old sync with await

        private byte[] _outBuff;

        private bool OldCheckResponseOk()
        {
            var res = (_outBuff != null) && (_outBuff[0] == 0 && _outBuff[1] == 0 && _outBuff[2] == Address && (_outBuff[3] == CurrentCmd || CurrentCmd != 20)); //doplneno podle popisu z mailu 28.8. - neni nutne kontrolovat
            if (!res)
            {
                //UnitStatus.SendError = true;
                //UnitStatus.LastSendErrorDescription = $"cmd=" + _cmd;
            }
            Logger.Debug($"res={res}");
            return res;
        }

        private bool OldCheckSendOk()
        {
            var res = (_outBuff != null) && ((_outBuff[0] == 4 || _outBuff[0] == 0) && _outBuff[1] == 0 && _outBuff[2] == Address);
            if (!res)
            {
                //UnitStatus.SendError = true;
                //UnitStatus.LastSendErrorDescription = $"cmd=" + _cmd;
            }
            Logger.Debug($"res={res}");
            return res;
        }


        /// <summary>
        /// posle prikaz na port, parametry jsou jen pro logovani cisla pokusu
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <returns></returns>
        private bool OldSendCommandBasic(int a1, int a2)
        {
            bool res;
            Logger.Debug($"+ attempt:{a1}.{a2}");

            if (!SlevyrService.CheckIsPortOpen()) return false;

            try
            {
                //odeslat pripraveny command s parametry
                var wtask = SlevyrService.SerialPort.WriteAsync(_sendBuff, BuffLength);
                wtask.Wait(RunConfig.SendCommandTimeOut);

                Thread.Sleep(7);

                Logger.Debug(" -w");

                //kontrola odeslání
                var task = SlevyrService.SerialPort.ReadAsync(11);
                task.Wait(RunConfig.SendCommandTimeOut);

                if (!task.IsCompleted)
                {
                    Logger.Debug(" -timeout");
                    res = false;
                }
                else
                {
                    Logger.Debug(" -ok");
                    _outBuff = task.Result;
                    res = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                res = false;
            }

            res = res && OldCheckSendOk();
            Logger.Debug($"-");

            return res;
        }

        //odesle prikaz, v _inBuffer musi byt prikaz nachystany
        //provede kontrolu odeslani, 
        //udela 3 pokusy
        private bool OldSendCommand(int a)
        {
            Logger.Debug("+");

            bool sendOk = false;

            lock (_lock)
            {
                sendOk = OldSendCommandBasic(a, 1);

                if (!sendOk)
                {
                    Thread.Sleep(RunConfig.RelaxTime);
                    DiscardBuffers();
                    sendOk = OldSendCommandBasic(a, 2);
                }

                if (!sendOk)
                {
                    Thread.Sleep(RunConfig.RelaxTime);
                    DiscardBuffers();
                    sendOk = OldSendCommandBasic(a, 3);
                }

                if (!sendOk)
                {
                    DiscardBuffers();
                }
            }

            Logger.Debug($"- res:{sendOk}");
            return sendOk;
        }

        private bool OldReceiveResults(int a)
        {
            bool res = false;
            Logger.Debug($"+ attempt:{a}");

            if (!SlevyrService.CheckIsPortOpen()) return false;

            lock (_lock)
            {
                //precte vysledky
                try
                {
                    var task = SlevyrService.SerialPort.ReadAsync(11);

                    task.Wait(RunConfig.ReadResultTimeOut);
                    if (!task.IsCompleted)
                    {
                        Logger.Debug(" -timeout");
                    }
                    else
                    {
                        Logger.Debug(" -ok");
                        _outBuff = task.Result;
                        res = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    res = false;
                }
            }

            res = res && OldCheckResponseOk();

            Thread.Sleep(RunConfig.RelaxTime);  //cekame po precteni vysledku pred tim nez se posle dalsi pozadavek

            Logger.Debug($"-");
            return res;
        }



        /// <summary>
        /// posle pozadavek a precte vysledek.
        /// provadi dva pokusy
        /// </summary>
        /// <returns></returns>
        private bool OldSendReceiveResults()
        {
            bool res = false;

            Logger.Debug("+");

            lock (_lock)
            {
                if (OldSendCommand(1))
                {
                    Thread.Sleep(RunConfig.RelaxTime);

                    res = OldReceiveResults(1);

                    if (!res)
                    {
                        Thread.Sleep(RunConfig.RelaxTime);
                        DiscardBuffers();
                        if (OldSendCommand(2))
                        {
                            Thread.Sleep(RunConfig.RelaxTime);
                            res = OldReceiveResults(2);
                            if (!res)
                            {
                                Thread.Sleep(RunConfig.RelaxTime);
                                DiscardBuffers();
                            }
                        }
                    }
                }
            }

            Logger.Debug($"- res:{res}");

            return res;
        }

        private bool OldReadCasOkNg(out Single value)
        {

            /*
            * Pro příkaz 0x61 je odpověď = 0x00 0x00 ADR 0x61 LSB MSB LSB MSB 0x00 0x00 0xXX
               čas posledního OK kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
               čas posledního NG kusu =(LSB+(256*MSB))/10 , Sekundy na desetiny
               Metoda měření cyklu = XX  , 0 = čas posledního cyklu (výchozí); >0 čas od posledního cyklu
            */

            Logger.Debug($"+ unit {Address}");

            if (RunConfig.IsMockupMode)
            {
                value = (Single)DateTime.Now.Hour * 2;  //hod. poslouzi jako hodnota casu ok
                UnitStatus.CasOk = value;
                UnitStatus.CasOkNgTime = DateTime.Now;
                return true;
            }


            OldPrepareCommand(CmdReadCasPosledniOkNg);

            value = 0;

            var res = OldSendReceiveResults();

            if (res)
            {
                var casOk10 = Helper.ToShort(_outBuff[4], _outBuff[5]);
                var casNg10 = Helper.ToShort(_outBuff[6], _outBuff[7]);

                UnitStatus.CasOk = (float)(casOk10 / 10.0);
                UnitStatus.CasNg = (float)(casNg10 / 10.0);

                UnitStatus.CasOkNgTime = DateTime.Now;
            }

            UnitStatus.IsCasOkNg = res;

            Logger.Debug($"- unit {Address}");

            return res;
        }


        private bool OldReadStavCitacu(out int okVal, out int ngVal)
        {
            Logger.Debug($"+ unit {Address}");

            if (RunConfig.IsMockupMode)
            {
                okVal = (short)DateTime.Now.Minute;  //minuta poslouzi jako hodnota ok
                ngVal = (short)((okVal + 1) / 2);
                UnitStatus.Ok = okVal;
                UnitStatus.Ng = ngVal;
                UnitStatus.OkNgTime = DateTime.Now;
                return true;
            }

            OldPrepareCommand(CmdReadStavCitacu);

            okVal = 0;
            ngVal = 0;

            var res = OldSendReceiveResults();

            if (res)
            {
                //načtení výsledku

                okVal = Helper.ToShort(_outBuff[4], _outBuff[5]);
                ngVal = Helper.ToShort(_outBuff[6], _outBuff[7]);

                var stopTime = Helper.ToShort(_outBuff[8], _outBuff[9]);
                short machineStatus = _outBuff[10];


                //short machineStatus = _outBuff[8];
                //short shutdownTime = Helper.ToShort(_outBuff[9], _outBuff[10]);

                UnitStatus.Ok = okVal;
                UnitStatus.Ng = ngVal;
                UnitStatus.MachineStatus = (MachineStateEnum)machineStatus;
                UnitStatus.MachineStopTime = stopTime;

                UnitStatus.OkNgTime = DateTime.Now;
            }
            else
            {
                UnitStatus.Ok = -1;
                UnitStatus.Ng = -1;
                UnitStatus.OkNgTime = DateTime.MaxValue;
                UnitStatus.ErrorTime = DateTime.Now;
                UnitStatus.MachineStatus = MachineStateEnum.Neznamy;
            }

            UnitStatus.IsOkNg = res;

            Logger.Debug($"- {res} machineState {UnitStatus.MachineStatus}/unit {Address}");

            return res;
        }


        public bool OldObtainStatusSync()
        {
            Logger.Info($"+ *** unit {Address}");

            int ok, ng;
            Single casOk = -1, casNg = -1;
            var res = OldReadStavCitacu(out ok, out ng);
            var s = res ? "" : "err";
            Logger.Info($">ok:{ok} ng:{ng} " + s);

            if (!res)
            {
                _errorRecordedCnt++;
                if (!_errorRecorded)
                {
                    ErrorsLogger.Error($"ReadStavCitacu;{Address};total errors:{_errorRecordedCnt}");
                    _errorRecorded = true;
                }
            }
            else
            {
                if (_errorRecorded)
                {
                    ErrorsLogger.Info($"ReadStavCitacu;{Address};recovered");
                    _errorRecorded = false;
                }
            }

            if (res && RunConfig.IsReadOkNgTime)
            {
                res = OldReadCasOkNg(out casOk);
                Logger.Info($">casOk:{casOk}");
            }

            if (res)
            {
                //prepocitat pro zobrazeni tabule
                try
                {
                    RecalcTabule();

                    UnitStatus.LastCheckTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            Logger.Info($"-");

            return res;
        }

        #endregion
    }
}