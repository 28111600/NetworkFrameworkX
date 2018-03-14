using System.IO;
using System.IO.Compression;

namespace NetworkFrameworkX.Share
{
    internal class GZip
    {
        /// <summary>
        /// GZip压缩
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] rawData)
        {
            using (MemoryStream ms = new MemoryStream()) {
                using (GZipStream gzipstream = new GZipStream(ms, CompressionMode.Compress, true)) {
                    gzipstream.Write(rawData, 0, rawData.Length);
                    gzipstream.Close();
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// ZIP解压
        /// </summary>
        /// <param name="zippedData"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] zippedData)
        {
            using (MemoryStream ms = new MemoryStream(zippedData)) {
                using (GZipStream gzipstream = new GZipStream(ms, CompressionMode.Decompress)) {
                    using (MemoryStream buffer = new MemoryStream()) {
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
        }
    }
}