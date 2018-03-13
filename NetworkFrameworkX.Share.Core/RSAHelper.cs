using System;
using System.IO;
using System.Security.Cryptography;

namespace NetworkFrameworkX.Share
{
    internal class RSAKey
    {
        public string XmlKeys { get; set; }

        public string XmlPublicKey { get; set; }

        public RSAKey()
        {
        }

        public RSAKey(string xmlKeys, string xmlPublicKey)
        {
            this.XmlKeys = xmlKeys;
            this.XmlPublicKey = xmlPublicKey;
        }

        public static RSAKey Generate()
        {
            Generate(out string xmlKeys, out string xmlPublicKey);
            return new RSAKey(xmlKeys, xmlPublicKey);
        }

        public static void Generate(out string xmlKeys, out string xmlPublicKey)
        {
            try {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                xmlKeys = rsa.ToXmlString(true);
                xmlPublicKey = rsa.ToXmlString(false);
            } catch (Exception) {
                xmlKeys = null;
                xmlPublicKey = null;
            }
        }
    }

    internal static class RSAHelper
    {
        public static byte[] Encrypt(string inputData, RSAKey key) => Encrypt(inputData.GetBytes(), key);

        public static byte[] Encrypt(string inputData, string xmlPublicKey) => Encrypt(inputData.GetBytes(), xmlPublicKey);

        public static byte[] Encrypt(byte[] inputData, RSAKey key) => Encrypt(inputData, key.XmlPublicKey);

        public static byte[] Encrypt(byte[] inputData, string xmlPublicKey)
        {
            try {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(xmlPublicKey);
                int MaxBlockSize = rsa.KeySize / 8 - 11;    //加密块最大长度限制

                if (inputData.Length <= MaxBlockSize) { return rsa.Encrypt(inputData, false); }

                using (MemoryStream PlaiStream = new MemoryStream(inputData))
                using (MemoryStream CrypStream = new MemoryStream()) {
                    Byte[] Buffer = new Byte[MaxBlockSize];
                    int BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);

                    while (BlockSize > 0) {
                        Byte[] ToEncrypt = new Byte[BlockSize];
                        Array.Copy(Buffer, 0, ToEncrypt, 0, BlockSize);

                        Byte[] Cryptograph = rsa.Encrypt(ToEncrypt, false);
                        CrypStream.Write(Cryptograph, 0, Cryptograph.Length);

                        BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);
                    }

                    return CrypStream.ToArray();
                }
            } catch (Exception) {
                return null;
            }
        }

        public static byte[] Decrypt(string inputData, RSAKey key) => Decrypt(inputData.GetBytes(), key);

        public static byte[] Decrypt(string inputData, string xmlPrivateKey) => Decrypt(inputData.GetBytes(), xmlPrivateKey);

        public static byte[] Decrypt(byte[] inputData, RSAKey key) => Decrypt(inputData, key.XmlKeys);

        public static byte[] Decrypt(byte[] inputData, string xmlPrivateKey)
        {
            try {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(xmlPrivateKey);
                int MaxBlockSize = rsa.KeySize / 8; //解密块最大长度限制

                if (inputData.Length <= MaxBlockSize) { return rsa.Decrypt(inputData, false); }

                using (MemoryStream CrypStream = new MemoryStream(inputData))
                using (MemoryStream PlaiStream = new MemoryStream()) {
                    Byte[] Buffer = new Byte[MaxBlockSize];
                    int BlockSize = CrypStream.Read(Buffer, 0, MaxBlockSize);

                    while (BlockSize > 0) {
                        Byte[] ToDecrypt = new Byte[BlockSize];
                        Array.Copy(Buffer, 0, ToDecrypt, 0, BlockSize);

                        Byte[] Plaintext = rsa.Decrypt(ToDecrypt, false);
                        PlaiStream.Write(Plaintext, 0, Plaintext.Length);

                        BlockSize = CrypStream.Read(Buffer, 0, MaxBlockSize);
                    }

                    return PlaiStream.ToArray();
                }
            } catch (Exception) {
                return null;
            }
        }

        public static byte[] Signature(byte[] inputData, RSAKey key) => Signature(inputData, key.XmlKeys);

        public static byte[] Signature(byte[] inputData, string xmlPrivateKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(xmlPrivateKey);
            RSAPKCS1SignatureFormatter formatter = new RSAPKCS1SignatureFormatter(rsa);
            formatter.SetHashAlgorithm("MD5");
            byte[] outputData = formatter.CreateSignature(inputData);
            return outputData;
        }

        public static bool SignatureValidate(byte[] inputData, byte[] signatureData, RSAKey key) => SignatureValidate(inputData, signatureData, key.XmlPublicKey);

        public static bool SignatureValidate(byte[] inputData, byte[] signatureData, string xmlPublicKey)
        {
            try {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(xmlPublicKey);
                RSAPKCS1SignatureDeformatter deformatter = new RSAPKCS1SignatureDeformatter(rsa);
                deformatter.SetHashAlgorithm("MD5");

                return deformatter.VerifySignature(inputData, signatureData);
            } catch {
                return false;
            }
        }
    }
}