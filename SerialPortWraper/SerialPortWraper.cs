using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
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
        
        private StreamReader _reader;

        #region Constructors

        public SerialPortWraper()
        {
        }



        public SerialPortWraper(SerialPortConfig cfg) 
            : base(cfg.Port, cfg.BaudRate, cfg.Parity, cfg.DataBits, cfg.StopBits)
        {
            //ReadTimeout = (int)TimeSpan.FromSeconds(3).TotalMilliseconds;
            //WriteTimeout = (int)TimeSpan.FromSeconds(3).TotalMilliseconds;
            //NewLine = "\r\n";
            //WriteBufferSize = OptimalBufferSize;
            //ReadBufferSize = OptimalBufferSize;
            //ReceivedBytesThreshold = 65535;  // We don't need this event, so max out the threshold
            //Encoding = Encoding.Default;
            DtrEnable = false;
            RtsEnable = false;
            //Handshake = Handshake.None;
        }


        #endregion

        #region Public Methods

        public async Task ReadAsync( byte[] buffer, int offset, int count)
        {
            int bytesToRead = count;
            var temp = new byte[count];

            while (bytesToRead > 0)
            {
                int readed = await this.BaseStream.ReadAsync(temp, 0, bytesToRead);
                Array.Copy(temp, 0, buffer, offset + count - bytesToRead, readed);
                bytesToRead -= readed;
            }
        }

        public async Task<byte[]> ReadAsync(int count)
        {
            var datos = new byte[count];
            await this.ReadAsync(datos, 0, count);
            return datos;
        }

        public new void Open()
        {
            base.Open();

            /* The .Net SerialStream class has a bug that causes its finalizer to crash when working 
             * with virtual COM ports (e.g. FTDI, Prolific, etc.) See the following page for details:
             * http://social.msdn.microsoft.com/Forums/en-US/netfxbcl/thread/8a1825d2-c84b-4620-91e7-3934a4d47330
             * To work around this bug, we suppress the finalizer for the BaseStream and close it ourselves instead.
             * See the Dispose method for the other half of this workaround.
             */
            GC.SuppressFinalize(BaseStream);
        }


        public new string ReadLine()
        {
            /* On the HTC P3300, an OutOfMemoryException occurs when using SerialPort.ReadLine().
             * However, a StreamReader.ReadLine() works just fine.  This suggests that SerialPort.ReadLine()
             * is buggy!  Use a StreamReader to get the job done.
             */
            if (_reader == null)
            {
                _reader = new StreamReader(BaseStream, Encoding.ASCII, false, OptimalBufferSize);
            }

            return _reader.ReadLine();
        }

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

