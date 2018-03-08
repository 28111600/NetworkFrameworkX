using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Client
{
    public class Client : LocalCallable, ICaller, ITerminal, IUdpSender
    {
        public User User = new User();

        private ServerStatus _Status = ServerStatus.Close;

        public string Name => "Client";

        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public event EventHandler<LogEventArgs> Log;

        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        private AESKey AESKey = null;

        private RSAKey RSAKey = null;

        public IUserCollection<IUser> UserList { get; private set; } = new UserCollection<IUser>();

        private ISerialzation<string> JsonSerialzation { get; } = new JsonSerialzation();

        public ISerialzation<IEnumerable<byte>> BinarySerialzation { get; private set; } = new BinarySerialzation();

        public string Guid { get; set; } = null;

        public IPEndPoint NetAddress { get; set; }

        public VirtualServer Server { get; private set; }

        public ServerStatus Status
        {
            get => this._Status;
            private set {
                this._Status = value;
                StatusChanged?.Invoke(this, new StatusChangedEventArgs(this._Status));
            }
        }

        public CallerType Type { get; } = CallerType.Client;

        protected override void OnLog(LogLevel level, string name, string text) => Log?.Invoke(this, new LogEventArgs(level, name, text));

        public void HandleCommand(string command)
        {
            Arguments args = new Arguments();
            args.Put("command", command);
            this.Server.CallFunction("command", args, this.Server);
        }

        public void Login(string Name, Arguments args = null)
        {
            args = args ?? new Arguments();
            args.Put("name", Name);
            this.Server.CallFunction("login", args, this.Server);
        }

        public void SendHeartBeat()
        {
            ThreadStart Ts = new ThreadStart(() => {
                while (this.Status == ServerStatus.Connected) {
                    this.Server.CallFunction("heartbeat", this.Server);
                    Thread.Sleep(2000);
                }
            });
            Thread T = new Thread(Ts) { IsBackground = true };
            T.Start();
        }

        public void LoadKey()
        {
            this.Server.RSAKey = new RSAKey();
            this.RSAKey = RSAKey.Generate();
        }

        private void SendMessage(MessageBody message, ITerminal ternimal)
        {
            this.SendMessage(message, ternimal.NetAddress);
        }

        private void SendMessage(MessageBody message, IPEndPoint remoteEndPoint)
        {
            string text = this.JsonSerialzation.Serialize(message);

            this.Send(text, remoteEndPoint);
        }

        private byte[] ValidData = null;
        private bool ServerValidated { get; set; } = false;

        private void RequestValidate()
        {
            this.ValidData = System.Guid.NewGuid().ToByteArray();

            MessageBody message = new MessageBody()
            {
                Guid = null,
                TimeStamp = Utility.GetTimeStamp(),
                Content = RSAHelper.Encrypt(this.ValidData, this.Server.RSAKey.XmlPublicKey),
                Flag = MessageFlag.RequestValidate
            };

            this.SendMessage(message, this.Server);
        }

        private void RequestPublicKey()
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                TimeStamp = Utility.GetTimeStamp(),
                Flag = MessageFlag.RequestPublicKey
            };

            this.SendMessage(message, this.Server);
        }

        private void SendClientPublicKey()
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                TimeStamp = Utility.GetTimeStamp(),
                Content = RSAHelper.Encrypt(this.RSAKey.XmlPublicKey, this.Server.RSAKey.XmlPublicKey),
                Flag = MessageFlag.SendClientPublicKey
            };

            this.SendMessage(message, this.Server);
        }

        public Client()
        {
            SetListenHandler();

            LoadInternalCommand();
        }

        public bool Start(string ip, int port)
        {
#if DEBUG
            this.Logger.Warning("!!! Debug Mode !!!");
#endif

            this.NetAddress = new IPEndPoint(IPAddress.Any, 0);
            this.UdpClient = new UdpClient(this.NetAddress);

            this.Server = new VirtualServer() { NetAddress = new IPEndPoint(IPAddress.Parse(ip), port), Client = this };
            this.Status = ServerStatus.Connecting;

            LoadKey();

            StartListen();

            this.RequestPublicKey();

            return true;
        }

        public void SetListenHandler()
        {
            this.ReceiveInternal += (data, remoteEndPoint) => {
                string text = data.GetString();

                DataReceived?.Invoke(this, new DataReceivedEventArgs(remoteEndPoint.Address, remoteEndPoint.Port, text));
                try {
                    if (this.Server.NetAddress.Address.Equals(remoteEndPoint.Address)) {
                        MessageBody message = this.JsonSerialzation.Deserialize<MessageBody>(text);

                        //接受服务端公钥
                        if (message.Flag == MessageFlag.SendPublicKey) {
                            this.Logger.Debug("接受      : 服务端RSA公钥");
                            this.Server.RSAKey.XmlPublicKey = message.Content.GetString();
                            this.Logger.Debug("发送      : 请求签名");
                            this.RequestValidate();
                            return;
                        }

                        //服务端未验证则不响应
                        if (!message.Flag.In(MessageFlag.ResponseValidate, MessageFlag.RefuseValidate) && !this.ServerValidated) {
                            return;
                        }

                        if (message.Flag == MessageFlag.ResponseValidate && !this.ServerValidated) {
                            this.ServerValidated = RSAHelper.SignatureValidate(this.ValidData, message.Content, this.Server.RSAKey.XmlPublicKey);
                            if (this.ServerValidated) {
                                this.Logger.Debug("服务端验证: 成功");
                                this.SendClientPublicKey();
                                this.Logger.Debug("发送      : 客户端公钥");
                            } else {
                                this.Status = ServerStatus.Close;
                                this.Logger.Debug("服务端验证: 失败");
                            }
                        } else if (message.Flag == MessageFlag.RefuseValidate) {
                            this.Status = ServerStatus.Close;
                            this.Logger.Debug("服务端验证: 失败");
                        } else if (message.Flag == MessageFlag.SendAESKey) {
                            this.Logger.Debug("接受      : AES密钥");
                            this.Guid = this.Server.Guid = message.Guid;
                            AESKey key = this.JsonSerialzation.Deserialize<AESKey>(RSAHelper.Decrypt(message.Content, this.RSAKey).GetString());
                            this.Server.AESKey = this.AESKey = key;
                            this.Logger.Debug("密钥交换  : 成功");
                            this.Status = ServerStatus.Connected;
                            SendHeartBeat();
                        } else if (message.Flag == MessageFlag.Message) {
                            if (!string.IsNullOrWhiteSpace(message.Guid)) {
                                if (this.AESKey != null) {
                                    CallBody call = this.JsonSerialzation.Deserialize<CallBody>(AESHelper.Decrypt(message.Content, this.AESKey).GetString());

                                    if (!this.User.Guid.IsNullOrEmpty()) {
                                        if (this.User.Guid == message.Guid) {
                                            if (this.User.TimeStamp <= message.TimeStamp) {
                                                this.User.TimeStamp = message.TimeStamp;
                                                this.FunctionList.Call(call.Call, call.Args, this);
                                            }
                                        }
                                    }

                                    if (call.Call == "login") {
                                        this.User.Guid = message.Guid;
                                        this.User.Name = call.Args.GetString("name");
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception e) {
                    this.Logger.Error(e.Message);
                    this.Status = ServerStatus.Close;
                }
            };
        }

        public void Stop()
        {
            this.Status = ServerStatus.Close;
            this.UdpClient.Close();
        }

        private void LoadInternalCommand()
        {
            Function funcWriteLine = new Function()
            {
                Name = "writeline",
                Func = (args, caller) => {
                    if (this.Status == ServerStatus.Connected) {
                        if (args.ContainsKey("text", "level")) {
                            OnLog((LogLevel)args.GetInt("level"), args.GetString("name"), args.GetString("text"));
                        }
                    }
                    return 0;
                }
            };
            AddFunction(funcWriteLine);
            Function funcLogout = new Function()
            {
                Name = "logout",
                Func = (args, caller) => {
                    if (this.Status == ServerStatus.Connected) {
                        OnLog(LogLevel.Info, null, "logout");
                        this.Stop();
                    }
                    return 0;
                }
            };
            AddFunction(funcLogout);
        }
    }
}