using System;
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
                this.stream.EndRead += (x, y) => this.OnReceive?.Invoke(this, new ReceiveEventArgs(y.Data, y.Data.Length));
                while (this.IsConnected) {
                    this.stream.Read();
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
        public static T[] Copy<T>(this T[] src, int index, int count)
        {
            T[] result = new T[count];
            Buffer.BlockCopy(src, index, result, 0, count);
            return result;
        }

        public static T[] Skip<T>(this T[] src, int count) => src.Copy(count, src.Length - count);

        public static T[] Take<T>(this T[] src, int count) => src.Copy(0, count);

        public static T[] Concat<T>(this T[] src, T[] dst)
        {
            T[] result = new T[src.Length + dst.Length];
            Buffer.BlockCopy(src, 0, result, 0, src.Length);
            Buffer.BlockCopy(dst, 0, result, src.Length, dst.Length);
            return result;
        }
    }
}