using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkFrameworkX.Share
{
    public class TcpClient
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
            Start,
            Listening,
            Connected,
            Close
        }

        public event EventHandler<ReceiveEventArgs> OnReceive;

        public event EventHandler<StatusChangeEventArgs> OnStateChange;

        public event EventHandler<ClientEventArgs> OnStart;

        public event EventHandler<ClientEventArgs> OnClose;

        private Thread T { get; set; }

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
                    } catch (Exception) {
                        return false;
                    }
                }
                return true;
            }
        }

        public object List { get; private set; }

        public void Close()
        {
            this.OnStateChange?.Invoke(this, new StatusChangeEventArgs(ConnectState.Close));
            this.OnClose?.Invoke(this, new ClientEventArgs());
            this.TCPClient.Close();
        }

        private const int SIZE_OF_BUFFER = 256;
        private const int SIZE_OF_INT32 = 4;
        public const int MAX_SIZE_OF_PACKET = 256 * 256 * 256; // 16 MByte

        private void StartThreading()
        {
            byte[] bufferOfPacket = null;
            int lengthOfPacket = 0;
            int indexOfPacket = 0;

            try {
                NetworkStream stream = this.TCPClient.GetStream();
                while (this.IsConnected) {
                    byte[] buffer = new byte[SIZE_OF_BUFFER];
                    int readBufferLength = stream.Read(buffer, 0, SIZE_OF_BUFFER);
                    if (readBufferLength > 0) {
                        byte[] data = buffer.Take(readBufferLength).ToArray();

                        while (data != null && data.Length > 0) {
                            if (lengthOfPacket == 0) {
                                // 从头开始读取
                                lengthOfPacket = BitConverter.ToInt32(data.Take(SIZE_OF_INT32).ToArray(), 0);

                                if (lengthOfPacket > MAX_SIZE_OF_PACKET || lengthOfPacket <= 0) {
                                    // 非法长度，关闭连接
                                    this.Close();
                                }

                                indexOfPacket = 0;
                                bufferOfPacket = new byte[lengthOfPacket];
                                data = data.Skip(SIZE_OF_INT32).ToArray();
                            }

                            if (indexOfPacket < lengthOfPacket) {
                                // 半包
                                int length = data.Length;
                                if (length + indexOfPacket < lengthOfPacket) {
                                    // 包不完整
                                    Buffer.BlockCopy(data, 0, bufferOfPacket, indexOfPacket, length);
                                    indexOfPacket += length;
                                    data = null;
                                } else if (length + indexOfPacket >= lengthOfPacket) {
                                    // 包完整，可能粘包
                                    int lengthNeed = lengthOfPacket - indexOfPacket;
                                    Buffer.BlockCopy(data, 0, bufferOfPacket, indexOfPacket, lengthNeed);
                                    data = data.Skip(lengthNeed).ToArray();
                                    lengthOfPacket = indexOfPacket = 0;
                                    this.OnReceive?.Invoke(this, new ReceiveEventArgs(bufferOfPacket, readBufferLength));
                                }
                            }
                        }
                    }
                }
            } catch {
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
                int length = data.Length;
                if (length > MAX_SIZE_OF_PACKET) {
                    throw new Exception("heap corruption");
                }

                byte[] head = BitConverter.GetBytes(length);
                byte[] buffer = new byte[SIZE_OF_INT32 + length];

                Buffer.BlockCopy(head, 0, buffer, 0, SIZE_OF_INT32);
                Buffer.BlockCopy(data, 0, buffer, SIZE_OF_INT32, length);
                return this.TCPClient.Client.Send(buffer);
            } else {
                return 0;
            }
        }
    }
}