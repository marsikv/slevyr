using System;
using NLog;

namespace GnssTool.NMEA.Sources
{
    public class SerialSourceHandler : SourceHandler
    {
        private volatile object _locker = new object();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private SerialPortWraper _sp;
        private SerialPortConfig _config;

        public SerialPortConfig Config
        {
            get
            {
                return new SerialPortConfig(_config);
            }
        }

        #region Constructors / Destructors

        public SerialSourceHandler(string id, string port, int baudRate)
            : base(id)
        {
            _config = new SerialPortConfig();
            _config.Port = port;
            _config.BaudRate = baudRate;
        }

        public SerialSourceHandler(SerialPortConfig config)
        {
            _config = new SerialPortConfig(config);
        }

        public SerialSourceHandler(string id, SerialPortConfig config)
            : base(id)
        {
            _config = new SerialPortConfig(config);
        }

        protected override void Disposing()
        {
            Stop();
        }

        #endregion

        protected override void WriteImpl(byte[] data)
        {
            _sp.Write(data, 0, data.Length);
        }

        protected override bool PauseImpl()
        {
            return true;
        }

        protected override bool StartImpl()
        {
            logger.Info("+");

            lock (_locker)
            {
                Stop();

                _sp = new SerialPortWraper();
                _sp.PortName = _config.Port;
                _sp.BaudRate = _config.BaudRate;
                _sp.DataBits = _config.DataBits;
                _sp.Parity = _config.Parity;
                _sp.StopBits = _config.StopBits;

                //SerialPortFixer can help
                //logger.Info("SerialPortFixer exec " + _config.Port);
                //SerialPortFixer.Execute(_config.Port);

                //add event handler
                _sp.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(_sp_DataReceived);

                logger.Info("open " + _config.Port);
                _sp.Open();
            }

            logger.Info("-");

            return true;
        }

        protected override void StopImpl()
        {
            logger.Info("+");

            lock (_locker)
            {
                System.IO.Ports.SerialPort sp = _sp;
                if (sp != null)
                {                    
                    _sp = null;

                    sp.Close();
                    try
                    {
                        sp.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            logger.Info("-");
        }

        void _sp_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (e != null)
            {
                System.IO.Ports.SerialPort sp = _sp;
                if (sp != null)
                {
                    OnReceived(sp.ReadExisting());
                }
            }
        }
    }
}
