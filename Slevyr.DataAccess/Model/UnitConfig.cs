using System;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitConfig
    {
        private DateTime _prestavka1Smeny = DateTime.Now.ToUniversalTime();
        private DateTime _prestavka2Smeny = DateTime.Now.ToUniversalTime();
        private DateTime _prestavka3Smeny = DateTime.Now.ToUniversalTime();

        public byte Addr = 100;
        public string UnitName;
        public string TypSmennosti = "A";  //stacil by char
        public short Cil1Smeny = 1000;
        public short Cil2Smeny = 2000;
        public short Cil3Smeny = 3000;
        public float Def1Smeny = 1.5f;
        public float Def2Smeny = 1.5f;
        public float Def3Smeny = 1.5f;
        public bool WriteProtectEEprom = false;
        public byte MinOK = 5;
        public byte MinNG = 5;
        public bool BootloaderOn = false;
        public byte ParovanyLED;
        public byte RozliseniCidel;
        public byte PracovniJasLed;

        public string Prestavka1SmenyW3
        {
            get { return ToRFC3339(_prestavka1Smeny); }
            set { _prestavka1Smeny = FromRFC3339(value); }
        }

        public string Prestavka2SmenyW3
        {
            get { return ToRFC3339(_prestavka2Smeny); }
            set { _prestavka2Smeny = FromRFC3339(value); }
        }

        public string Prestavka3SmenyW3
        {
            get { return ToRFC3339(_prestavka3Smeny); }
            set { _prestavka3Smeny = FromRFC3339(value); }
        }


        private DateTime FromRFC3339(string ds)
        {
            return Rfc3339DateTime.Parse(ds);
        }

        

        private string ToRFC3339(DateTime dt)
        {
            return Rfc3339DateTime.ToString(dt);
        }
    }
}
