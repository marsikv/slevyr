using System;

namespace SledovaniVyrobyWebAppHost.Model
{
    public class UnitConfig
    {
        public char TypSmennosti = 'A';
        public short Cil1Smeny = 1000;
        public short Cil2Smeny = 2000;
        public short Cil3Smeny = 3000;
        public float Def1Smeny = 1.5f;
        public float Def2Smeny = 1.5f;
        public float Def3Smeny = 1.5f;
        public TimeSpan Prestavka1Smeny = new TimeSpan(10,30,0);
        public TimeSpan Prestavka2Smeny = new TimeSpan(16, 0, 0);
        public TimeSpan Prestavka3Smeny = new TimeSpan(22, 30, 0);
        public byte addr = 100;
        public bool writeProtectEEprom = false;
        public byte minOK = 5;
        public byte minNG = 5;
        public bool bootloaderOn = false;
        public byte parovanyLED;
        public byte rozliseniCidel;
        public byte pracovniJasLed;
    }
}
