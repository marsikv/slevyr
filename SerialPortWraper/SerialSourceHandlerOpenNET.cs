using System;
//using OpenNETCF.IO.Serial;
using System.Text;

namespace SledovaniVyroby.SerialPortWraper
{
    
    /// <summary>    
    /// serial source based on OpenNETCF.IO.Serial.Port
    /// see http://serial.codeplex.com/
    /// </summary>
    /*
    public class SerialSourceHandlerOpenNET : SourceHandler
    {
        private volatile object _locker = new object();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private Port _sp;
        private SerialPortConfig _config;

        public SerialPortConfig Config
        {
            get
            {
                return new SerialPortConfig(_config);
            }
        }

        #region Constructors / Destructors

        public SerialSourceHandlerOpenNET(string id, string port, int baudRate)
            : base(id)
        {
            _config = new SerialPortConfig();
            _config.Port = port;
            _config.BaudRate = baudRate;
        }

        public SerialSourceHandlerOpenNET(SerialPortConfig config)
        {
            _config = new SerialPortConfig(config);
        }

        public SerialSourceHandlerOpenNET(string id, SerialPortConfig config)
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
            //TODO co to ma delat a jak imlementovat v OpenNETCF.IO.Serial
            //_sp.Write(data, 0, data.Length);
        }

        protected override bool PauseImpl()
        {
            return true;
        }

        protected override bool StartImpl()
        {
            logger.Info("+");

            bool res = false;

            lock (_locker)
            {
                Stop();

                var portSettings = new HandshakeNone();
                portSettings.BasicSettings.BaudRate = (BaudRates)_config.BaudRate;
                portSettings.BasicSettings.ByteSize = (byte)_config.DataBits;
                portSettings.BasicSettings.Parity = (Parity)((int)_config.Parity);
                portSettings.BasicSettings.StopBits = ConvertStopBitsToOpenNET(_config.StopBits);//TODO OpenNET does not have "StopBits.None"

                _sp = new Port(_config.Port, portSettings)
                {
                    RThreshold = 32,	// get an event for every 32 byte receive, previous value was 1
                    InputLen = 1,	// calling Input will read 1 byte
                    SThreshold = 1
                };

                //add event handler
                _sp.DataReceived += new Port.CommEvent(_sp_DataReceived);

                logger.Info("open " + _config.Port);

                //_sp.Open();

                try
                {
                   res = _sp.Open();                   
                }
                catch (Exception ex)
                {
                    logger.ErrorException(String.Format("Open port {0}", _config.Port), ex);
                }
            }

            logger.Info("-");

            return res;
        }

        protected override void StopImpl()
        {
            logger.Info("+");

            lock (_locker)
            {
                Port sp = _sp;
                if (sp != null)
                {
                    _sp = null;

                    sp.DataReceived -= _sp_DataReceived;
                    if (sp.IsOpen) sp.Close();
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

        void _sp_DataReceived()
        {
            //Port sp = _sp;
            //if (sp != null)
            //{
            //    // since RThreshold = 1, we get an event for every character
            //    byte[] inputData = new byte[sp.RThreshold];

            //    // read the character
            //    inputData = sp.Input;

            //    // display as text
            //    Encoding enc = Encoding.ASCII;
            //    var inputString = enc.GetString(inputData, 0, inputData.Length);

            //    OnReceived(inputString);
            //}
            try
            {
                Port sp = _sp;
                if (sp != null)
                {
                    byte[] inputData;

                    StringBuilder sb = new StringBuilder();

                    while (sp.InBufferCount > 0)   //read all buffer content
                    {
                        inputData = sp.Input;
                        sb.Append(Encoding.ASCII.GetString(inputData, 0, inputData.Length));
                    }

                    OnReceived(sb.ToString());
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("", ex) ;
            }
        }

        public static OpenNETCF.IO.Serial.StopBits ConvertStopBitsToOpenNET(System.IO.Ports.StopBits stopBits)
        {
            switch (stopBits)
            {
                case System.IO.Ports.StopBits.None:
                    {
                        logger.Warn("OpenNETCF.IO.Serial does not support \"StopBits.None\"");
                        return StopBits.one;
                    }
                case System.IO.Ports.StopBits.One:
                    return StopBits.one;
                case System.IO.Ports.StopBits.OnePointFive:
                    return StopBits.onePointFive;
                case System.IO.Ports.StopBits.Two:
                    return StopBits.two;
                default:
                    return StopBits.one;
            }
        }
    }
     */
}
