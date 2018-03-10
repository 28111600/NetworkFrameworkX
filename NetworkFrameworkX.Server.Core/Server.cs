using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    public sealed class Server : Server<ServerConfig>
    {
    }

    public class Server<TConfig> : LocalCallable, IServer, IUdpSender where TConfig : IServerConfig, new()
    {
        private static readonly object _lockObject = new object();

        public CallerType Type { get; } = CallerType.Console;

        public IPEndPoint NetAddress { get; set; }

        public string Guid { get; set; } = null;

        public string Name { get; set; } = "NetworkFrameworkXServer";

        public event EventHandler<DataReceivedEventArgs> DataReceived;

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

        public ISerialzation<string> JsonSerialzation { get; private set; } = new JsonSerialzation();

        public ISerialzation<IEnumerable<byte>> BinarySerialzation { get; private set; } = new BinarySerialzation();

        public FunctionCollection CommandList = new FunctionCollection();

        public bool AddCommand(IFunction func) => this.CommandList.Add(func);

        public int CallCommand(string name, IArguments args, ICaller caller = null) => this.CommandList.Call(name, args, caller ?? this);

        public int CallCommand(string name, ICaller caller = null) => CallCommand(name, new Arguments(), caller);

        public string[] GetCommandName() => this.CommandList.Keys.ToArray();

        public string[] GetHistory() => this.History.ToArray();

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

        public void LoadKey()
        {
            string pathKeys = GetFilePath(FilePath.Keys);
            string pathPublicKey = GetFilePath(FilePath.PublicKey);

            FileInfo fileKey = new FileInfo(pathKeys);
            if (!fileKey.Exists) {
                this.RSAKey = RSAKey.Generate();
                File.WriteAllText(pathKeys, this.RSAKey.XmlKeys);
                File.WriteAllText(pathPublicKey, this.RSAKey.XmlPublicKey);
            } else {
                string xmlKeys = File.ReadAllText(pathKeys);
                string xmlPublicKey = File.ReadAllText(pathPublicKey);
                this.RSAKey = new RSAKey(xmlKeys, xmlPublicKey);
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

        private int LoadAllLang()
        {
            int Ret = 0;

            DirectoryInfo Folder = new DirectoryInfo(GetFolderPath(FolderPath.Lang));

            foreach (FileInfo File in Folder.GetFiles("*.json")) {
                Language Lang = Language.Load(File.FullName);
                if (this.langList.ContainsKey(Lang.Name)) {
                    this.langList.Remove(Lang.Name);
                }

                this.langList.Add(Lang.Name, Lang);
                Ret += 1;
            }

            if (this.langList.Count == 0) {
                Language DefaultLang = new Language();
                DefaultLang.Save(Path.Combine(GetFolderPath(FolderPath.Lang), $"{DefaultLang.Name}.json"));
                this.langList.Add(DefaultLang.Name, DefaultLang);
            }

            return Ret;
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
                    return Path.Combine(GetFolderPath(FolderPath.Config), "keys.xml");

                case FilePath.PublicKey:
                    return Path.Combine(GetFolderPath(FolderPath.Config), "publickey.xml");

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

        private void SavePluginConfig(IPlugin plugin)
        {
            string name = plugin.Name.ToLower();

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

            string name = plugin.Name.ToLower();

            if (this.pluginList.ContainsKey(name)) {
                this.Logger.Warning(string.Format(this.lang.PluginNameDuplicate, plugin.Name));
                return;
            }

            plugin.Server = this;
            this.pluginList.Add(plugin.Name.ToLower(), plugin);
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

        private void Stop(ICaller caller)
        {
            Arguments args = new Arguments();

            args.Put("msg", this.lang.ServerClosed);

            /*
             * {"Call":"logout","t":-8587072129809509320,"Args":{"msg":"msg"}}
             */

            this.UserList.ForEach(x => x.CallFunction("logout", args, x));

            this.Status = ServerStatus.Close;

            caller.Logger.Info(this.lang.ServerStop);

            foreach (IPlugin item in this.pluginList.Values) {
                item.OnDestroy();
            }

            this.UdpClient.Close();

            lock (_lockObject) {
                this.LogWriter?.Close();
                this.LogWriter = null;
            }

            AfterStop?.Invoke(this, new EventArgs());
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

        private void RefuseSignature(IPEndPoint remoteEndPoint)
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                TimeStamp = Utility.GetTimeStamp(),
                Content = null,
                Flag = MessageFlag.RefuseValidate
            };

            this.SendMessage(message, remoteEndPoint);
        }

        private void SendSignature(byte[] inputData, IPEndPoint remoteEndPoint)
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                TimeStamp = Utility.GetTimeStamp(),
                Content = RSAHelper.Signature(inputData, this.RSAKey),
                Flag = MessageFlag.ResponseValidate
            };

            this.SendMessage(message, remoteEndPoint);
        }

        private void SendPublicKey(IPEndPoint remoteEndPoint)
        {
            MessageBody message = new MessageBody()
            {
                Guid = null,
                TimeStamp = Utility.GetTimeStamp(),
                Content = this.RSAKey.XmlPublicKey.GetBytes(),
                Flag = MessageFlag.SendPublicKey
            };

            this.SendMessage(message, remoteEndPoint);
        }

        private void GenerateAndSendAESKey(byte[] inputData, IPEndPoint remoteEndPoint)
        {
            string guid = System.Guid.NewGuid().ToString();
            string xmlClientPublicKey = RSAHelper.Decrypt(inputData, this.RSAKey).GetString();
            AESKey key = AESKey.Generate();
            this.AESKeyList.Add(guid, key);

            MessageBody message = new MessageBody()
            {
                Guid = guid,
                TimeStamp = Utility.GetTimeStamp(),
                Content = RSAHelper.Encrypt(this.JsonSerialzation.Serialize(key).GetBytes(), xmlClientPublicKey),
                Flag = MessageFlag.SendAESKey
            };

            this.SendMessage(message, remoteEndPoint);
        }

        public void SetListenHandler()
        {
            this.ReceiveInternal += (data, remoteEndPoint) => {
                string text = data.GetString();
                DataReceived?.Invoke(this, new DataReceivedEventArgs(remoteEndPoint.Address, remoteEndPoint.Port, text));

                try {
                    MessageBody message = this.JsonSerialzation.Deserialize<MessageBody>(text);

                    if (message.Flag == MessageFlag.RequestPublicKey) {
                        this.Logger.Debug($"客户端    : 请求公钥 - {remoteEndPoint.Address}");
                        this.SendPublicKey(remoteEndPoint);
                        this.Logger.Debug("发送      : 服务端公钥");
                    } else if (message.Flag == MessageFlag.RequestValidate) {
                        this.Logger.Debug($"客户端    : 请求签名 - {remoteEndPoint.Address}");
                        byte[] rawData = RSAHelper.Decrypt(message.Content, this.RSAKey);
                        if (rawData != null) {
                            this.SendSignature(rawData, remoteEndPoint);
                            this.Logger.Debug("发送      : 服务端签名");
                        } else {
                            this.RefuseSignature(remoteEndPoint);
                            this.Logger.Debug("解析数据  : 失败");
                        }
                    } else if (message.Flag == MessageFlag.SendClientPublicKey) {
                        this.Logger.Debug("接受      : 客户端公钥");
                        this.Logger.Debug("生成      : AES密钥");
                        this.GenerateAndSendAESKey(message.Content, remoteEndPoint);
                        this.Logger.Debug("发送      : AES密钥");
                    } else if (message.Flag == MessageFlag.Message) {
                        if (!string.IsNullOrWhiteSpace(message.Guid)) {
                            AESKey key = this.AESKeyList[message.Guid];

                            CallBody call = message.Content != null ? this.JsonSerialzation.Deserialize<CallBody>(AESHelper.Decrypt(message.Content, key).GetString()) : null;

                            if (this.UserList.ContainsKey(message.Guid)) {
                                IServerUser user = this.UserList[message.Guid];

                                if (user.NetAddress.Address.Equals(remoteEndPoint.Address)) {
                                    if (user.TimeStamp <= message.TimeStamp) {
                                        user.TimeStamp = message.TimeStamp;
                                        user.RefreshHeartBeat();

                                        if (call != null) { this.FunctionList.Call(call.Call, call.Args, user); }
                                    }
                                }
                            } else {
                                //新登录
                                if (call == null) { return; }
                                if (call.Call == "login") {
                                    ServerUser userLogin = new ServerUser()
                                    {
                                        Guid = message.Guid,
                                        Server = this,
                                        Name = "username",
                                        NetAddress = remoteEndPoint,
                                        AESKey = this.AESKeyList[message.Guid]
                                    };

                                    if (ClientPreLogin != null) {
                                        ClientPreLoginEventArgs<ServerUser> eventArgs = new ClientPreLoginEventArgs<ServerUser>(userLogin, call.Args);
                                        ClientPreLogin?.Invoke(this, eventArgs);
                                        userLogin = eventArgs.User;
                                    }

                                    if (userLogin != null) {
                                        if (userLogin.Status == UserStatus.Online) {
                                            userLogin.SocketError += (sender, e) => {
                                                ForceLogout(this.UserList[e.Guid]);
                                            };

                                            userLogin.RefreshHeartBeat();

                                            this.UserList.Add(userLogin.Guid, userLogin);

                                            Arguments args = new Arguments();
                                            args.Put("status", true);
                                            args.Put("guid", userLogin.Guid);
                                            args.Put("name", userLogin.Name);

                                            /*
                                             * {"Call":"login","t":-8587072129809509320,"Args":{"status":"True"}}
                                             */

                                            userLogin.CallFunction("login", args, userLogin);

                                            ClientLogin?.Invoke(this, new ClientEventArgs<IServerUser>(userLogin, ClientLoginStatus.Success));
                                        } else if (userLogin.Status == UserStatus.Offline) {
                                            Arguments args = new Arguments();
                                            args.Put("status", false);
                                            ClientLogin?.Invoke(this, new ClientEventArgs<IServerUser>(userLogin, ClientLoginStatus.Fail));
                                            userLogin.CallFunction("login", args, userLogin);
                                        }
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception e) {
                    this.Logger.Error(e.Message);
                }
            };
        }

        private void ForceLogout(IServerUser user)
        {
            this.AESKeyList.Remove((user as ITerminal).Guid);
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

                return false;
            }

            this.NetAddress = new IPEndPoint(IPAddress.Any, this.Config.Port);

            this.UdpClient = new UdpClient(this.NetAddress);

            this.Status = ServerStatus.Connected;

            this.Logger.Info(string.Format(this.lang.Port, this.Config.Port));

            this.Logger.Info(this.lang.ServerStart);

            SetListenHandler();

            StartListen();

            AppDomain.CurrentDomain.AssemblyResolve += ((sender, e) => {
                AssemblyName assemblyName = new AssemblyName(e.Name);
                FileInfo assemblyFile = new FileInfo(Path.Combine(GetFolderPath(FolderPath.PluginDependency), $"{assemblyName.Name}.dll"));

                return assemblyFile.Exists ? Assembly.LoadFrom(assemblyFile.FullName) : null;
            });

            this.ClientLogin += (sender, e) => {
                if (e.Status == ClientLoginStatus.Success) {
                    string text = string.Format(this.lang.ClientLogin, e.User.Name);

                    this.UserList.ForEach(x => x.Logger.Info(text));
                    this.Logger.Info(text);
                }
            };

            this.ClientLogout += (sender, e) => {
                if (e.Status == ClientLoginStatus.Success) {
                    string text = string.Format(this.lang.ClientLogout, e.User.Name);

                    this.UserList.ForEach(x => x.Logger.Info(text));
                    this.Logger.Info(text);
                }
            };

            LoadInternalCommand();
            LoadInternalPlugin();

            LoadTestCommand();

            LoadAllPlugin();

            ThreadStart ts = new ThreadStart(() => {
                while (this.Status == ServerStatus.Connected) {
                    List<IServerUser> playerLostConnectionList = this.UserList.Where(x => !x.Value.CheckConnection()).Select(x => x.Value).ToList();

                    Parallel.ForEach(playerLostConnectionList, x => ForceLogout(x));

                    Thread.Sleep(this.Config.Timeout);
                }
            });
            new Thread(ts).Start();

            return true;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void LoadTestCommand()
        {
            AddCommand(new Function()
            {
                Name = "test-rsa",
                Comment = "Test RSA",
                Func = (args, caller) => {
                    string input = args.ContainsKey("0") ? args["0"] : "TEST TEST TEST 1234567890 你好!";
                    byte[] outputData = RSAHelper.Encrypt(input, this.RSAKey);
                    byte[] outputData2 = RSAHelper.Decrypt(outputData, this.RSAKey);
                    string output = outputData2.GetString();

                    var dict = new Dictionary<string, string>();
                    dict.Add("input", input);
                    dict.Add("output", output);
                    caller.Logger.Info("Test RSA");
                    caller.Logger.Info(dict);

                    return 0;
                }
            });
        }

        private void LoadInternalCommand()
        {
            /*
             * {"Call":"command","Args":{"command":"cmd arg1 arg2 ..."}}
             */
            Function functionCommand = new Function()
            {
                Name = "command",
                Comment = null,
                Func = (args, caller) => {
                    if (caller.Type.In(CallerType.Client)) {
                        IServerUser user = (IServerUser)caller;

                        string command = args.GetString("command");
                        if (!string.IsNullOrWhiteSpace(command)) {
                            HandleCommand(command, caller);
                        }
                    }
                    return 0;
                }
            };
            AddFunction(functionCommand);

            Function commandExit = new Function()
            {
                Name = "exit",
                Comment = "Exit",
                Func = (args, caller) => {
                    if (caller.Type.In(CallerType.Console)) {
                        Stop(caller);
                    }
                    return 0;
                }
            };
            AddCommand(commandExit);

            Function commandSave = new Function()
            {
                Name = "save",
                Comment = "Save to config.json",
                Func = (args, caller) => {
                    SaveConfig(caller);

                    Parallel.ForEach(this.pluginList.Values, x => SavePluginConfig(x));

                    return 0;
                }
            };
            AddCommand(commandSave);

            Function commandLoad = new Function()
            {
                Name = "load",
                Comment = "Load from config.json",
                Func = (args, caller) => {
                    LoadConfig(caller);
                    return 0;
                }
            };
            AddCommand(commandLoad);

            Function functionHeartbeat = new Function()
            {
                Name = "heartbeat",
                Func = (args, caller) => {
                    return 0;
                }
            };

            AddFunction(functionHeartbeat);

            Function commandHistory = new Function()
            {
                Name = "history",
                Comment = "Show history",
                Func = (args, caller) => {
                    const int MaxShowHistory = 16;

                    caller.Logger.Info($"History");
                    int skip = Math.Max(this.History.Count - MaxShowHistory, 0);
                    var historyList = this.History.Skip(skip).Select((item, index) => $"{ skip + index + 1} {item}");

                    caller.Logger.Info(historyList);

                    return 0;
                }
            };

            AddCommand(commandHistory);

            Function commandHelp = new Function()
            {
                Name = "help",
                Comment = "Show help",
                Func = (args, caller) => {
                    if (args.ContainsKey("0")) {
                        string name = args.GetString("0");
                        if (this.CommandList.ContainsKey(name)) {
                            IFunction item = this.CommandList[name];
                            caller.Logger.Info($"Help - {item.Name}");

                            if (!item.Comment.IsNullOrEmpty()) {
                                var dict = new Dictionary<string, string>();

                                dict.Add("Comment", item.Comment);
                                dict.Add("Usage", item.Name);

                                caller.Logger.Info(dict);
                            }
                        }
                    } else {
                        caller.Logger.Info("Help");
                        var dict = new Dictionary<string, string>();

                        foreach (var item in this.CommandList) {
                            if (item.Value.Comment.IsNullOrEmpty()) {
                                dict.Add(item.Key, string.Empty);
                            } else {
                                dict.Add(item.Key, item.Value.Comment);
                            }
                        }
                        caller.Logger.Info(dict);
                    }
                    return 0;
                }
            };

            AddCommand(commandHelp);
        }

        private void LoadInternalPlugin()
        {
            LoadPlugin(new Plugin.Base());
            LoadPlugin(new Plugin.ServerInfo());
        }
    }
}