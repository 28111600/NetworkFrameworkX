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
            Generate(out byte[] key, out byte[] iv);
            return new AESKey(key, iv);
        }

        public static void Generate(out byte[] key, out byte[] iv)
        {
            try {
                RijndaelManaged AES = new RijndaelManaged();
                AES.GenerateKey();
                AES.GenerateIV();
                key = AES.Key;
                iv = AES.IV;
            } catch (Exception) {
                key = null;
                iv = null;
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
            RijndaelManaged AES = new RijndaelManaged();

            ICryptoTransform transform = AES.CreateEncryptor(key, iv);
            byte[] outputData = transform.TransformFinalBlock(inputData, 0, inputData.Length);

            return outputData;
        }

        public static byte[] Decrypt(string inputData, AESKey key) => Decrypt(inputData.GetBytes(), key);

        public static byte[] Decrypt(string inputData, byte[] key, byte[] iv) => Decrypt(inputData.GetBytes(), key, iv);

        public static byte[] Decrypt(byte[] inputData, AESKey key) => Decrypt(inputData, key.Key, key.IV);

        public static byte[] Decrypt(byte[] inputData, byte[] key, byte[] iv)
        {
            RijndaelManaged AES = new RijndaelManaged();

            ICryptoTransform transform = AES.CreateDecryptor(key, iv);
            byte[] outputData = transform.TransformFinalBlock(inputData, 0, inputData.Length);

            return outputData;
        }
    }
}