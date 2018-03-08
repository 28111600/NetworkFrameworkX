using System;
using System.Collections.Generic;
using System.Linq;
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

        public CallerType Type { get; } = CallerType.Client;

        public override IUdpSender UdpSender => this.Server;

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

            CallFunction("writeline", Args, this);
        }

        public void RefreshHeartBeat() => this.LastHeartBeat = DateTime.UtcNow;
    }

    public class UserCollection<T> : Dictionary<string, T>, IUserCollection<T> where T : IUser
    {
        public void ForEach(Action<T> action)
        {
            foreach (T item in this.Values) {
                action?.Invoke(item);
            }
        }

        public bool All(Func<T, bool> match) => this.Values.All(match);
    }
}