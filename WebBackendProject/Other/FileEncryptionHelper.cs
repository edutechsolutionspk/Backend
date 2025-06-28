using System.Security.Cryptography;

namespace WebBackendProject.Other
{
    public class FileEncryptionHelper
    {
        public static string GenerateRandomKey()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key); // Convert to Base64 for easy storage
            }
        }

        // Function to generate a random IV (16 bytes)
        public static string GenerateRandomIV()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateIV();
                return Convert.ToBase64String(aes.IV);
            }
        }

        // Function to encrypt a file
       
        public static void EncryptFile(string inputFile, string outputFile, string key, string iv)
        {
            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(iv);

            byte[] fileBytes;

            // Read the file into memory first to prevent file locks
            using (var fileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, fileBytes.Length);
            }

            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(fileBytes, 0, fileBytes.Length);
                }
            }
        }

         // Function to decrypt a file
        public byte[] DecryptFile(string filePath, byte[] key, byte[] iv)
        {
            byte[] encryptedBytes = System.IO.File.ReadAllBytes(filePath);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                        cryptoStream.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        public Stream DecryptFileStream(string filePath, byte[] key, byte[] iv)
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            return cryptoStream; // Return the stream directly
        }

    }
}
