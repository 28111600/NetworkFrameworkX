using System;
using System.Net;
using System.Net.Sockets;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Share
{
    public abstract class LocalCallable : UdpSender
    {
        protected FunctionCollection FunctionList = new FunctionCollection();

        public bool AddFunction(IFunction func) => this.FunctionList.Add(func);

        public ILogger Logger { get; private set; } = null;

        protected abstract void OnLog(LogLevel level, string name, string text);

        public LocalCallable()
        {
            this.Logger = new Logger((sender, e) => OnLog(e.Level, e.Name, e.Text));
        }
    }

    public abstract class RemoteCallable : ITerminal
    {
        public event EventHandler<SocketExcptionEventArgs> SocketError;

        public string Guid { get; set; }

        internal AESKey Key { get; set; }

        public IPEndPoint NetAddress { get; set; } = null;

        public abstract IUdpSender UdpSender { get; }

        public ILogger Logger { get; private set; } = null;

        private JsonSerialzation JsonSerialzation = new JsonSerialzation();

        protected abstract void OnLog(LogLevel level, string name, string text);

        internal AESKey AESKey { get; set; } = null;

        public RemoteCallable()
        {
            this.Logger = new Logger((sender, e) => OnLog(e.Level, e.Name, e.Text));
        }

        protected int CallFunction(string name, IArguments args, ICaller caller)
        {
            try {
                CallBody call = new CallBody() { Call = name, Args = args as Arguments };
                MessageBody message = new MessageBody()
                {
                    Flag = MessageFlag.Message,
                    Guid = this.Guid,
                    Content = AESHelper.Encrypt(this.JsonSerialzation.Serialize(call), this.AESKey),
                    TimeStamp = Utility.GetTimeStamp()
                };

                string text = this.JsonSerialzation.Serialize(message);
                this.UdpSender.Send(text.GetBytes(), this);
                return 0;
            } catch (SocketException) {
                SocketError?.Invoke(this, new SocketExcptionEventArgs(this.Guid));
                return -1;
            }
        }
    }

    public abstract class UdpSender : IUdpSender
    {
        public long Traffic_In { get; private set; } = 0;

        public long Traffic_Out { get; private set; } = 0;

        public UdpClient UdpClient { get; protected set; }

        protected void StartListen()
        {
            BeginReceived(this.UdpClient, this.ReceivedCircle);
        }

        private static void BeginReceived(UdpClient udpClient, AsyncCallback requestCallback)
        {
            if (udpClient != null && udpClient.Client != null) {
                try {
                    udpClient.BeginReceive(requestCallback, null);
                } catch (Exception) {
                    if (udpClient != null && udpClient.Client != null) {
                        BeginReceived(udpClient, requestCallback);
                    }
                }
            }
        }

        private void ReceivedCircle(IAsyncResult result)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try {
                byte[] data = this.UdpClient.EndReceive(result, ref remoteEndPoint);

                this.Traffic_In += data.LongLength;

                byte[] dataDecompress = GZip.Decompress(data);

                this.ReceiveInternal?.Invoke(dataDecompress, remoteEndPoint);
            } catch (SocketException e) {
                if (e.SocketErrorCode.In(SocketError.ConnectionReset, SocketError.NetworkReset)) {
                    this.SocketExceptionInternal?.Invoke(e);
                } else {
                    throw;
                }
            } catch (Exception) {
                if (this.UdpClient.Client != null && this.UdpClient.Client.Connected) { throw; }
            } finally {
                BeginReceived(this.UdpClient, this.ReceivedCircle);
            }
        }

        protected Action<SocketException> SocketExceptionInternal;

        protected Action<byte[], IPEndPoint> ReceiveInternal;

        public void Send(string text, ITerminal ternimal) => Send(text, ternimal.NetAddress);

        public void Send(string text, IPEndPoint endPoint) => Send(text.GetBytes(), endPoint);

        public void Send(byte[] data, ITerminal ternimal) => Send(data, ternimal.NetAddress);

        public void Send(byte[] data, IPEndPoint endPoint)
        {
            if (endPoint != null) {
                byte[] dataCompress = GZip.Compress(data);
                this.Traffic_Out += this.UdpClient.Send(dataCompress, dataCompress.Length, endPoint);
            }
        }
    }
}