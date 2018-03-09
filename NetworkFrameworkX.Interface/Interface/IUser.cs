using System;
using System.Collections.Generic;

namespace NetworkFrameworkX.Interface
{
    public enum UserStatus
    {
        Offline,
        Online
    }

    public interface IUserCollection<T> : IDictionary<string, T> where T : IUser
    {
        bool All(Func<T, bool> match);

        void ForEach(Action<T> action);
    }

    public interface IUser
    {
        string Guid { get; set; }

        long TimeStamp { get; set; }

        string Name { get; set; }

        bool IsAdmin { get; set; }

        UserStatus Status { get; set; }
    }

    public interface IServerUser : IUser, ICaller, ITerminal
    {
        DateTime LastHeartBeat { get; set; }

        bool CheckConnection();

        void LostConnection();

        void RefreshHeartBeat();
    }
}