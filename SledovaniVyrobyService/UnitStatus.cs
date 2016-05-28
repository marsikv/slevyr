using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SledovaniVyroby.SledovaniVyrobyService
{
    public class UnitStatus
    {
        //public short Address { get; set; }
        #region properties

        public bool SendError { get; set; }
        public string LastSendErrorDescription { get; set; }

        public short Ok { get; set; }
        public short Ng { get; set; }

        public DateTime Time { get; set; }
        public bool Handshake { get; set; }
        public DateTime OkNgTime { get; set; }
        public bool IsOkNg { get; set; }
        public float CasOk { get; set; }
        public DateTime CasOkTime { get; set; }
        public bool IsCasOk { get; set; }
        public float CasNg { get; set; }
        public DateTime CasNgTime { get; set; }
        public bool IsCasNg { get; set; }
        public short RozdilKusu { get; set; }
        public DateTime RozdilKusuTime { get; set; }
        public bool IsRozdilKusu { get; set; }
        public float Defektivita { get; set; }
        public DateTime DefektivitaTime { get; set; }
        public bool IsDefektivita { get; set; }

        public int CilTabule { get; set; }
        public float DefectTabule { get; set; }
        public int RozdilTabule { get; set; }


        //tyhle param by asi bylo lepsi do samostatneho DTO

        public int Cil1smeny { get; set; }
        public int Cil2smeny { get; set; }
        public int Cil3smeny { get; set; }
        
              
        public int CilDef1smeny { get; set; }  //uklada se v procentech * 10
        public int CilDef2smeny { get; set; }
        public int CilDef3smeny { get; set; }
               
        public int Prestav1smeny { get; set; }
        public int Prestav2smeny { get; set; }
        public int Prestav3smeny { get; set; }
        public char Smennost { get; set; }

        #endregion

        public UnitStatus()
        {
            //TODO docasne reseni jen pro testovani, aby bylo na zacatku v souladu se staven stranky index.html
            Cil1smeny = 1000;
            Cil2smeny = 2000;
            Cil3smeny = 3000;
            CilDef1smeny = 15;
            CilDef2smeny = 15;
            CilDef3smeny = 15;
            Prestav1smeny = 10;
            Prestav2smeny = 10;
            Prestav3smeny = 10;
        }

        #region methods

        /// <summary>
        /// zjistit jaka je prave ted smena a nastavit cil a defektivitu podle toho
        /// </summary>
        public void RecalcTabule()
        {

            //jen pro testovani. predpokladame smenu 24hod. zacinajici o pulnoci
            if (Cil1smeny < 0) return;
            int delkaSmeny = 24*60*60; //predpokladame ze smene trva 24hod  - prevedu na sec.
            double prumCasKus = delkaSmeny/Cil1smeny;
            int ocekavanyPocOk = (int) ((DateTime.Now).TimeOfDay.TotalSeconds / prumCasKus);


            CilTabule = Cil1smeny;
            DefectTabule = CilDef1smeny / 10;
            RozdilTabule = Ok - ocekavanyPocOk;
        }

        #endregion
    }
}
