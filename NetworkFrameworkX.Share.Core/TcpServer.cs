using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetworkFrameworkX.Share
{
    public class TcpServer
    {
        public class ClientEventArgs : EventArgs
        {
            public TcpClient TcpClient { get; private set; }

            public ClientEventArgs(TcpClient tcpClient)
            {
                this.TcpClient = tcpClient;
            }
        }

        public class ServerEventArgs : EventArgs
        {
            public ServerEventArgs()
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

        public enum ConnectState
        {
            Start,
            Listening,
            NewConnect,
            Connected,
            Stop
        }

        private const int RAMDOMPORT = 0;

        private List<TcpClient> arrayClientConnected;

        public event EventHandler<StatusChangeEventArgs> OnStateChange;

        public event EventHandler<ClientEventArgs> OnClientStart;

        public event EventHandler<ClientEventArgs> OnClientClose;

        public event EventHandler<ServerEventArgs> OnStart;

        public event EventHandler<ServerEventArgs> OnStop;

        public int ClientCount => this.arrayClientConnected.Count;

        private TcpListener TCPListener { get; set; }

        public IPEndPoint LocalAddress => this.TCPListener.Server.LocalEndPoint as IPEndPoint;

        private Thread T { get; set; }

        public TcpServer(int localPort)
        {
            this.arrayClientConnected = new List<TcpClient>();
            this.Init(localPort);
        }

        public TcpServer()
        {
            this.arrayClientConnected = new List<TcpClient>();
            this.Init(0);
        }

        public void Init(int localPort)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
            this.TCPListener = new TcpListener(localEndPoint);
        }

        public void ClientClose(TcpClient tcpClient)
        {
            this.arrayClientConnected.Remove(tcpClient);
            this.OnClientClose?.Invoke(this, new ClientEventArgs(tcpClient));
        }

        private void StartThreading()
        {
            while (this.TCPListener != null) {
                try {
                    TcpClient tcpClient = new TcpClient(this.TCPListener.AcceptTcpClient());
                    this.arrayClientConnected.Add(tcpClient);
                    tcpClient.OnClose += (x, y) => { this.OnClientClose?.Invoke(this, new ClientEventArgs(x as TcpClient)); };
                    this.OnClientStart?.Invoke(this, new ClientEventArgs(tcpClient));
                    this.OnStateChange?.Invoke(this, new StatusChangeEventArgs(ConnectState.NewConnect));
                    tcpClient.Start();
                } catch (Exception) {
                }
            }
        }

        public int Start()
        {
            this.TCPListener.Start();
            this.OnStart?.Invoke(this, new ServerEventArgs());
            this.OnStateChange?.Invoke(this, new StatusChangeEventArgs(ConnectState.Start));
            this.OnStateChange?.Invoke(this, new StatusChangeEventArgs(ConnectState.Listening));
            ThreadStart ts = new ThreadStart(this.StartThreading);
            if (this.T != null && this.T.IsAlive) {
                this.T.Abort();
            }
            this.T = new Thread(ts);
            this.T.Start();
            return this.LocalAddress.Port;
        }

        public void Stop()
        {
            if (this.TCPListener != null) {
                TcpClient[] arrayClientConnectedTemp = this.arrayClientConnected.ToArray();

                Array.ForEach(arrayClientConnectedTemp, (x) => { x.Close(); });

                if (this.TCPListener != null) {
                    this.TCPListener.Stop();
                    this.OnStateChange?.Invoke(this, new StatusChangeEventArgs(ConnectState.Stop));
                    this.OnStop?.Invoke(this, new ServerEventArgs());
                }
            }

            if (this.T.IsAlive) {
                this.T.Abort();
            }
        }
    }
}