using System.IO;
using System.IO.Compression;

namespace NetworkFrameworkX.Share
{
    internal class GZip
    {
        private const int SIZE_OF_BLOCK = 1024;

        /// <summary>
        /// GZip压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream()) {
                using (GZipStream gzipstream = new GZipStream(ms, CompressionMode.Compress)) {
                    using (MemoryStream buffer = new MemoryStream(data)) {
                        buffer.CopyTo(gzipstream);
                    }
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// ZIP解压
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data)) {
                using (GZipStream gzipstream = new GZipStream(ms, CompressionMode.Decompress)) {
                    using (MemoryStream buffer = new MemoryStream()) {
                        gzipstream.CopyTo(buffer);
                        return buffer.ToArray();
                    }
                }
            }
        }
    }
}