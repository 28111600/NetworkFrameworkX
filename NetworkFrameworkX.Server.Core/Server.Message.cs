using System;
using System.Threading;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    public partial class Server<TConfig>
    {
        private void SendMessage(string message, TcpClient tcpClient) => tcpClient.Send(message);

        private void SendMessage(MessageBody message, TcpClient tcpClient)
        {
            string text = this.JsonSerialzation.Serialize(message);

            this.Send(text, tcpClient);
        }

        private void RefuseSignature(TcpClient tcpClient)
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                Content = null,
                Flag = MessageFlag.RefuseValidate
            };

            this.SendMessage(message, tcpClient);
        }

        private void SendSignature(byte[] inputData, TcpClient tcpClient)
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                Content = RSAHelper.Signature(inputData, this.RSAKey),
                Flag = MessageFlag.ResponseValidate
            };

            this.SendMessage(message, tcpClient);
        }

        private void SendPublicKey(TcpClient tcpClient)
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                Content = this.RSAKey.PublicKey,
                Flag = MessageFlag.SendPublicKey
            };

            this.SendMessage(message, tcpClient);
        }

        private void GenerateAndSendAESKey(byte[] inputData, TcpClient tcpClient)
        {
            string guid = System.Guid.NewGuid().ToString();
            byte[] clientPublicKey = RSAHelper.Decrypt(inputData, this.RSAKey);
            AESKey key = AESKey.Generate();
            this.AESKeyList.Add(guid, key);

            MessageBody message = new MessageBody()
            {
                Guid = guid,
                Content = RSAHelper.Encrypt(this.JsonSerialzation.Serialize(key), clientPublicKey),
                Flag = MessageFlag.SendAESKey
            };

            this.SendMessage(message, tcpClient);
        }

        private void HandleMessage(object sender, TcpClient.ReceiveEventArgs e)
        {
            this.Traffic_In += e.Data.Length;

            TcpClient tcpClient = sender as TcpClient;
#if GZIP
            string text = GZip.Decompress(e.Data).GetString();
#else
            string text = e.Data.GetString();
#endif
            DataReceived?.Invoke(this, new DataReceivedEventArgs(tcpClient.RemoteAddress.Address, tcpClient.RemoteAddress.Port, text));

            this.Logger.Debug($"DataReceived: {tcpClient.RemoteAddress}");

            try {
                MessageBody message = this.JsonSerialzation.Deserialize<MessageBody>(text);

                if (message.Flag == MessageFlag.RequestPublicKey) {
                    this.Logger.Debug("AKA", $"客户端    : 请求公钥 - {tcpClient.RemoteAddress}");
                    this.SendPublicKey(tcpClient);
                    this.Logger.Debug("AKA", $"发送      : 服务端公钥- {tcpClient.RemoteAddress}");
                } else if (message.Flag == MessageFlag.RequestValidate) {
                    this.Logger.Debug("AKA", $"客户端    : 请求签名 - {tcpClient.RemoteAddress}");
                    byte[] rawData = RSAHelper.Decrypt(message.Content, this.RSAKey);
                    if (rawData != null) {
                        this.SendSignature(rawData, tcpClient);
                        this.Logger.Debug("AKA", $"发送      : 服务端签名 - {tcpClient.RemoteAddress}");
                    } else {
                        this.RefuseSignature(tcpClient);
                        this.Logger.Debug("AKA", $"解析数据  : 失败 - {tcpClient.RemoteAddress}");
                    }
                } else if (message.Flag == MessageFlag.SendClientPublicKey) {
                    this.Logger.Debug("AKA", $"接受      : 客户端公钥 - {tcpClient.RemoteAddress}");
                    this.Logger.Debug("AKA", $"生成      : AES密钥 - {tcpClient.RemoteAddress}");
                    this.GenerateAndSendAESKey(message.Content, tcpClient);
                    this.Logger.Debug("AKA", $"发送      : AES密钥 - {tcpClient.RemoteAddress}");
                } else if (message.Flag == MessageFlag.Message) {
                    if (!string.IsNullOrWhiteSpace(message.Guid) && this.AESKeyList.ContainsKey(message.Guid)) {
                        AESKey key = this.AESKeyList[message.Guid];

                        CallBody call = message.Content != null ? this.JsonSerialzation.Deserialize<CallBody>(AESHelper.Decrypt(message.Content, key).GetString()) : null;

                        if (this.UserList.ContainsKey(message.Guid)) {
                            IServerUser user = this.UserList[message.Guid];
                            user.RefreshHeartBeat();
                            this.Logger.Debug($"RefreshHeartBeat: {user.Name} / {user.Guid}");

                            if (call != null) {
                                ThreadPool.QueueUserWorkItem((x) => {
                                    var tuple = x as Tuple<LocalCallable, CallBody, ICaller>;
                                    tuple.Item1.CallFunction(tuple.Item2.Call, tuple.Item2.Args, tuple.Item3);
                                }, new Tuple<LocalCallable, CallBody, ICaller>(this, call, user));
                            }
                        } else {
                            //新登录
                            if (call == null) { return; }
                            if (call.Call == "login") {
                                this.Logger.Debug($"尝试登入 - {tcpClient.RemoteAddress.Address}");

                                ServerUser user = new ServerUser()
                                {
                                    Guid = message.Guid,
                                    Server = this,
                                    Name = null,
                                    NetAddress = tcpClient.RemoteAddress,
                                    AESKey = this.AESKeyList[message.Guid]
                                };

                                if (ClientPreLogin != null) {
                                    ClientPreLoginEventArgs<ServerUser> eventArgs = new ClientPreLoginEventArgs<ServerUser>(ref user, call.Args);
                                    ClientPreLogin?.Invoke(this, eventArgs);
                                    user = eventArgs.User;
                                }

                                if (user != null) {
                                    user._TcpClient = tcpClient;
                                    if (user.Status == UserStatus.Online) {
                                        user.LoginTime = DateTime.Now;

                                        user.SocketError += (x, y) => {
                                            this.Logger.Error("SocketError", y.Exception.Message);
                                            ForceLogout(this.UserList[y.Guid]);
                                        };

                                        user.RefreshHeartBeat();

                                        this.UserList.Add(user.Guid, user);

                                        Arguments args = new Arguments();
                                        args.Put("status", true);
                                        args.Put("guid", user.Guid);
                                        args.Put("name", user.Name);

                                        user.CallFunction("login", args);

                                        ClientLogin?.Invoke(this, new ClientEventArgs<IServerUser>(user, ClientLoginStatus.Success));

                                        this.Logger.Debug($"登入成功 - {tcpClient.RemoteAddress.Address}");
                                    } else if (user.Status == UserStatus.Offline) {
                                        Arguments args = new Arguments();
                                        args.Put("status", false);
                                        ClientLogin?.Invoke(this, new ClientEventArgs<IServerUser>(user, ClientLoginStatus.Fail));
                                        user.CallFunction("login", args);
                                        this.Logger.Error($"登入失败 - {tcpClient.RemoteAddress.Address}");
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                this.Logger.Error(ex.Message);
            }
        }
    }
}