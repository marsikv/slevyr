
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

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="parent"></param>
        public SerialPortConfig(SerialPortConfig parent)
        {
            Update(parent);
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


        public int ReceiveLength{ get; set; }

        public override string ToString()
        {
            return $"Port={Port},Baudrate={BaudRate},Parity={Parity},StopBits={StopBits},DataBits={DataBits}";
        }

        /// <summary>
        /// Update own attributes by configuration passed as methods parameter.
        /// </summary>
        /// <param name="spc">Original configuration to update by.</param>
        public virtual void Update(SerialPortConfig parent)
        {
            Port = parent.Port;
            BaudRate = parent.BaudRate;
            Parity = parent.Parity;
            DataBits = parent.DataBits;
            StopBits = parent.StopBits;
        }
    }
}
