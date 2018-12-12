using System;
using System.Net;
using System.Net.Sockets;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Share
{
    public abstract class LocalCallable : TcpSender
    {
        public FunctionCollection FunctionTable { get; private set; } = new FunctionCollection();

        public bool AddFunction(IFunction func) => this.FunctionTable.Add(func);

        public int CallFunction(string name, IArguments args, ICaller caller) => this.FunctionTable.Call(name, args, caller);

        public ILogger Logger { get; private set; } = null;

        protected abstract void OnLog(LogLevel level, string name, string text);

        public LocalCallable()
        {
            this.Logger = new Logger((sender, e) => OnLog(e.Level, e.Name, e.Text));
        }
    }

    public abstract class RemoteCallable : TcpSender
    {
        public event EventHandler<SocketExcptionEventArgs> SocketError;

        public string Guid { get; set; }

        internal AESKey Key { get; set; }

        public IPEndPoint NetAddress { get; set; } = null;

        internal abstract TcpClient TcpClient { get; }

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
                    Content = AESHelper.Encrypt(this.JsonSerialzation.Serialize(call), this.AESKey)
                };

                string text = this.JsonSerialzation.Serialize(message);
                this.Send(text, this.TcpClient);
                return 0;
            } catch (SocketException ex) {
                SocketError?.Invoke(this, new SocketExcptionEventArgs(this.Guid, ex));
                return -1;
            }
        }
    }

    public abstract class TcpSender : MarshalByRefObject, ITcpSender
    {
        public long Traffic_In { get; protected set; } = 0;

        public long Traffic_Out { get; protected set; } = 0;

        protected Action<SocketException> SocketExceptionInternal;

        protected Action<byte[], IPEndPoint> ReceiveInternal;

        internal void Send(string text, TcpClient tcpClient) => Send(text.GetBytes(), tcpClient);

        internal void Send(byte[] data, TcpClient tcpClient)
        {
            if (tcpClient != null && tcpClient.IsConnected) {
#if GZIP
                byte[] dataCompress = GZip.Compress(data);
                this.Traffic_Out += tcpClient.Send(dataCompress);
#else
                this.Traffic_Out += tcpClient.Send(data);
#endif
            }
        }
    }
}