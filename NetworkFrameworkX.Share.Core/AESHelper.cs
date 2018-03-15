using System;
using System.Security.Cryptography;

namespace NetworkFrameworkX.Share
{
    [Serializable]
    internal class AESKey
    {
        public byte[] Key { get; set; }

        public byte[] IV { get; set; }

        public AESKey()
        {
        }

        public AESKey(byte[] key, byte[] iv)
        {
            this.Key = key;
            this.IV = iv;
        }

        public static AESKey Generate()
        {
            byte[] key = null, iv = null;
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider()) {
                aes.KeySize = 128;
                aes.GenerateKey();
                aes.GenerateIV();
                key = aes.Key;
                iv = aes.IV;
                return new AESKey(key, iv);
            }
        }
    }

    internal class AESHelper
    {
        public static byte[] Encrypt(string inputData, AESKey key) => Encrypt(inputData.GetBytes(), key);

        public static byte[] Encrypt(string inputData, byte[] key, byte[] iv) => Encrypt(inputData.GetBytes(), key, iv);

        public static byte[] Encrypt(byte[] inputData, AESKey key) => Encrypt(inputData, key.Key, key.IV);

        public static byte[] Encrypt(byte[] inputData, byte[] key, byte[] iv)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider()) {
                ICryptoTransform transform = aes.CreateEncryptor(key, iv);
                return transform.TransformFinalBlock(inputData, 0, inputData.Length);
            }
        }

        public static byte[] Decrypt(string inputData, AESKey key) => Decrypt(inputData.GetBytes(), key);

        public static byte[] Decrypt(string inputData, byte[] key, byte[] iv) => Decrypt(inputData.GetBytes(), key, iv);

        public static byte[] Decrypt(byte[] inputData, AESKey key) => Decrypt(inputData, key.Key, key.IV);

        public static byte[] Decrypt(byte[] inputData, byte[] key, byte[] iv)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider()) {
                ICryptoTransform transform = aes.CreateDecryptor(key, iv);
                return transform.TransformFinalBlock(inputData, 0, inputData.Length);
            }
        }
    }
}