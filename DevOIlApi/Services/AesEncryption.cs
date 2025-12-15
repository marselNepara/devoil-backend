// Services/AesEncryption.cs
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DevOIlApi.Services
{
    public static class AesEncryption
    {
        private static byte[] _key = null!;
        private static byte[] _iv = null!;

        public static void LoadConfiguration(IConfiguration configuration)
        {
            var key = configuration["Encryption:AesKey"];
            var iv = configuration["Encryption:AesIv"];

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv))
                throw new InvalidOperationException("Ключ шифрования не настроен в appsettings.json");

            _key = Encoding.UTF8.GetBytes(key);
            _iv = Encoding.UTF8.GetBytes(iv);

            // Проверка длины
            if (_key.Length != 32) throw new InvalidOperationException("AES Key должен быть 32 байта.");
            if (_iv.Length != 16) throw new InvalidOperationException("AES IV должен быть 16 байт.");
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                using var sw = new StreamWriter(cs);
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            if (!IsLikelyEncrypted(cipherText)) return cipherText; // Это "чистый" текст

            try
            {
                var buffer = Convert.FromBase64String(cipherText);
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;

                using var ms = new MemoryStream(buffer);
                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch
            {
                return "[ошибка расшифровки]";
            }
        }

        private static bool IsLikelyEncrypted(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 16) return false;
            if (input.Contains(" ")) return false;
            if (input.Length % 4 != 0) return false;

            try
            {
                Convert.FromBase64String(input);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}