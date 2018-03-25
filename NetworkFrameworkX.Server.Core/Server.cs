using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    public sealed class Server : Server<ServerConfig>
    {
    }

    public class Server<TConfig> : LocalCallable, IServer where TConfig : IServerConfig, new()
    {
        private static readonly object _lockObject = new object();

        public CallerType Type { get; } = CallerType.Console;

        private TcpServer TcpServer { get; set; }

        public string Guid { get; set; } = null;

        public string Name { get; set; } = "NetworkFrameworkXServer";

        internal event EventHandler<DataReceivedEventArgs> DataReceived;

        public event EventHandler<LogEventArgs> Log;

        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        public event EventHandler<ClientPreLoginEventArgs<ServerUser>> ClientPreLogin;

        public event EventHandler<ClientEventArgs<IServerUser>> ClientLogin;

        public event EventHandler<ClientEventArgs<IServerUser>> ClientLogout;

        private Dictionary<string, AESKey> AESKeyList = new Dictionary<string, AESKey>();

        private RSAKey RSAKey = null;

        public event EventHandler<EventArgs> AfterStop;

        private History History { get; } = new History() { MaxLength = 1024 };

        public IServerConfig Config { get; set; }

        public IUserCollection<IServerUser> UserList { get; private set; } = new UserCollection<IServerUser>();

        public Dictionary<string, TcpClient> TcpClientList { get; private set; } = new Dictionary<string, TcpClient>();

        public ISerialzation<string> JsonSerialzation { get; private set; } = new JsonSerialzation();

        public ISerialzation<IEnumerable<byte>> BinarySerialzation { get; private set; } = new BinarySerialzation();

        public FunctionCollection CommandList = new FunctionCollection();

        public bool AddCommand(IFunction func) => this.CommandList.Add(func);

        public int CallCommand(string name, IArguments args, ICaller caller = null) => this.CommandList.Call(name, args, caller ?? this);

        public int CallCommand(string name, ICaller caller = null) => CallCommand(name, new Arguments(), caller);

        public string[] GetCommandName() => this.CommandList.Keys.ToArray();

        public string[] GetHistory() => this.History.ToArray();

        public int CallFunction(string name, IArguments args = null) => this.FunctionList.Call(name, args ?? new Arguments(), this);

        private List<string> _logToWrite = new List<string>();

        private static string GetLogString(LogLevel level, string name, string text)
        {
            return string.Format($"[{{0}} {{1}}]: {{2}}", Utility.GetTimeString(DateTime.Now), level.ToText(), text);
        }

        protected override void OnLog(LogLevel level, string name, string text)
        {
            Log?.Invoke(this, new LogEventArgs(level, name, text));

            if (this.Config == null) {
                this._logToWrite.Add(GetLogString(level, name, text));
            } else {
                if (this.Config.Log) {
                    if (this._logToWrite != null) {
                        this._logToWrite.ForEach(x => this.LogWriter?.WriteLine(x));
                        this._logToWrite = null;
                    }
                    this.LogWriter?.WriteLine(GetLogString(level, name, text));
                }
            }
        }

        public string WorkPath { get; private set; }

        public Server() : this(new FileInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName).DirectoryName)
        {
        }

        public Server(string workPath)
        {
            this.WorkPath = workPath;
        }

        public void LoadConfig() => LoadConfig(this);

        public void LoadConfig(ICaller caller)
        {
            string path = GetFilePath(FilePath.Config);
            this.Config = ServerConfig.Load<TConfig>(path);
            caller.Logger.Info(this.lang.LoadConfig);
        }

        public void SaveConfig(ICaller caller)
        {
            string path = GetFilePath(FilePath.Config);
            ServerConfig.Save((TConfig)this.Config, path);
            caller.Logger.Info(this.lang.SaveConfig);
        }

        public void LoadKey(bool generate = false)
        {
            string pathKeys = GetFilePath(FilePath.Keys);

            if (!File.Exists(pathKeys) || generate) {
                this.Logger.Info(this.lang.GenerateKeys);
                this.RSAKey = RSAKey.Generate();
                File.WriteAllText(pathKeys, this.RSAKey.Keys.GetBase64String());
            } else {
                try {
                    string keys = File.ReadAllText(pathKeys);
                    this.RSAKey = new RSAKey() { Keys = keys.FromBase64String() };
                    this.RSAKey.GeneratePublicKey();
                } catch (Exception) {
                    // 密钥非法时重新生成
                    LoadKey(true);
                }
            }
        }

        private Language lang = new Language();
        private Dictionary<string, Language> langList = new Dictionary<string, Language>();

        public bool LoadLang(string Name)
        {
            if (this.langList.ContainsKey(Name)) {
                this.lang = this.langList[Name];
                this.Logger.Info(string.Format(this.lang.LoadLanguage, this.lang.Name));
                return true;
            } else {
                return false;
            }
        }

        private void LoadAllLang()
        {
            DirectoryInfo Folder = new DirectoryInfo(GetFolderPath(FolderPath.Lang));

            foreach (FileInfo File in Folder.GetFiles("*.json")) {
                Language Lang = Language.Load(File.FullName);
                if (this.langList.ContainsKey(Lang.Name)) {
                    this.langList.Remove(Lang.Name);
                }

                this.langList.Add(Lang.Name, Lang);
            }

            if (this.langList.Count == 0) {
                Language DefaultLang = new Language();
                DefaultLang.Save(Path.Combine(GetFolderPath(FolderPath.Lang), $"{DefaultLang.Name}.json"));
                this.langList.Add(DefaultLang.Name, DefaultLang);
            }
        }

        private ServerStatus _Status = ServerStatus.Close;

        public ServerStatus Status
        {
            get => this._Status;
            private set {
                this._Status = value;
                StatusChanged?.Invoke(this, new StatusChangedEventArgs(this._Status));
            }
        }

        public string GetFolderPath(FolderPath path)
        {
            switch (path) {
                case FolderPath.Root:
                    return this.WorkPath;

                case FolderPath.Config:
                    return Path.Combine(GetFolderPath(FolderPath.Root), "config");

                case FolderPath.Lang:
                    return Path.Combine(GetFolderPath(FolderPath.Root), "lang");

                case FolderPath.Plugin:
                    return Path.Combine(GetFolderPath(FolderPath.Root), "plugin");

                case FolderPath.PluginDependency:
                    return Path.Combine(GetFolderPath(FolderPath.Plugin), "dependency");

                case FolderPath.Log:
                    return Path.Combine(GetFolderPath(FolderPath.Root), "log");

                default:
                    return null;
            }
        }

        public string GetFilePath(FilePath path)
        {
            switch (path) {
                case FilePath.Config:
                    return Path.Combine(GetFolderPath(FolderPath.Config), "config.json");

                case FilePath.History:
                    return Path.Combine(GetFolderPath(FolderPath.Config), "history.txt");

                case FilePath.Keys:
                    return Path.Combine(GetFolderPath(FolderPath.Config), "keys.txt");

                default:
                    return null;
            }
        }

        public void Initialize()

        {
            this.Logger.Info(this.lang.Initializing);
            foreach (FolderPath item in Enum.GetValues(typeof(FolderPath))) {
                DirectoryInfo folder = new DirectoryInfo(GetFolderPath(item));
                if (!folder.Exists) { folder.Create(); }
            }
        }

        private Dictionary<string, IPlugin> pluginList = new Dictionary<string, IPlugin>();

        public IList<string> PluginList => this.pluginList.Keys.ToList();

        public void SavePluginConfig(string name)
        {
            if (this.pluginList.ContainsKey(name)) {
                SavePluginConfig(this.pluginList[name]);
            }
        }

        private void SavePluginConfig(IPlugin plugin)
        {
            string name = plugin.Name.ToLowerInvariant();

            DirectoryInfo folderPlugin = new DirectoryInfo(Path.Combine(GetFolderPath(FolderPath.Plugin), name));
            if (!folderPlugin.Exists) { folderPlugin.Create(); }

            FileInfo fileConfig = new FileInfo(Path.Combine(folderPlugin.FullName, "config.json"));

            string config = plugin.SerializeConfig();
            if (!config.IsNullOrEmpty()) {
                File.WriteAllText(fileConfig.FullName, config);
            }
        }

        public void LoadPlugin(IPlugin plugin)
        {
            if (plugin.Name.IsNullOrEmpty()) {
                this.Logger.Warning(this.lang.PluginLoadError);
                return;
            }

            string name = plugin.Name.ToLowerInvariant();

            if (this.pluginList.ContainsKey(name)) {
                this.Logger.Warning(string.Format(this.lang.PluginNameDuplicate, plugin.Name));
                return;
            }

            plugin.Server = this;
            this.pluginList.Add(plugin.Name.ToLowerInvariant(), plugin);
            this.Logger.Info(string.Format(this.lang.LoadPlugin, plugin.Name));

            DirectoryInfo folderPlugin = new DirectoryInfo(Path.Combine(GetFolderPath(FolderPath.Plugin), name));
            if (!folderPlugin.Exists) { folderPlugin.Create(); }

            FileInfo fileConfig = new FileInfo(Path.Combine(folderPlugin.FullName, "config.json"));
            if (!fileConfig.Exists) {
                SavePluginConfig(plugin);
            }

            if (fileConfig.Exists) {
                string config = File.ReadAllText(fileConfig.FullName);
                plugin.DeserializeConfig(config);
            }
            if (plugin.Config.Enabled) {
                plugin.OnLoad();
            }
        }

        public void LoadAllPlugin()
        {
            DirectoryInfo folderPlugin = new DirectoryInfo(GetFolderPath(FolderPath.Plugin));

            foreach (FileInfo file in folderPlugin.GetFiles("*.dll")) {
                Assembly asm = Assembly.LoadFile(file.FullName);
                foreach (Type type in asm.GetTypes()) {
                    foreach (Type iFace in type.GetInterfaces()) {
                        if (iFace.Equals(typeof(IPlugin))) {
                            IPlugin plugin = Activator.CreateInstance(type) as IPlugin;
                            LoadPlugin(plugin);
                        }
                    }
                }
            }
        }

        public void HandleCommand(string command, ICaller caller)
        {
            if (caller.Type.In(CallerType.Console)) {
                this.History.Add(command);
            }
            if (caller.Type.In(CallerType.Console, CallerType.Client)) {
                List<string> commandTextList = command.Split(Utility.CharWhiteSpace).ToList();

                commandTextList.RemoveAll((s) => string.IsNullOrWhiteSpace(s));
                if (commandTextList.Count > 0 && !string.IsNullOrWhiteSpace(commandTextList[0])) {
                    if (this.CommandList.ContainsKey(commandTextList[0])) {
                        Arguments args = new Arguments();

                        for (int i = 1; i < commandTextList.Count; i++) {
                            args.Put((i - 1).ToString(), commandTextList[i]);
                        }

                        args.Put("args", command.Substring(commandTextList[0].Length).Trim());

                        CallCommand(commandTextList[0], args, caller);

                        if (commandTextList[0] != "say" && caller.Type == CallerType.Client) {
                            IUser user = caller as IUser;
                            if (caller != null) {
                                this.Logger.Info(string.Format(this.lang.UserCommand, user.Name, command));
                            }
                        }
                    } else {
                        caller.Logger.Error(this.lang.UnknownCommand);
                    }
                }
            }
        }

        public void Stop() => Stop(this);

        public void Stop(ICaller caller)
        {
            Arguments args = new Arguments();

            args.Put("msg", this.lang.ServerClosed);

            this.UserList.ParallelForEach(x => x.CallFunction("logout", args));

            this.Status = ServerStatus.Close;

            caller.Logger.Info(this.lang.ServerStop);

            foreach (IPlugin item in this.pluginList.Values) {
                item.OnDestroy();
            }

            this.TcpServer?.Stop();

            lock (_lockObject) {
                this.LogWriter?.Close();
                this.LogWriter = null;
            }

            AfterStop?.Invoke(this, new EventArgs());
        }

        private void SendMessage(string message, TcpClient tcpClient)
        {
            tcpClient.Send(message);
        }

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

        public void SetListenHandler()
        {
            this.TcpServer.OnClientStart += (sender0, e0) => {
                e0.TcpClient.OnReceive += (sender, e) => {
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

                                        ServerUser userLogin = new ServerUser()
                                        {
                                            Guid = message.Guid,
                                            Server = this,
                                            Name = null,
                                            NetAddress = tcpClient.RemoteAddress,
                                            AESKey = this.AESKeyList[message.Guid]
                                        };

                                        if (ClientPreLogin != null) {
                                            ClientPreLoginEventArgs<ServerUser> eventArgs = new ClientPreLoginEventArgs<ServerUser>(ref userLogin, call.Args);
                                            ClientPreLogin?.Invoke(this, eventArgs);
                                            userLogin = eventArgs.User;
                                        }

                                        if (userLogin != null) {
                                            userLogin._TcpClient = tcpClient;
                                            if (userLogin.Status == UserStatus.Online) {
                                                userLogin.LoginTime = DateTime.Now;

                                                userLogin.SocketError += (x, y) => { ForceLogout(this.UserList[y.Guid]); };

                                                userLogin.RefreshHeartBeat();

                                                this.UserList.Add(userLogin.Guid, userLogin);

                                                Arguments args = new Arguments();
                                                args.Put("status", true);
                                                args.Put("guid", userLogin.Guid);
                                                args.Put("name", userLogin.Name);

                                                userLogin.CallFunction("login", args);

                                                ClientLogin?.Invoke(this, new ClientEventArgs<IServerUser>(userLogin, ClientLoginStatus.Success));

                                                this.Logger.Debug($"登入成功 - {tcpClient.RemoteAddress.Address}");
                                            } else if (userLogin.Status == UserStatus.Offline) {
                                                Arguments args = new Arguments();
                                                args.Put("status", false);
                                                ClientLogin?.Invoke(this, new ClientEventArgs<IServerUser>(userLogin, ClientLoginStatus.Fail));
                                                userLogin.CallFunction("login", args);
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
                };
            };

            this.TcpServer.Start();
        }

        private void ForceLogout(IServerUser user)
        {
            this.AESKeyList.Remove(user.Guid);
            user.LostConnection();
            this.Logger.Info(string.Format(this.lang.ClientLostConnection, user.Name));
            ClientLogout?.Invoke(this, new ClientEventArgs<IServerUser>(user, ClientLoginStatus.Success));
        }

        private StreamWriter LogWriter = null;

        public bool Start()
        {
            this.Logger.Info($"------{this.Name}------");

#if DEBUG
            this.Logger.Warning("#### DEBUG MODE ####");
#endif

            Initialize();

            LoadConfig();

            LoadKey();

            if (this.Config.Log) {
                string pathLog = Path.Combine(GetFolderPath(FolderPath.Log), $"{Utility.GetDateTimeStringForFileName(DateTime.Now)}.log");
                this.LogWriter = new StreamWriter(pathLog, true) { AutoFlush = true };
            }

            this.History.Path = GetFilePath(FilePath.History);
            this.History.Load();

            LoadAllLang();

            LoadLang(this.Config.Language);

            if (!Utility.IsPortAvailabled(this.Config.Port)) {
                this.Logger.Error(string.Format(this.lang.PortNoAvailabled, this.Config.Port));
                this.Stop();
                return false;
            }

            this.TcpServer = new TcpServer(this.Config.Port);

            this.Status = ServerStatus.Connected;

            this.Logger.Info(string.Format(this.lang.Port, this.Config.Port));

            this.Logger.Info(this.lang.ServerStart);

            SetListenHandler();

            AppDomain.CurrentDomain.AssemblyResolve += ((sender, e) => {
                AssemblyName assemblyName = new AssemblyName(e.Name);
                FileInfo assemblyFile = new FileInfo(Path.Combine(GetFolderPath(FolderPath.PluginDependency), $"{assemblyName.Name}.dll"));

                return assemblyFile.Exists ? Assembly.LoadFrom(assemblyFile.FullName) : null;
            });

            this.ClientLogin += (sender, e) => {
                if (e.Status == ClientLoginStatus.Success) {
                    string text = string.Format(this.lang.ClientLogin, e.User.Name);

                    this.UserList.ParallelForEach(x => x.Logger.Info(text));
                    this.Logger.Info(text);
                }
            };

            this.ClientLogout += (sender, e) => {
                if (e.Status == ClientLoginStatus.Success) {
                    string text = string.Format(this.lang.ClientLogout, e.User.Name);

                    this.UserList.ParallelForEach(x => x.Logger.Info(text));
                    this.Logger.Info(text);
                }
            };

            LoadTestCommand();
            LoadInternalCommand();

            LoadAllPlugin();

            ThreadStart ts = new ThreadStart(() => {
                while (this.Status == ServerStatus.Connected) {
                    List<IServerUser> playerLostConnectionList = this.UserList.Where(x => !x.Value.CheckConnection()).Select(x => x.Value).ToList();

                    playerLostConnectionList.ForEach(x => ForceLogout(x));

                    Thread.Sleep(this.Config.Timeout);
                }
            });
            new Thread(ts).Start();

            return true;
        }

        private void LoadInternalCommand()
        {
            /*
             * {"Call":"command","Args":{"command":"cmd arg1 arg2 ..."}}
             */
            this.AddFunction(new Function()
            {
                Name = "command",
                Comment = null,
                Func = (args, caller) => {
                    if (caller.Type.In(CallerType.Client)) {
                        IServerUser user = (IServerUser)caller;

                        string command = args.GetString("command");
                        if (!string.IsNullOrWhiteSpace(command)) {
                            this.HandleCommand(command, caller);
                        }
                    }
                    return 0;
                }
            });

            this.AddFunction(new Function()
            {
                Name = "heartbeat",
                Func = (args, caller) => {
                    return 0;
                }
            });
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void LoadTestCommand()
        {
            AddCommand(new Function()
            {
                Name = "test-rsa",
                Comment = "Test RSA",
                Func = (args, caller) => {
                    string input = args.GetString("args"); input = input.IsNullOrEmpty() ? "TEST TEST TEST 1234567890 你好!" : input;
                    byte[] encryptData = RSAHelper.Encrypt(input, this.RSAKey);
                    byte[] decryptData = RSAHelper.Decrypt(encryptData, this.RSAKey);

                    var dict = new Dictionary<string, string>();
                    dict.Add("input", input);
                    dict.Add("encrypt", BitConverter.ToString(encryptData).Replace("-", string.Empty));
                    dict.Add("decrypt", decryptData.GetString());
                    caller.Logger.Info("Test RSA");
                    caller.Logger.Info(dict);

                    return 0;
                }
            });
        }
    }
}