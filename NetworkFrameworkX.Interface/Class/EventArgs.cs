using System;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Interface
{
    public class ElapsedEventArgs : EventArgs
    {
        public TimeSpan Elapsed { get; private set; }

        public ElapsedEventArgs(TimeSpan elapsed)
        {
            this.Elapsed = elapsed;
        }
    }

    public class StatusChangedEventArgs : EventArgs
    {
        public ServerStatus Status { get; private set; }

        public StatusChangedEventArgs(ServerStatus status)
        {
            this.Status = status;
        }
    }

    public class ClientEventArgs<T> : EventArgs where T : IUser
    {
        public T User { get; private set; }

        public ClientEventArgs(T user)
        {
            this.User = user;
        }
    }

    public class ClientPreLoginEventArgs<T> : EventArgs where T : IUser
    {
        public T User { get; set; }
        public IArguments Args { get; set; }

        public ClientPreLoginEventArgs(T user, IArguments args)
        {
            this.User = user;
            this.Args = args;
        }
    }

    public class LogEventArgs : EventArgs
    {
        public LogLevel Level { get; private set; }

        public string Name { get; private set; }

        public string Text { get; private set; }

        public string LevelText { get; private set; }

        public LogEventArgs(LogLevel level, string name, string text)
        {
            this.Level = level;
            this.Name = name;
            this.Text = text;
            this.LevelText = level.ToText();
        }
    }

    public class SocketExcptionEventArgs : EventArgs
    {
        public string Guid { get; private set; }

        public SocketExcptionEventArgs(string guid)
        {
            this.Guid = guid;
        }
    }
}