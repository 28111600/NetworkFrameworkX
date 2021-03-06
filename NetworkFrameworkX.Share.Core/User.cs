using System;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Share
{
    public class User : IUser
    {
        public User()
        {
        }

        public string Guid { get; set; } = null;

        public UserStatus Status { get; set; } = UserStatus.Offline;

        public bool IsAdmin { get; set; } = false;

        public string Name { get; set; } = "Username";

        public long TimeStamp { get; set; }

        public DateTime LoginTime { get; internal set; }

        public CallerType Type { get; } = CallerType.Client;
    }

    public class ServerUser : RemoteCallable, IServerUser
    {
        public ServerUser()
        {
        }

        public UserStatus Status { get; set; } = UserStatus.Offline;

        public bool IsAdmin { get; set; } = false;

        public DateTime LastHeartBeat { get; set; }

        public string Name { get; set; } = "Username";

        public IServer Server { get; set; }

        public long TimeStamp { get; set; }

        internal TcpClient _TcpClient = null;

        internal override TcpClient TcpClient => this._TcpClient;

        public DateTime LoginTime { get; internal set; }

        public CallerType Type { get; } = CallerType.Client;

        public int CallFunction(string name, IArguments args = null) => this.CallFunction(name, args ?? new Arguments(), this);

        public bool CheckConnection()
        {
            TimeSpan timeSpan = (DateTime.UtcNow - this.LastHeartBeat);
            return (timeSpan.TotalMilliseconds <= this.Server.Config.Timeout);
        }

        public void LostConnection() => this.Server.UserList.Remove(this.Guid);

        protected override void OnLog(LogLevel level, string name, string text)
        {
            Arguments Args = new Arguments();
            Args.Put("level", (int)level);
            Args.Put("name", name);
            Args.Put("text", text);

            this.CallFunction("log", Args);
        }

        public void RefreshHeartBeat() => this.LastHeartBeat = DateTime.UtcNow;
    }

    public class UserCollection<T> : StringKeyCollection<T>, IUserCollection<T> where T : IUser
    {
    }
}