using System.Security.Cryptography;

namespace NetworkFrameworkX.Share
{
    internal static class MD5
    {
        private static MD5CryptoServiceProvider crypto = new MD5CryptoServiceProvider();

        public static byte[] Encrypt(byte[] input) => crypto.ComputeHash(input);
    }

    internal static class SHA256
    {
        private static SHA256CryptoServiceProvider crypto = new SHA256CryptoServiceProvider();

        public static byte[] Encrypt(byte[] input) => crypto.ComputeHash(input);
    }
}