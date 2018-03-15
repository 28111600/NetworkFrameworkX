using System.Security.Cryptography;

namespace NetworkFrameworkX.Share.Core
{
    internal class MD5
    {
        public static byte[] Encrypt(byte[] input)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(input);
        }
    }
}