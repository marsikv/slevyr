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
        public string Name;
        private readonly string Pswdhash;

        private static readonly MD5CryptoServiceProvider Md5Provider = new MD5CryptoServiceProvider();

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
        /// Ovìøí zda odpovída jmeno uživatele i jeho heslo
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool IsValid(string username, string password)
        {
            return Name.Equals(username, StringComparison.InvariantCultureIgnoreCase) && PasswordIsValid(password);
        }
    }
}