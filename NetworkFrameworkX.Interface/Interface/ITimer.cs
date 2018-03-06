using System;

namespace NetworkFrameworkX.Interface
{
    public interface ITimer
    {
        TimeSpan TimeSpan { get; set; }

        TimeSpan Interval { get; set; }

        EventHandler<ElapsedEventArgs> Func { get; set; }

        bool Enabled { get; set; }
    }
}