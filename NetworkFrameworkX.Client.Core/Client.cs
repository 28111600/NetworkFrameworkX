using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Client
{
    public class Client : LocalCallable, ICaller, ITerminal, ITcpSender
    {
        public int Timeout = 5 * 1000;

        public User User = new User();

        internal TcpClient TcpClient { get; set; }

        private ServerStatus _Status = ServerStatus.Close;

        public string Name => "Client";

        internal event EventHandler<DataReceivedEventArgs> DataReceived;

        public event EventHandler<LogEventArgs> Log;

        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        public event EventHandler<ClientEventArgs<User>> ClientLogin;

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

        public int CallFunction(string name, IArguments args = null) => this.FunctionList.Call(name, args ?? new Arguments(), this);

        protected override void OnLog(LogLevel level, string name, string text) => Log?.Invoke(this, new LogEventArgs(level, name, text));

        public void HandleCommand(string command)
        {
            Arguments args = new Arguments();
            args.Put("command", command);
            this.Server.CallFunction("command", args);
        }

        public void Login(Arguments args = null)
        {
            args = args ?? new Arguments();
            this.Server.CallFunction("login", args);
        }

        public void SendHeartBeat()
        {
            ThreadStart Ts = new ThreadStart(() => {
                while (this.Status == ServerStatus.Connected) {
                    this.Server.CallFunction("heartbeat");
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

        private void SendMessage(MessageBody message, TcpClient tcpClient)
        {
            string text = this.JsonSerialzation.Serialize(message);

            this.Send(text, tcpClient);
        }

        private byte[] ValidData = null;
        private bool ServerValidated { get; set; } = false;

        private void RequestValidate()
        {
            this.ValidData = System.Guid.NewGuid().ToByteArray();

            MessageBody message = new MessageBody()
            {
                Guid = null,
                Content = RSAHelper.Encrypt(this.ValidData, this.Server.RSAKey.XmlPublicKey),
                Flag = MessageFlag.RequestValidate
            };

            this.SendMessage(message, this.TcpClient);
        }

        private void RequestPublicKey()
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                Flag = MessageFlag.RequestPublicKey
            };

            this.SendMessage(message, this.TcpClient);
        }

        private void SendClientPublicKey()
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                Content = RSAHelper.Encrypt(this.RSAKey.XmlPublicKey, this.Server.RSAKey.XmlPublicKey),
                Flag = MessageFlag.SendClientPublicKey
            };

            this.SendMessage(message, this.TcpClient);
        }

        public Client()
        {
        }

        public bool Start(string ip, int port)
        {
#if DEBUG
            this.Logger.Warning("!!! Debug Mode !!!");
#endif

            this.Server = new VirtualServer() { NetAddress = new IPEndPoint(IPAddress.Parse(ip), port), Client = this };
            this.Status = ServerStatus.Connecting;
            this.ServerValidated = false;

            this.TcpClient = new TcpClient();

            SetListenHandler();

            LoadInternalCommand();

            LoadKey();

            this.TcpClient.Connect(ip, port);

            this.TcpClient.Start();

            this.RequestPublicKey();

            ThreadStart ts = new ThreadStart(() => {
                Thread.Sleep(this.Timeout);
                if (this.Status == ServerStatus.Connecting) {
                    this.ClientLogin?.Invoke(this, new ClientEventArgs<User>(this.User, ClientLoginStatus.Fail));
                    this.Logger.Error("登录超时");
                    this.Stop();
                }
            });

            new Thread(ts).Start();

            return true;
        }

        public void SetListenHandler()
        {
            this.TcpClient.OnReceive += (sender, e) => {
                this.Traffic_In += e.Data.Length;

                TcpClient tcpClient = sender as TcpClient;
#if GZIP
                string text = GZip.Decompress(e.Data).GetString();
#else
                string text = e.Data.GetString();
#endif
                DataReceived?.Invoke(this, new DataReceivedEventArgs(tcpClient.RemoteAddress.Address, tcpClient.RemoteAddress.Port, text));

                try {
                    MessageBody message = this.JsonSerialzation.Deserialize<MessageBody>(text);

                    //接受服务端公钥
                    if (message.Flag == MessageFlag.SendPublicKey) {
                        this.Logger.Debug("AKA", "接受      : 服务端RSA公钥");
                        this.Server.RSAKey.XmlPublicKey = message.Content.GetString();
                        this.Logger.Debug("AKA", "发送      : 请求签名");
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
                            this.Logger.Debug("AKA", "服务端验证: 成功");
                            this.SendClientPublicKey();
                            this.Logger.Debug("AKA", "发送      : 客户端公钥");
                        } else {
                            this.Status = ServerStatus.Close;
                            this.Logger.Debug("AKA", "服务端验证: 失败");
                        }
                    } else if (message.Flag == MessageFlag.RefuseValidate) {
                        this.Status = ServerStatus.Close;
                        this.Logger.Debug("AKA", "服务端验证: 失败");
                    } else if (message.Flag == MessageFlag.SendAESKey) {
                        this.Logger.Debug("AKA", "接受      : AES密钥");
                        this.Guid = this.Server.Guid = message.Guid;
                        AESKey key = this.JsonSerialzation.Deserialize<AESKey>(RSAHelper.Decrypt(message.Content, this.RSAKey).GetString());
                        this.Server.AESKey = this.AESKey = key;
                        this.Logger.Debug("AKA", "密钥交换  : 成功");
                        this.Status = ServerStatus.Connected;
                        SendHeartBeat();
                    } else if (message.Flag == MessageFlag.Message) {
                        if (!string.IsNullOrWhiteSpace(message.Guid)) {
                            if (this.AESKey != null) {
                                CallBody call = this.JsonSerialzation.Deserialize<CallBody>(AESHelper.Decrypt(message.Content, this.AESKey).GetString());

                                if (!this.User.Guid.IsNullOrEmpty()) {
                                    if (this.User.Guid == message.Guid) {
                                            this.FunctionList.Call(call.Call, call.Args, this);
                                    }
                                }

                                if (call.Call == "login") {
                                    if (call.Args.GetBool("status")) {
                                        this.User.Guid = message.Guid;
                                        this.User.Name = call.Args.GetString("name");
                                        this.ClientLogin?.Invoke(this, new ClientEventArgs<User>(this.User, ClientLoginStatus.Success));
                                    } else {
                                        this.ClientLogin?.Invoke(this, new ClientEventArgs<User>(this.User, ClientLoginStatus.Fail));
                                        this.Logger.Error("登录失败");
                                        this.Stop();
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    this.Logger.Error(ex.Message);
                    this.Stop();
                }
            };
        }

        public void Stop()
        {
            this.Status = ServerStatus.Close;
            this.TcpClient.Close();
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