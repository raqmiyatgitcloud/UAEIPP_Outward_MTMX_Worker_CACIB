using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace Raqmiyat.Framework.Custom
{
    public static class SqlConManager
    {

        public static string GetConnectionString(string _connectionString, bool IsEncrypted)
        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new InvalidOperationException("DBConnection cannot be empty");
                }

                string encryptedConnectionString = _connectionString;
                bool isEncrypted = IsEncrypted;

                string decryptedConnectionString = isEncrypted ? Decrypt(encryptedConnectionString) : encryptedConnectionString;

                SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder(decryptedConnectionString);

                string dataSource = connectionStringBuilder.DataSource;
                string initialCatalog = connectionStringBuilder.InitialCatalog;
                string userId = connectionStringBuilder.UserID;
                string password = connectionStringBuilder.Password;
                string applicationName = connectionStringBuilder.ApplicationName;

                string modifiedConnectionString = $"Data Source={dataSource};Initial Catalog={initialCatalog};User ID={userId};password={password};Application Name={applicationName};MultipleActiveResultSets=True;";

                return modifiedConnectionString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error occurred in GetConnectionString(): {ex.Message}");
            }
        }
        public static string GetEntityConnectionString(string _connectionString, bool IsEncrypted)
        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new InvalidOperationException("DBConnection cannot be empty");
                }

                string encryptedConnectionString = _connectionString;
                bool isEncrypted = IsEncrypted;

                string decryptedConnectionString = isEncrypted ? Decrypt(encryptedConnectionString) : encryptedConnectionString;

                return decryptedConnectionString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error occurred in GetConnectionString(): {ex.Message}");
            }
        }
        public static string Encrypt(string toEncrypt)
        {
            try
            {
                var key = "RyCt4d6Wl85N4u2T";

                byte[] keyArray;
                byte[] toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);

                keyArray = SHA256.HashData(Encoding.UTF8.GetBytes(key));

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = keyArray;

                    aesAlg.Mode = CipherMode.ECB;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    ICryptoTransform cTransform = aesAlg.CreateEncryptor();
                    byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                    return Convert.ToBase64String(resultArray, 0, resultArray.Length);
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string Decrypt(string toDecrypt)
        {
            try
            {
                var key = "RyCt4d6Wl85N4u2T";

                byte[] keyArray;
                byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

                keyArray = SHA256.HashData(Encoding.UTF8.GetBytes(key));

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = keyArray;

                    aesAlg.Mode = CipherMode.ECB;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    ICryptoTransform cTransform = aesAlg.CreateDecryptor();
                    byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                    return Encoding.UTF8.GetString(resultArray);
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
