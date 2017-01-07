
namespace SledovaniVyroby.SerialPortWraper
{
    public class SerialPortConfig
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SerialPortConfig()
        {
            //Default values
            Parity = System.IO.Ports.Parity.None;
            DataBits = 8;
            StopBits = System.IO.Ports.StopBits.One;
        }

        public SerialPortConfig(SerialPortConfig cfg)
        {
            Port = cfg.Port;
            BaudRate = cfg.BaudRate;
            Parity = cfg.Parity;
            DataBits = cfg.DataBits;
            StopBits = cfg.StopBits;
            ReceivedBytesThreshold = cfg.ReceivedBytesThreshold;
        }

        /// <summary>
        /// property to hold the PortName
        /// of our manager class
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Property to hold the BaudRate
        /// of our manager class
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// property to hold the Parity
        /// of our manager class
        /// </summary>
        public System.IO.Ports.Parity Parity { get; set; }

        /// <summary>
        /// property to hold the StopBits
        /// of our manager class
        /// </summary>
        public System.IO.Ports.StopBits StopBits { get; set; }

        /// <summary>
        /// Gets or sets the standard length of data bits per byte.
        /// </summary>
        public int DataBits { get; set; }

        public int ReceivedBytesThreshold { get; set; } //ma smysl jen pokud je UseDataReceivedEvent
   

        public override string ToString()
        {
            return $"Port={Port},Baudrate={BaudRate},Parity={Parity},StopBits={StopBits},DataBits={DataBits}";
        }

    }
}
