using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slevyr.DataAccess.Model
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

        public int CilKusuTabule { get; set; }
        public float CilDefectTabule { get; set; }
        public float AktualDefectTabule { get; set; }
        public int RozdilTabule { get; set; }


        #endregion

        public UnitStatus()
        {
        }

        #region methods

        /// <summary>
        /// zjistit jaka je prave ted smena a nastavit cil a defektivitu podle toho
        /// </summary>
        public void RecalcTabule(UnitConfig unitConfig)
        {

            DateTime dateTimeNow = DateTime.Now;

            //pocet sekund od zacátku dne
            //TODO overit jestli takto lze ziskat
            int secFromMidn = dateTimeNow.Second + dateTimeNow.Minute * 60 + dateTimeNow.Hour * 3600;

            //pocet sekund celého dne
            int allDaySec = 24*60*60;

            //pocet sec. prestavky, nyni prestavka 30min - udelat jako parametr ?
            int prestavkaSec = 30*60;

            int prestavka = (30/(24*60));
            int str2Hod = (120/(24*60));

            //?
            //double partOfTheDay = sec/allDaySec;

            int zacatekSmeny1 = (int)unitConfig.Zacatek1SmenyTime.TotalSeconds;
            int zacatekSmeny2 = (int)unitConfig.Zacatek2SmenyTime.TotalSeconds;
            int zacatekSmeny3 = (int)unitConfig.Zacatek3SmenyTime.TotalSeconds;

            int zacatekPrestavkySmeny1 = (int)unitConfig.Prestavka1SmenyTime.TotalSeconds;
            int zacatekPrestavkySmeny2 = (int)unitConfig.Prestavka2SmenyTime.TotalSeconds;
            int zacatekPrestavkySmeny3 = (int)unitConfig.Prestavka3SmenyTime.TotalSeconds;

            //výpoèet sekund celé smìny -30 minut na pøestávku * poèet sekund celého dne
            int delkaSmenySec = zacatekPrestavkySmeny2 - zacatekPrestavkySmeny1 - prestavkaSec;

            int odZacatkuSmenySec = 0;

            //TODO overit u P.
            if (secFromMidn < zacatekPrestavkySmeny3)
            {
                odZacatkuSmenySec = secFromMidn + 2*3600;  //Pøed pøestávkou na tøetí smìnì od pùlnoci + 2hodiny ze starého dne
                CilKusuTabule = unitConfig.Cil3Smeny;
                CilDefectTabule = unitConfig.Def3Smeny / 10;
                
            }
            else if (secFromMidn >= zacatekPrestavkySmeny3 && secFromMidn < zacatekSmeny1)
            {
                odZacatkuSmenySec = secFromMidn - prestavkaSec  + 2 * 3600;  //Pøed pøestávkou na tøetí smìnì od pùlnoci po pøestávce + 2hod ze starého dne
                CilKusuTabule = unitConfig.Cil3Smeny;
                CilDefectTabule = unitConfig.Def3Smeny / 10;
            }
            else if (secFromMidn >= zacatekSmeny1 && secFromMidn < zacatekPrestavkySmeny1)
            {
                odZacatkuSmenySec = secFromMidn - zacatekSmeny1;  // Pøed pøestávkou na první smìnì
                CilKusuTabule = unitConfig.Cil1Smeny;
                CilDefectTabule = unitConfig.Def1Smeny / 10;
            }
            else if (secFromMidn >= zacatekPrestavkySmeny1 && secFromMidn < zacatekSmeny2)
            {
                odZacatkuSmenySec = secFromMidn - zacatekSmeny1 - prestavkaSec;  // Po pøestávce na první smìnì
                CilKusuTabule = unitConfig.Cil1Smeny;
                CilDefectTabule = unitConfig.Def1Smeny / 10;
            }
            else if (secFromMidn >= zacatekSmeny2 && secFromMidn < zacatekPrestavkySmeny2)
            {
                odZacatkuSmenySec = secFromMidn - zacatekSmeny2;  // Pøed pøestávkou na druhé smìnì
                CilKusuTabule = unitConfig.Cil2Smeny;
                CilDefectTabule = unitConfig.Def2Smeny / 10;
            }
            else if (secFromMidn >= zacatekPrestavkySmeny2 && secFromMidn < zacatekSmeny3)
            {
                odZacatkuSmenySec = secFromMidn - zacatekSmeny2 - prestavkaSec;  // Po pøestávce na druhe smìnì
                CilKusuTabule = unitConfig.Cil2Smeny;
                CilDefectTabule = unitConfig.Def2Smeny / 10;
            }
            else if (secFromMidn >= zacatekSmeny3 && secFromMidn < (24*3600 - 1))
            {
                odZacatkuSmenySec = secFromMidn - zacatekSmeny3; // Pøed pøestávkou na tøetí smìnì do pùlnoci
                CilKusuTabule = unitConfig.Cil3Smeny;
                CilDefectTabule = unitConfig.Def3Smeny/10;
            }
            else
            {
                //  strOdZacatkuSmeny = (strPartOfTheDay) * 86400 + str2Hod * 86400                 '  Pøed pøestávkou na tøetí smìnì od pùlnoci + 2hodiny ze starého dne
            }

            //Pocet sekund na 1 kus
            //var prumCasKus = delkaSmenySec / unitConfig.Cil1Smeny;
            //TODO overit u Patrika
            var prumCasKus = delkaSmenySec / CilKusuTabule;

            var ocekavanyPocOk = (int) (odZacatkuSmenySec / prumCasKus);

            RozdilTabule = Ok - ocekavanyPocOk;

            AktualDefectTabule = (float)Ng / (float)Ok;
        }

        #endregion
    }
}
