using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SledovaniVyroby.SledovaniVyrobyService
{
    public static class Helper
    {
        public static short ToShort(byte lsb, byte msb)
        {
            return (short) ((msb << 8) + lsb);
        }

        public static Single ToSingle(byte[] input, int start)
        {
            return BitConverter.ToSingle(input, start);
        }

        public static void FromShort(short number, out byte lsb, out byte msb)
        {
            msb = (byte) (number >> 8);
            lsb = (byte) (number & 255);
        }
    }
}
