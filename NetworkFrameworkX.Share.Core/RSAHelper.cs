using System;
using System.IO;
using System.Security.Cryptography;

namespace NetworkFrameworkX.Share
{
    internal class RSAKey
    {
        public byte[] Keys { get; set; }

        public byte[] PublicKey { get; set; }

        public RSAKey()
        {
        }

        public RSAKey(byte[] keys, byte[] publicKey)
        {
            this.Keys = keys;
            this.PublicKey = publicKey;
        }

        public void GeneratePublicKey() => this.PublicKey = GeneratePublicKey(this.Keys);

        public static byte[] GeneratePublicKey(byte[] keys)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.ImportCspBlob(keys);
                return rsa.ExportCspBlob(false);
            }
        }

        public static RSAKey Generate()
        {
            byte[] keys = null, publicKey = null;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                keys = rsa.ExportCspBlob(true);
                publicKey = rsa.ExportCspBlob(false);
                return new RSAKey(keys, publicKey);
            }
        }
    }

    internal static class RSAHelper
    {
        public static byte[] Encrypt(string inputData, RSAKey key) => Encrypt(inputData.GetBytes(), key);

        public static byte[] Encrypt(string inputData, byte[] publicKey) => Encrypt(inputData.GetBytes(), publicKey);

        public static byte[] Encrypt(byte[] inputData, RSAKey key) => Encrypt(inputData, key.PublicKey);

        public static byte[] Encrypt(byte[] inputData, byte[] publicKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.ImportCspBlob(publicKey);
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

        public static byte[] Decrypt(string inputData, byte[] privateKey) => Decrypt(inputData.GetBytes(), privateKey);

        public static byte[] Decrypt(byte[] inputData, RSAKey key) => Decrypt(inputData, key.Keys);

        public static byte[] Decrypt(byte[] inputData, byte[] privateKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.ImportCspBlob(privateKey);
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

        public static byte[] Signature(byte[] inputData, RSAKey key) => Signature(inputData, key.Keys);

        public static byte[] Signature(byte[] inputData, byte[] privateKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.ImportCspBlob(privateKey);
                RSAPKCS1SignatureFormatter formatter = new RSAPKCS1SignatureFormatter(rsa);
                formatter.SetHashAlgorithm("MD5");
                return formatter.CreateSignature(inputData);
            }
        }

        public static bool SignatureValidate(byte[] inputData, byte[] signatureData, RSAKey key) => SignatureValidate(inputData, signatureData, key.PublicKey);

        public static bool SignatureValidate(byte[] inputData, byte[] signatureData, byte[] publicKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.ImportCspBlob(publicKey);
                RSAPKCS1SignatureDeformatter deformatter = new RSAPKCS1SignatureDeformatter(rsa);
                deformatter.SetHashAlgorithm("MD5");
                return deformatter.VerifySignature(inputData, signatureData);
            }
        }
    }
}