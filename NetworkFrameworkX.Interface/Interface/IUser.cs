using System;
using System.Net;

namespace NetworkFrameworkX.Interface
{
    public enum UserStatus
    {
        Offline,
        Online
    }

    public interface IUserCollection<T> : IStringKeyCollection<T> where T : IUser
    {
    }

    public interface IUser
    {
        string Guid { get; set; }

        DateTime LoginTime { get; }

        string Name { get; set; }

        bool IsAdmin { get; set; }

        UserStatus Status { get; set; }
    }

    public interface IServerUser : IUser, ICaller
    {
        IPEndPoint NetAddress { get; set; }

        DateTime LastHeartBeat { get; set; }

        bool CheckConnection();

        void LostConnection();

        void RefreshHeartBeat();
    }
}