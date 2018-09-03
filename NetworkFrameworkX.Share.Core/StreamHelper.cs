using System;
using System.IO;

namespace NetworkFrameworkX.Share
{
    internal class EndReadEventArgs : EventArgs
    {
        public byte[] Data { get; private set; }

        public EndReadEventArgs(byte[] data)
        {
            this.Data = data;
        }
    }

    /// <summary>
    /// Stream写入/读取帮助类，处理半包/粘包问题
    /// </summary>
    internal class StreamHelper
    {
        /*
        00 00 00 05 | 01 02 03 04 05 | 00 00 00 02 | 06 07
        pocket head | pocket body    | pocket head | pocket body
        */

        private const int SIZE_OF_BYTE = 256;
        private const int LENGTH_OF_HEAD = 4;
        private const int SIZE_OF_BUFFER = SIZE_OF_BYTE;
        private const int MAX_SIZE_OF_PACKET = SIZE_OF_BYTE * SIZE_OF_BYTE * SIZE_OF_BYTE; // 16 MByte

        private const string ERR_HEAP_CORRUPTION = "heap corruption";

        private byte[] bufferOfPacket = null;
        private int lengthOfPacket = 0;
        private int indexOfPacket = 0;


        private Stream stream = null;

        public int MaxSizeOfPacket { get; set; } = MAX_SIZE_OF_PACKET;

        public StreamHelper(Stream stream)
        {
            this.stream = stream;
        }

        public int Write(byte[] data)
        {
            int length = data.Length;
            if (length > this.MaxSizeOfPacket) {
                throw new Exception(ERR_HEAP_CORRUPTION);
            }

            byte[] head = BitConverter.GetBytes(length);
            byte[] buffer = new byte[LENGTH_OF_HEAD + length];

            Buffer.BlockCopy(head, 0, buffer, 0, LENGTH_OF_HEAD);
            Buffer.BlockCopy(data, 0, buffer, LENGTH_OF_HEAD, length);

            this.stream.Write(buffer, 0, buffer.Length);
            return buffer.Length;
        }

        /// <summary>
        /// 读取到完整的包时触发此事件
        /// </summary>
        public event EventHandler<EndReadEventArgs> EndRead;

        public void Read()
        {
            byte[] buffer = new byte[SIZE_OF_BUFFER];
            int readBufferLength = this.stream.Read(buffer, 0, SIZE_OF_BUFFER);
            if (readBufferLength > 0) {
                byte[] data = buffer.Take(readBufferLength);

                while (data != null && data.Length > 0) {
                    if (this.lengthOfPacket == 0) {
                        // 从头开始读取
                        this.lengthOfPacket = BitConverter.ToInt32(data.Take(LENGTH_OF_HEAD), 0);

                        if (this.lengthOfPacket > this.MaxSizeOfPacket || this.lengthOfPacket <= 0) {
                            // 非法长度，抛出异常
                            throw new Exception(ERR_HEAP_CORRUPTION);
                        }

                        this.indexOfPacket = 0;
                        this.bufferOfPacket = new byte[this.lengthOfPacket];
                        data = data.Skip(LENGTH_OF_HEAD);
                    }

                    if (this.indexOfPacket < this.lengthOfPacket) {
                        // 半包
                        int length = data.Length;
                        if (length + this.indexOfPacket < this.lengthOfPacket) {
                            // 包不完整
                            Buffer.BlockCopy(data, 0, this.bufferOfPacket, this.indexOfPacket, length);
                            this.indexOfPacket += length;
                            data = null;
                        } else if (length + this.indexOfPacket >= this.lengthOfPacket) {
                            // 包完整，可能粘包
                            int lengthNeed = this.lengthOfPacket - this.indexOfPacket;
                            Buffer.BlockCopy(data, 0, this.bufferOfPacket, this.indexOfPacket, lengthNeed);
                            data = data.Skip(lengthNeed);
                            this.lengthOfPacket = this.indexOfPacket = 0;
                            this.EndRead?.Invoke(this, new EndReadEventArgs(this.bufferOfPacket));
                        }
                    }
                }
            }
        }
    }
}