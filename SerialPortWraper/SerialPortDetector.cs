using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SledovaniVyroby.SerialPortWraper
{
    public class SerialPortDetectorProgressEventArgs : EventArgs
    {
        public SerialPortDetectorProgressEventArgs(double progress, IEnumerable<SerialPortConfig> detected)
        {
            Progress = progress;
            Detected = detected;
        }

        public double Progress { get; private set; }
        public IEnumerable<SerialPortConfig> Detected { get; private set; }
        public bool CancellationPending { get; set; }
    }

    public class SerialPortDetector
    {
        private static volatile object _locker = new object();

        private List<SerialPortConfig> _defaultConfigs = new List<SerialPortConfig>();

        public event EventHandler<SerialPortDetectorProgressEventArgs> Progress;

        #region Constructors / Destructors

        public SerialPortDetector()
        {
            // Common GNSS bit rates ordered by its probability: 4800, 9600, 19200, 38400, 57600, 14400, 115200, 2400, 1200 bps.

            // Prepare default SerialPortConfig's
            _defaultConfigs.Add(new SerialPortConfig() { BaudRate = 4800 });
            _defaultConfigs.Add(new SerialPortConfig() { BaudRate = 9600 });
            _defaultConfigs.Add(new SerialPortConfig() { BaudRate = 38400 });
            _defaultConfigs.Add(new SerialPortConfig() { BaudRate = 19200 });
            _defaultConfigs.Add(new SerialPortConfig() { BaudRate = 57600 });
            _defaultConfigs.Add(new SerialPortConfig() { BaudRate = 14400 });
            _defaultConfigs.Add(new SerialPortConfig() { BaudRate = 115200 });
            _defaultConfigs.Add(new SerialPortConfig() { BaudRate = 2400 });
            _defaultConfigs.Add(new SerialPortConfig() { BaudRate = 1200 });
        }

        #endregion

        #region Public static methods

        private const string SP_NAME_GROUPID = "name";
        private const string SP_NAME_REGEX = "(?<" + SP_NAME_GROUPID + ">COM[0-9]+).*";

        public static string[] GetPortNames()
        {
            Regex regex = new Regex(SP_NAME_REGEX);
            return System.IO.Ports.SerialPort.GetPortNames().Select(name =>
            {
                Match match = regex.Match(name);
                if (match.Success)
                {
                    return match.Groups[SP_NAME_GROUPID].Value;
                }
                else
                {
                    return name + " (invalid name !!!)";
                }
            }).ToArray();
        }

        #endregion

        #region Public methods

        public SerialPortConfig[] Detect()
        {
            return Detect(_defaultConfigs);
        }

        public SerialPortConfig[] Detect(IEnumerable<SerialPortConfig> configs)
        {
            lock (_locker)
            {
                List<SerialPortConfig> detected = new List<SerialPortConfig>();
                string[] ports = GetPortNames();

                int total = ports.Length * configs.Count();
                int tested = 0;

                OnProgress(0, detected.Select(c => new SerialPortConfig(c)));

                bool cancel = false;

                Parallel.ForEach(ports, port =>
                {
                    if (!cancel)
                    {
                        for (int j = 0; j < configs.Count(); j++)
                        {
                            SerialPortConfig config = new SerialPortConfig(configs.ElementAt(j));
                            config.Port = port;

                            if (CheckConfig(config))
                            {
                                detected.Add(config);

                                Interlocked.Add(ref tested, configs.Count() - j);
                                break;
                            }

                            Interlocked.Increment(ref tested);

                            // compute progress information and fire event
                            cancel = OnProgress((double)tested / total, detected.Select(c => new SerialPortConfig(c)));
                            if (cancel)
                            {
                                break;
                            }
                        }
                    }
                });

                if (cancel)
                {
                    //System.Windows.MessageBox.Show("Detection was canceled");
                }
                else
                {
                    OnProgress(1, detected.Select(c => new SerialPortConfig(c)).ToArray());
                }

                return detected.ToArray();
            }
        }

        private bool CheckConfig(SerialPortConfig config)
        {
            return true;
        }

        #endregion

        #region Protected methods

        protected bool OnProgress(double progress, IEnumerable<SerialPortConfig> detected)
        {
            if (Progress != null)
            {
                SerialPortDetectorProgressEventArgs eventArg = new SerialPortDetectorProgressEventArgs(progress, detected);
                Progress(this, eventArg);
                if (eventArg.CancellationPending)
                {
                    return true;
                }
                return eventArg.CancellationPending;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Private methods

        #endregion

    }
}
