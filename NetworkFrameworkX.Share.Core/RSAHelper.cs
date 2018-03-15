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

        public void GeneratePublicKey() => this.XmlPublicKey = GeneratePublicKey(this.XmlKeys);

        public static string GeneratePublicKey(string xmlKeys)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.FromXmlString(xmlKeys);
                return rsa.ToXmlString(false);
            }
        }

        public static RSAKey Generate()
        {
            string xmlKeys = null, xmlPublicKey = null;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                xmlKeys = rsa.ToXmlString(true);
                xmlPublicKey = rsa.ToXmlString(false);
                return new RSAKey(xmlKeys, xmlPublicKey);
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
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.FromXmlString(xmlPublicKey);
                int maxBlockSize = rsa.KeySize / 8 - 11;  //加密块最大长度限制

                if (inputData.Length <= maxBlockSize) { return rsa.Encrypt(inputData, false); }

                using (MemoryStream plaiStream = new MemoryStream(inputData)) {
                    using (MemoryStream crypStream = new MemoryStream()) {
                        byte[] buffer = new Byte[maxBlockSize];
                        int blockSize = plaiStream.Read(buffer, 0, maxBlockSize);

                        while (blockSize > 0) {
                            byte[] toEncrypt = new Byte[blockSize];
                            Buffer.BlockCopy(buffer, 0, toEncrypt, 0, blockSize);
                            byte[] cryptograph = rsa.Encrypt(toEncrypt, false);
                            crypStream.Write(cryptograph, 0, cryptograph.Length);

                            blockSize = plaiStream.Read(buffer, 0, maxBlockSize);
                        }

                        return crypStream.ToArray();
                    }
                }
            }
        }

        public static byte[] Decrypt(string inputData, RSAKey key) => Decrypt(inputData.GetBytes(), key);

        public static byte[] Decrypt(string inputData, string xmlPrivateKey) => Decrypt(inputData.GetBytes(), xmlPrivateKey);

        public static byte[] Decrypt(byte[] inputData, RSAKey key) => Decrypt(inputData, key.XmlKeys);

        public static byte[] Decrypt(byte[] inputData, string xmlPrivateKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.FromXmlString(xmlPrivateKey);
                int maxBlockSize = rsa.KeySize / 8;  //解密块最大长度限制

                if (inputData.Length <= maxBlockSize) { return rsa.Decrypt(inputData, false); }

                using (MemoryStream crypStream = new MemoryStream(inputData)) {
                    using (MemoryStream plaiStream = new MemoryStream()) {
                        byte[] buffer = new Byte[maxBlockSize];
                        int blockSize = crypStream.Read(buffer, 0, maxBlockSize);

                        while (blockSize > 0) {
                            byte[] toDecrypt = new Byte[blockSize];
                            Buffer.BlockCopy(buffer, 0, toDecrypt, 0, blockSize);

                            byte[] plaintext = rsa.Decrypt(toDecrypt, false);
                            plaiStream.Write(plaintext, 0, plaintext.Length);

                            blockSize = crypStream.Read(buffer, 0, maxBlockSize);
                        }

                        return plaiStream.ToArray();
                    }
                }
            }
        }

        public static byte[] Signature(byte[] inputData, RSAKey key) => Signature(inputData, key.XmlKeys);

        public static byte[] Signature(byte[] inputData, string xmlPrivateKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.FromXmlString(xmlPrivateKey);
                RSAPKCS1SignatureFormatter formatter = new RSAPKCS1SignatureFormatter(rsa);
                formatter.SetHashAlgorithm("MD5");
                return formatter.CreateSignature(inputData);
            }
        }

        public static bool SignatureValidate(byte[] inputData, byte[] signatureData, RSAKey key) => SignatureValidate(inputData, signatureData, key.XmlPublicKey);

        public static bool SignatureValidate(byte[] inputData, byte[] signatureData, string xmlPublicKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.FromXmlString(xmlPublicKey);
                RSAPKCS1SignatureDeformatter deformatter = new RSAPKCS1SignatureDeformatter(rsa);
                deformatter.SetHashAlgorithm("MD5");
                return deformatter.VerifySignature(inputData, signatureData);
            }
        }
    }
}