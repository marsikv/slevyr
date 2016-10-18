using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Slevyr.DataAccess.Services
{
    public static class Helper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

        public static string GetDescriptionFromEnumValue(Enum value)
        {
            try
            {
                DescriptionAttribute attribute = value.GetType()
                       .GetField(value.ToString())
                       .GetCustomAttributes(typeof(DescriptionAttribute), false)
                       .SingleOrDefault() as DescriptionAttribute;
                return attribute == null ? value.ToString() : attribute.Description;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return "Error: Neznámý stav";
            }

        }

    }
}
