using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;


namespace SledovaniVyroby.SerialPortWraper
{
    /// <summary>
    /// Wraps the .Net <see cref="System.IO.Ports.SerialPort"/> class and implements workarounds for some
    /// known bugs and limitations.
    /// </summary>
    public class SerialPortWraper : System.IO.Ports.SerialPort
    {
        private const int OptimalBufferSize = 128;
        
        //private StreamReader _reader;

        #region Constructors

        public SerialPortWraper()
        {
        }


        public SerialPortWraper(SerialPortConfig cfg) 
            : base(cfg.Port, cfg.BaudRate, cfg.Parity, cfg.DataBits, cfg.StopBits)
        {          
            DtrEnable = false;
            RtsEnable = false;
            Handshake = Handshake.None;
            PortName = cfg.Port;
            BaudRate = cfg.BaudRate;
            DataBits = cfg.DataBits;
            StopBits = cfg.StopBits;
            ReceivedBytesThreshold = cfg.ReceivedBytesThreshold;
        }


        #endregion

        #region Public Methods

        public void Write(byte[] inBuff, int buffLength)
        {
            base.Write(inBuff, 0, buffLength);
        }

        public async Task WriteAsync(byte[] buffer, int count)
        {
            await this.BaseStream.WriteAsync(buffer, 0, count);
        }

        private async Task ReadAsync( byte[] buffer, int offset, int count, CancellationToken token)
        {
            int bytesToRead = count;
            var temp = new byte[count];

            while (bytesToRead > 0)
            {
                if (token.IsCancellationRequested)
                {
                    //clear buffer ?                    
                    return;
                }
                int readed = await this.BaseStream.ReadAsync(temp, 0, bytesToRead, token);
                Array.Copy(temp, 0, buffer, offset + count - bytesToRead, readed);
                bytesToRead -= readed;
            }
        }

        public async Task<byte[]> ReadAsync(int count, CancellationToken token)
        {
            var data = new byte[count];
            await this.ReadAsync(data, 0, count, token);
            return data;
        }



        //public new void Open()
        //{

        //    /* The .Net SerialStream class has a bug that causes its finalizer to crash when working 
        //     * with virtual COM ports (e.g. FTDI, Prolific, etc.) See the following page for details:
        //     * http://social.msdn.microsoft.com/Forums/en-US/netfxbcl/thread/8a1825d2-c84b-4620-91e7-3934a4d47330
        //     * To work around this bug, we suppress the finalizer for the BaseStream and close it ourselves instead.
        //     * See the Dispose method for the other half of this workaround.
        //     */
        //    GC.SuppressFinalize(BaseStream);
        //}


        //public new string ReadLine()
        //{
        //    /* On the HTC P3300, an OutOfMemoryException occurs when using SerialPort.ReadLine().
        //     * However, a StreamReader.ReadLine() works just fine.  This suggests that SerialPort.ReadLine()
        //     * is buggy!  Use a StreamReader to get the job done.
        //     */
        //    if (_reader == null)
        //    {
        //        _reader = new StreamReader(BaseStream, Encoding.ASCII, false, OptimalBufferSize);
        //    }

        //    return _reader.ReadLine();
        //}

        #endregion

        #region Implementation of IDisposable


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    /* The .Net SerialStream class has a bug that causes its finalizer to crash when working 
                     * with virtual COM ports (e.g. FTDI, Prolific, etc.) See the following page for details:
                     * http://social.msdn.microsoft.com/Forums/en-US/netfxbcl/thread/8a1825d2-c84b-4620-91e7-3934a4d47330
                     * To work around this bug, we suppress the finalizer for the BaseStream and close it ourselves instead.
                     * See the Open method for the other half of this workaround.
                     */
                    if (IsOpen)
                    {
                        BaseStream.Close();
                    }
                }
                catch
                {
                    // The BaseStream is already closed, disposed, or in an invalid state. Ignore and continue disposing.
                }
            }

            base.Dispose(disposing);
        }


        #endregion
    }
}

