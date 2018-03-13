using System;
using System.IO;
using System.IO.Compression;

namespace NetworkFrameworkX.Share
{
    internal class GZip
    {
        /// <summary>
        /// 将传入的二进制字符串资料以GZip算法压缩
        /// </summary>
        /// <param name="text">原始未压缩字符串</param>
        /// <returns>经GZip压缩后的Base64字符串</returns>
        public static string CompressString(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length == 0) {
                return string.Empty;
            } else {
                byte[] buffer = Compress(text.GetBytes());
                return Convert.ToBase64String(buffer);
            }
        }

        /// <summary>
        /// 将传入的二进制字符串资料以GZip算法解压缩
        /// </summary>
        /// <param name="text">经GZip压缩后的二进制字符串</param>
        /// <returns>原始未压缩字符串</returns>
        public static string DecompressString(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length == 0) {
                return string.Empty;
            } else {
                byte[] buffer = Convert.FromBase64String(text);
                return Decompress(buffer).GetString();
            }
        }

        public static byte[] Compress(string text) => Compress(text.GetBytes());

        public static string DecompressString(byte[] rawData) => Decompress(rawData).GetString();

        /// <summary>
        /// GZip压缩
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] rawData)
        {
            MemoryStream ms = new MemoryStream();
            GZipStream gipstream = new GZipStream(ms, CompressionMode.Compress, true);
            gipstream.Write(rawData, 0, rawData.Length);
            gipstream.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// ZIP解压
        /// </summary>
        /// <param name="zippedData"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] zippedData)
        {
            MemoryStream ms = new MemoryStream(zippedData);
            GZipStream gzipstream = new GZipStream(ms, CompressionMode.Decompress);
            MemoryStream buffer = new MemoryStream();
            byte[] block = new byte[1024];
            while (true) {
                int bytesRead = gzipstream.Read(block, 0, block.Length);
                if (bytesRead > 0) {
                    buffer.Write(block, 0, bytesRead);
                } else {
                    break;
                }
            }
            gzipstream.Close();
            return buffer.ToArray();
        }
    }
}