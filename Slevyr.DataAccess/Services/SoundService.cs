using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Slevyr.DataAccess.Model;
using Timer = System.Timers.Timer;

namespace Slevyr.DataAccess.Services
{
    public static class SoundService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static RunConfig _runConfig = new RunConfig();
        private static SoundPlayer _soundPlayerAlarm;
        private static SoundPlayer _soundPlayerHlaseniSmen;
        private static SoundPlayer _soundPlayerHlaseniPrestavek;
        private static Timer _stopSoundTimer;

        public static void Init(RunConfig runConfig)
        {
            Logger.Info("");

            _runConfig = runConfig;
        }

        public static void PlayHlaseniSmen()
        {
            Logger.Info("");

            if (!_runConfig.IsHlaseniSmenSoundEnabled) return;

            if (!File.Exists(_runConfig.HlaseniSmenSoundFile))
            {
                Logger.Error($"Sound file '{_runConfig.HlaseniSmenSoundFile}' not found");
                return;
            }

            //pokud je alarm aktivni tak neprehravat ?

            if (_soundPlayerHlaseniSmen == null) _soundPlayerHlaseniSmen = new SoundPlayer(_runConfig.HlaseniSmenSoundFile);
            //_soundPlayerHlaseniSmen.Play();

            //poslat prikaz jednotce pro zacatek prenosu
            if (_runConfig.TransmitSound)
            {
                SlevyrService.StartRozhlas();
            }

            //tri sekundy pockat
            Thread.Sleep(4000);

            var task = Task.Run(() => _soundPlayerHlaseniSmen.PlaySync());
            task.Wait(100000);  //cekame 100sec max na prehrati zvuku

            //poslat prikaz jednotce pro konec prenosu
            if (_runConfig.TransmitSound)
            {
                SlevyrService.StopRozhlas();
            }
        }



        public static void PlayHlaseniPrestavek()
        {
            Logger.Info("");

            if (!_runConfig.IsHlaseniPrestavekSoundEnabled) return;

            if (!File.Exists(_runConfig.HlaseniPrestavekSoundFile))
            {
                Logger.Error($"Sound file '{_runConfig.HlaseniPrestavekSoundFile}' not found");
                return;
            }

            //pokud je alarm aktivni tak neprehravat ?

            if (_soundPlayerHlaseniPrestavek == null) _soundPlayerHlaseniPrestavek = new SoundPlayer(_runConfig.HlaseniPrestavekSoundFile);

            //poslat prikaz jednotce pro zacatek prenosu
            if (_runConfig.TransmitSound)
            {
                SlevyrService.StartRozhlas();
            }

            //tri sekundy pockat
            Thread.Sleep(4000);

            var task =  Task.Run(() => _soundPlayerHlaseniPrestavek.PlaySync());
            task.Wait(100000);  //cekame 100sec max na prehrati zvuku

            //_soundPlayerHlaseniPrestavek.Play();

            //poslat prikaz jednotce pro konec prenosu
            if (_runConfig.TransmitSound)
            {
                SlevyrService.StopRozhlas();
            }
            

        }

        public static void StartAlarm()
        {
            Logger.Info("");

            if (!_runConfig.IsPoplachSoundEnabled) return;

            if (!File.Exists(_runConfig.PoplachSoundFile))
            {
                Logger.Error($"Sound file '{_runConfig.PoplachSoundFile}' not found");
                return;
            }

            //poslat prikaz jednotce pro zacatek prenosu
            if (_runConfig.TransmitSound)
            {
                SlevyrService.StartRozhlas();
            }

            //tri sekundy pockat
            Thread.Sleep(4000);

            if (_soundPlayerAlarm == null) _soundPlayerAlarm = new SoundPlayer(_runConfig.PoplachSoundFile);
            _soundPlayerAlarm.PlayLooping();


            if (_stopSoundTimer == null)
            {
                _stopSoundTimer = new System.Timers.Timer();
                _stopSoundTimer.Interval = 15 * 60 * 1000;    //15 minut
                _stopSoundTimer.Elapsed += (o, args) =>
                {
                    _stopSoundTimer.Stop();
                    _soundPlayerAlarm?.Stop();
                    if (_runConfig.TransmitSound)
                    {
                        SlevyrService.StopRozhlas();
                    }
                }; 
            }

            _stopSoundTimer.Stop();
            _stopSoundTimer.Start();
        }

        public static void StopAlarm()
        {
            _soundPlayerAlarm?.Stop();
            if (_runConfig.TransmitSound)
            {
                SlevyrService.StopRozhlas();
            }
        }
    }


}
