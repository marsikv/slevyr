using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using Slevyr.DataAccess.Model;

namespace Slevyr.DataAccess.Services
{
    public static class SoundService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static RunConfig _runConfig = new RunConfig();
        private static SoundPlayer _alarmSound;
        private static System.Timers.Timer _stopSoundTimer;

        public static void Init(RunConfig runConfig)
        {
            Logger.Info("");

            _runConfig = runConfig;
        }

        public static void StartAlarm()
        {
            Logger.Info("");

            if (!File.Exists(_runConfig.PoplachSoundFile))
            {
                Logger.Error($"Sound file '{_runConfig.PoplachSoundFile}' not found");
                return;
            }
            if (_alarmSound == null) _alarmSound = new SoundPlayer(_runConfig.PoplachSoundFile);
            _alarmSound.PlayLooping();


            if (_stopSoundTimer == null)
            {
                _stopSoundTimer = new System.Timers.Timer();
                _stopSoundTimer.Interval = 5* 1000;    //15 minut
                _stopSoundTimer.Elapsed += (o, args) =>
                {
                    _stopSoundTimer.Stop();
                    _alarmSound?.Stop();
                }; 
            }

            _stopSoundTimer.Stop();
            _stopSoundTimer.Start();


        }

        public static void StopAlarm()
        {
            _alarmSound?.Stop();
        }
    }


}
