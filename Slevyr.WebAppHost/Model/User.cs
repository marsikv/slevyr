using System;
using System.Security.Cryptography;
using System.Text;

namespace Slevyr.WebAppHost.Model
{
    /// <summary>
    /// Uzivatel ktery je pouzivany pro autentizaci a autorizaci
    /// TODO pouzit odpovidajici ASP.NET tridu (Principal ?)
    /// </summary>
    public class User
    {
        #region private fields

        private readonly string Pswdhash;

        private static readonly MD5CryptoServiceProvider Md5Provider = new MD5CryptoServiceProvider();

        #endregion


        #region properties

        public string Name { get; set; }

        public bool NastaveniEnabled { get; set; }

        public bool ExportEnabled { get; set; }

        public bool TabuleEnabled { get; set; }

        public bool PrehledUdrzbyEnabled { get; set; }

        #endregion


        /// <summary>
        /// konstruktor z dvojice Name-pswd md5 hash
        /// </summary>
        /// <param Name="configTuple"></param>
        public User(string configTuple)
        {
            var s = configTuple.Split('-');
            if (s.Length == 2)
            {
                Name = s[0];
                Pswdhash = s[1];
                SetBasicRole();
            }
            else
            {
                throw new ArgumentException("Spatna forma zadani uzivatele");
            }
        }

        private bool PasswordIsValid(string pswd)
        {
            return Pswdhash.Equals(MD5Hash(pswd));
        }

        public void SetBasicRole()
        {
            ExportEnabled = true;
            NastaveniEnabled = false;
            PrehledUdrzbyEnabled = true;
            TabuleEnabled = true;
        }

        public void SetAdminRole()
        {
            ExportEnabled = true;
            NastaveniEnabled = true;
            PrehledUdrzbyEnabled = true;
            TabuleEnabled = true;
        }

        public void SetMaintenanceRole()
        {
            ExportEnabled = false;
            NastaveniEnabled = false;
            PrehledUdrzbyEnabled = true;
            TabuleEnabled = false;
        }

        public string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            byte[] bytes = Md5Provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        /// <summary>
        /// Autentizuje uživatele, ovìøí zda odpovída jmeno uživatele i jeho heslo
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool IsAutentized(string username, string password)
        {
            return Name.Equals(username, StringComparison.InvariantCultureIgnoreCase) && PasswordIsValid(password);
        }

        public bool NameMatch(string username)
        {
            return Name.Equals(username, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}