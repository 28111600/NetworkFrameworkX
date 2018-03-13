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

        public void Close()
        {
            this.OnStateChange?.Invoke(this, new StatusChangeEventArgs(ConnectState.Close));
            this.OnClose?.Invoke(this, new ClientEventArgs());
            this.TCPClient.Close();
        }

        private const int SIZEOFBUFFER = 256;
        private const int SIZEOFINT32 = 4;

        private void StartThreading()
        {
            byte[] bufferOfPacket = null;
            int lengthOfPacket = 0;
            int indexOfPacket = 0;

            try {
                while (this.IsConnected) {
                    byte[] buffer = new byte[SIZEOFBUFFER];
                    int readBufferLength = this.TCPClient.GetStream().Read(buffer, 0, SIZEOFBUFFER);
                    if (readBufferLength > 0) {
                        byte[] data = buffer.Take(readBufferLength).ToArray();

                        while (data.Length > 0) {
                            if (lengthOfPacket == 0) {
                                // 从头开始读取
                                byte[] len = data.Take(SIZEOFINT32).ToArray();
                                lengthOfPacket = BitConverter.ToInt32(len, 0);
                                indexOfPacket = 0;
                                bufferOfPacket = new byte[lengthOfPacket];
                                data = data.Skip(SIZEOFINT32).ToArray();
                            }

                            if (indexOfPacket < lengthOfPacket) {
                                // 半包
                                if (data.Length + indexOfPacket < lengthOfPacket) {
                                    // 包不完整
                                    Array.Copy(data, 0, bufferOfPacket, indexOfPacket, data.Length);
                                    indexOfPacket += data.Length;
                                    data = new byte[0];
                                } else if (data.Length + indexOfPacket >= lengthOfPacket) {
                                    // 包完整，可能粘包
                                    int lengthNeed = lengthOfPacket - indexOfPacket;
                                    Array.Copy(data, 0, bufferOfPacket, indexOfPacket, lengthNeed);
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
                byte[] head = BitConverter.GetBytes(data.Length);// SIZEOFINT32
                byte[] buffer = new byte[head.Length + data.Length];
                Array.Copy(head, buffer, head.Length);
                Array.Copy(data, 0, buffer, head.Length, data.Length);
                return this.TCPClient.Client.Send(buffer);
            } else {
                return 0;
            }
        }
    }
}