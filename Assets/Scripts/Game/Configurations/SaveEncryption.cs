using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Configurations
{
    public static class SaveEncryption
    {
        // Must be 32 bytes for AES-256.
        private static byte[] GetKey()
        {
            string partA = "Pirate";
            string partB = "Bay";
            string partC = Application.identifier;
            string combined = partA + partC + partB + "SaveKey";

            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        }

        public static void SaveEncryptedJson(string path, string json)
        {
            byte[] iv = GenerateRandomBytes(16);

            using Aes aes = Aes.Create();
            aes.Key = GetKey();
            aes.IV = iv;

            using MemoryStream memoryStream = new MemoryStream();

            // Store IV at the beginning of the file.
            memoryStream.Write(iv, 0, iv.Length);

            using (CryptoStream cryptoStream = new CryptoStream(
                       memoryStream,
                       aes.CreateEncryptor(),
                       CryptoStreamMode.Write))
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                cryptoStream.Write(jsonBytes, 0, jsonBytes.Length);
            }

            File.WriteAllBytes(path, memoryStream.ToArray());
        }

        public static string LoadEncryptedJson(string path)
        {
            byte[] fileBytes = File.ReadAllBytes(path);

            byte[] iv = new byte[16];
            Array.Copy(fileBytes, 0, iv, 0, iv.Length);

            byte[] encryptedBytes = new byte[fileBytes.Length - iv.Length];
            Array.Copy(fileBytes, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            using Aes aes = Aes.Create();
            aes.Key = GetKey();
            aes.IV = iv;

            using MemoryStream encryptedStream = new MemoryStream(encryptedBytes);
            using CryptoStream cryptoStream = new CryptoStream(
                encryptedStream,
                aes.CreateDecryptor(),
                CryptoStreamMode.Read);
            using StreamReader reader = new StreamReader(cryptoStream);

            return reader.ReadToEnd();
        }
        
        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return bytes;
        }
    }
}