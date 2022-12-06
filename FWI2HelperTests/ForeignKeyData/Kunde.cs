using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace FWI2HelperTests.ForeignKeyData
{
    public class Kunde : ModelBase<Kunde, DBKunde>
    {
        public string Name { get; set; } = String.Empty;

        public string Vorname { get; set; } = String.Empty;

        public string EMail { get; set; } = String.Empty;

        public string PasswortHash { get; set; } = string.Empty;

        public void SetPasswordFromSecureString(SecureString str)
        {
            string passwd = str.ToString() ?? String.Empty;
            this.PasswortHash = ComputeSha256Hash(passwd);
        }

        public void SetPassword(string passwd)
        {
            this.PasswortHash = ComputeSha256Hash(passwd);
        }

        public static Kunde? CheckLogin(string login, string password)
        {
            string passwordHash = Kunde.ComputeSha256Hash(password);

            return (from kunde in DBKunde.Instance.ReadAll()
                    where kunde.EMail == login && kunde.PasswortHash == passwordHash
                    select kunde).FirstOrDefault();
        }

        private static string ComputeSha256Hash(string rawData)
        {
            // TODO: Move to FWI2Helper
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public override string ToString()
        {
            return $"{this.Name}, {this.Vorname} ({this.EMail})";
        }
    }
}