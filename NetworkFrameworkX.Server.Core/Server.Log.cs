using System;
using System.Collections.Generic;
using System.IO;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    public partial class Server<TConfig>
    {
        public event EventHandler<LogEventArgs> Log;

        private List<string> _logToWrite = new List<string>();

        private static string GetLogString(LogLevel level, string name, string text) => string.Format($"[{{0}} {{1}}]: {{2}}", Utility.GetTimeString(DateTime.Now), level.ToText(), text);

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

        private StreamWriter LogWriter = null;
    }
}