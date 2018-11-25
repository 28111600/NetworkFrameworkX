using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    public sealed class Server : Server<ServerConfig>
    {
    }

    public partial class Server<TConfig> : LocalCallable, IServer where TConfig : IServerConfig, new()
    {
        private const string FILE_CONFIG = "config.json";

        private static readonly object _lockObject = new object();

        public CallerType Type { get; } = CallerType.Console;

        private TcpServer TcpServer { get; set; }

        public string Guid { get; set; } = null;

        public string Name { get; set; } = "NetworkFrameworkXServer";

        internal event EventHandler<DataReceivedEventArgs> DataReceived;

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

        internal Dictionary<string, TcpClient> TcpClientList { get; private set; } = new Dictionary<string, TcpClient>();

        public ISerialzation<string> JsonSerialzation { get; private set; } = new JsonSerialzation();

        public ISerialzation<IEnumerable<byte>> BinarySerialzation { get; private set; } = new BinarySerialzation();

        public string[] GetCommandName() => this.CommandList.Keys.ToArray();

        public string[] GetHistory() => this.History.ToArray();

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
            string path = GetFilePath(FilePath.Keys);

            if (!File.Exists(path) || generate) {
                this.Logger.Info(this.lang.GenerateKeys);
                this.RSAKey = RSAKey.Generate();
                File.WriteAllText(path, this.RSAKey.Keys.GetBase64String());
            } else {
                try {
                    string keys = File.ReadAllText(path);
                    this.RSAKey = new RSAKey() { Keys = keys.FromBase64String() };
                    this.RSAKey.GeneratePublicKey();
                } catch (Exception) {
                    // 密钥非法时重新生成
                    LoadKey(true);
                }
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

                case FolderPath.PluginConfig:
                    return Path.Combine(GetFolderPath(FolderPath.Config), "plugin");

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
                    return Path.Combine(GetFolderPath(FolderPath.Config), FILE_CONFIG);

                case FilePath.History:
                    return Path.Combine(GetFolderPath(FolderPath.Config), "history.txt");

                case FilePath.Keys:
                    return Path.Combine(GetFolderPath(FolderPath.Config), "keys.txt");

                default:
                    return null;
            }
        }

        public void InitializeCore()

        {
            this.Logger.Info(this.lang.Initializing);
            foreach (FolderPath item in Enum.GetValues(typeof(FolderPath))) {
                DirectoryInfo folder = new DirectoryInfo(GetFolderPath(item));
                if (!folder.Exists) { folder.Create(); }
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

            UnLoadAllPlugin();

            this.TcpServer?.Stop();

            lock (_lockObject) {
                this.LogWriter?.Close();
                this.LogWriter = null;
            }

            AfterStop?.Invoke(this, new EventArgs());
        }

        private void SetListenHandler()
        {
            this.TcpServer.OnClientStart += (sender0, e0) => {
                e0.TcpClient.OnReceive += this.HandleMessage;
            };

            this.TcpServer.Start();
        }

        public void ForceLogout(IServerUser user)
        {
            this.AESKeyList.Remove(user.Guid);
            user.LostConnection();
            this.Logger.Info(string.Format(this.lang.ClientLostConnection, user.Name));
            ClientLogout?.Invoke(this, new ClientEventArgs<IServerUser>(user, ClientLoginStatus.Success));
        }

        public bool Start()
        {
            this.Logger.Info($"------{this.Name}------");

#if DEBUG
            this.Logger.Warning("#### DEBUG MODE ####");
#endif

            InitializeCore();

            LoadConfig();

            LoadKey();

            if (this.Config.Log) {
                string pathLog = Path.Combine(GetFolderPath(FolderPath.Log), $"{Utility.GetDateTimeStringForFileName(DateTime.Now)}.log");
                this.LogWriter = new StreamWriter(pathLog, true) { AutoFlush = true };
            }

            this.History.Path = GetFilePath(FilePath.History);
            this.History.Load();

            InitializeLanguage();

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

            InitializeCommand();

            InitializePlugin();

            ThreadStart ts = new ThreadStart(() => {
                while (this.Status == ServerStatus.Connected) {
                    List<IServerUser> playerLostConnectionList = this.UserList.Where(x => !x.Value.CheckConnection()).Select(x => x.Value).ToList();

                    playerLostConnectionList.ForEach(x => ForceLogout(x));
                    if (playerLostConnectionList.Count > 0) { this.Logger.Debug($"ForceLogout: {playerLostConnectionList.Count}"); }

                    Thread.Sleep(this.Config.Timeout);
                }
            });
            new Thread(ts).Start();

            return true;
        }
    }
}