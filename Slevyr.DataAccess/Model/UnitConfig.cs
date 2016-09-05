using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.Model
{
    public class UnitConfig
    {
        public byte Addr;
        public string UnitName;
        public string TypSmennosti = "A";  //stacil by char
        public int Cil1Smeny;
        public int Cil2Smeny;
        public int Cil3Smeny;
        public float Def1Smeny;
        public float Def2Smeny;
        public float Def3Smeny;
        public string Prestavka1Smeny;
        public string Prestavka2Smeny;
        public string Prestavka3Smeny;
        public string Zacatek1Smeny;
        public string Zacatek2Smeny;
        public string Zacatek3Smeny;
        public bool WriteProtectEEprom;
        public byte MinOK;
        public byte MinNG;
        public bool BootloaderOn;
        public byte ParovanyLED;
        public byte RozliseniCidel;
        public byte PracovniJasLed;

        public TimeSpan Prestavka1SmenyTime => string.IsNullOrWhiteSpace(Prestavka1Smeny) ? TimeSpan.Zero : TimeSpan.Parse(Prestavka1Smeny);
        public TimeSpan Prestavka2SmenyTime => string.IsNullOrWhiteSpace(Prestavka2Smeny) ? TimeSpan.Zero : TimeSpan.Parse(Prestavka2Smeny);
        public TimeSpan Prestavka3SmenyTime => string.IsNullOrWhiteSpace(Prestavka3Smeny) ? TimeSpan.Zero : TimeSpan.Parse(Prestavka3Smeny);

        public TimeSpan Zacatek1SmenyTime => string.IsNullOrWhiteSpace(Zacatek1Smeny) ? TimeSpan.Zero : TimeSpan.Parse(Zacatek1Smeny);
        public TimeSpan Zacatek2SmenyTime => string.IsNullOrWhiteSpace(Zacatek2Smeny) ? TimeSpan.Zero : TimeSpan.Parse(Zacatek2Smeny);
        public TimeSpan Zacatek3SmenyTime => string.IsNullOrWhiteSpace(Zacatek3Smeny) ? TimeSpan.Zero : TimeSpan.Parse(Zacatek3Smeny);


        public void SaveToFile(string dataFilePath)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            Directory.CreateDirectory(dataFilePath);

            using (StreamWriter sw = new StreamWriter(Path.Combine(dataFilePath, $"unitCfg_{Addr}.json")))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this);
            }
        }

        public void LoadFromFile(byte addr, string dataFilePath)
        {
            var fileName = Path.Combine(dataFilePath, $"unitCfg_{addr}.json");

            if (!File.Exists(fileName)) return;

            using (StreamReader file = File.OpenText(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                var res = (UnitConfig)serializer.Deserialize(file, typeof(UnitConfig));

                if (res == null) return;

                Addr = res.Addr;
                UnitName = res.UnitName;
                TypSmennosti = res.TypSmennosti;
                Cil1Smeny = res.Cil1Smeny;
                Cil2Smeny = res.Cil2Smeny;
                Cil3Smeny = res.Cil3Smeny;
                Def1Smeny = res.Def1Smeny;
                Def2Smeny = res.Def2Smeny;
                Def3Smeny = res.Def3Smeny;
                Prestavka1Smeny = res.Prestavka1Smeny;
                Prestavka2Smeny = res.Prestavka2Smeny;
                Prestavka3Smeny = res.Prestavka3Smeny;
                Zacatek1Smeny = res.Zacatek1Smeny;
                Zacatek2Smeny = res.Zacatek2Smeny;
                Zacatek3Smeny = res.Zacatek3Smeny;
                WriteProtectEEprom = res.WriteProtectEEprom;
                MinOK = res.MinOK;
                MinNG = res.MinNG;
                BootloaderOn = res.BootloaderOn;
                ParovanyLED = res.ParovanyLED;
                RozliseniCidel = res.RozliseniCidel;
                PracovniJasLed = res.PracovniJasLed;

            }
        }
    }
}
