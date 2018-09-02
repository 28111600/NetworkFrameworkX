using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkFrameworkX.Share
{
    internal class TcpClient
    {
        public class ClientEventArgs : EventArgs
        {
            public ClientEventArgs()
            {
            }
        }

        public class StatusChangeEventArgs : EventArgs
        {
            public ConnectState ConnectState { get; private set; }

            public StatusChangeEventArgs(ConnectState connectState)
            {
                this.ConnectState = connectState;
            }
        }

        public class ReceiveEventArgs : EventArgs
        {
            public byte[] Data { get; private set; }

            public int Length { get; private set; }

            public ReceiveEventArgs(byte[] data, int length)
            {
                this.Data = data;
                this.Length = length;
            }
        }

        public enum ConnectState
        {
            Start = 0,
            Listening = 1,
            Connected = 2,
            Close = 3
        }

        public event EventHandler<ReceiveEventArgs> OnReceive;

        public event EventHandler<StatusChangeEventArgs> OnStateChange;

        public event EventHandler<ClientEventArgs> OnStart;

        public event EventHandler<ClientEventArgs> OnClose;

        private Thread T { get; set; }

        private StreamHelper stream = null;

        private System.Net.Sockets.TcpClient TCPClient { get; set; }

        public IPEndPoint LocalAddress => this.TCPClient.Client.LocalEndPoint as IPEndPoint;

        public IPEndPoint RemoteAddress => this.TCPClient.Client.RemoteEndPoint as IPEndPoint;

        public bool IsConnected
        {
            get {
                if (this.TCPClient == null) {
                    return false;
                } else {
                    try {
                        if (this.TCPClient.Client.Poll(20, SelectMode.SelectRead) && this.TCPClient.Client.Available == 0) {
                            return false;
                        }
                    } catch {
                        return false;
                    }
                }
                return true;
            }
        }

        public void Close()
        {
            this.OnStateChange?.Invoke(this, new StatusChangeEventArgs(ConnectState.Close));
            this.OnClose?.Invoke(this, new ClientEventArgs());
            this.TCPClient?.Close();
        }

        private void StartThreading()
        {
            try {
                while (this.IsConnected) {
                    this.stream.Read((x) => this.OnReceive?.Invoke(this, new ReceiveEventArgs(x, x.Length)));
                }
            } catch {
                this.Close();
            } finally {
                this.OnStateChange?.Invoke(this, new StatusChangeEventArgs(ConnectState.Close));
                this.Close();
            }
        }

        public void Connect(string host, int port)
        {
            this.TCPClient.Connect(host, port);
        }

        public void Connect(IPEndPoint remoteEP)
        {
            this.TCPClient.Connect(remoteEP);
        }

        public int Start()
        {
            this.stream = new StreamHelper(this.TCPClient.GetStream());

            this.OnStart?.Invoke(this, new ClientEventArgs());
            this.OnStateChange?.Invoke(this, new StatusChangeEventArgs(ConnectState.Connected));
            ThreadStart ts = new ThreadStart(this.StartThreading);
            if (this.T != null && this.T.IsAlive) {
                this.T.Abort();
            }
            this.T = new Thread(ts);
            this.T.Start();
            return this.LocalAddress.Port;
        }

        public TcpClient(System.Net.Sockets.TcpClient tcpClient)
        {
            this.TCPClient = tcpClient;
        }

        public TcpClient()
        {
            this.TCPClient = new System.Net.Sockets.TcpClient(new IPEndPoint(IPAddress.Any, 0));
        }

        public int Send(string text) => this.Send(Encoding.UTF8.GetBytes(text));

        public int Send(byte[] data)
        {
            if (this.IsConnected) {
                return this.stream.Write(data);
            } else {
                return 0;
            }
        }
    }

    internal static class TcpUtility
    {
        private static T[] Copy<T>(this T[] source, int index, int count)
        {
            T[] result = new T[count];
            Array.Copy(source, index, result, 0, count);
            return result;
        }

        public static T[] Skip<T>(this T[] source, int count) => source.Copy(count, source.Length - count);

        public static T[] Take<T>(this T[] source, int count) => source.Copy(0, count);
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

        private const int SIZE_OF_BUFFER = 256;
        private const int SIZE_OF_INT32 = 4;
        private const int MAX_SIZE_OF_PACKET = 256 * 256 * 256; // 16 MByte

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
                throw new Exception("heap corruption");
            }

            byte[] head = BitConverter.GetBytes(length);
            byte[] buffer = new byte[SIZE_OF_INT32 + length];

            Buffer.BlockCopy(head, 0, buffer, 0, SIZE_OF_INT32);
            Buffer.BlockCopy(data, 0, buffer, SIZE_OF_INT32, length);

            this.stream.Write(buffer, 0, buffer.Length);
            return buffer.Length;
        }

        public void Read(Action<byte[]> onReceive)
        {
            byte[] buffer = new byte[SIZE_OF_BUFFER];
            int readBufferLength = this.stream.Read(buffer, 0, SIZE_OF_BUFFER);
            if (readBufferLength > 0) {
                byte[] data = buffer.Take(readBufferLength);

                while (data != null && data.Length > 0) {
                    if (this.lengthOfPacket == 0) {
                        // 从头开始读取
                        this.lengthOfPacket = BitConverter.ToInt32(data.Take(SIZE_OF_INT32), 0);

                        if (this.lengthOfPacket > this.MaxSizeOfPacket || this.lengthOfPacket <= 0) {
                            // 非法长度，抛出异常
                            throw new Exception("heap corruption");
                        }

                        this.indexOfPacket = 0;
                        this.bufferOfPacket = new byte[this.lengthOfPacket];
                        data = data.Skip(SIZE_OF_INT32);
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
                            onReceive?.Invoke(this.bufferOfPacket);
                        }
                    }
                }
            }
        }
    }
}