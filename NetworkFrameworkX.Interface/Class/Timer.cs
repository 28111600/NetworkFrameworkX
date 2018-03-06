using System;

namespace NetworkFrameworkX.Interface
{
    public sealed class Timer : ITimer
    {
        public TimeSpan TimeSpan { get; set; } = TimeSpan.Zero;

        public TimeSpan Interval { get; set; } = TimeSpan.Zero;

        public EventHandler<ElapsedEventArgs> Func { get; set; }

        public bool Enabled { get; set; } = true;
    }
}