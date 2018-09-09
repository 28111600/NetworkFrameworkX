using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Share
{
    internal class Logger : MarshalByRefObject, ILogger
    {
        private event EventHandler<LogEventArgs> Log;

        public Logger(EventHandler<LogEventArgs> handler)
        {
            Log += handler;
        }

        private void OnLog(LogLevel level, string name, string text)
        {
#if DEBUG
            if (true) {
#else
            if (level != LogLevel.Debug) {
#endif
                Log?.Invoke(this, new LogEventArgs(level, name, text));
            }
        }

        private void OnLog(LogLevel level, string name, IEnumerable<string> text)
        {
            if (text.Count() == 0) {
                OnLog(level, name, $"- Empty List");
                return;
            };

            OnLog(level, name, text.Select(x => $"- {x}").Join(Utility.StringNewLine));
        }

        private void OnLog(LogLevel level, string name, IEnumerable<KeyValuePair<string, string>> text)
        {
            const int TabLength = 4;
            const int SpaceLength = 2;

            if (text.Count() == 0) {
                OnLog(level, name, $"- Empty List");
                return;
            };

            //需处理双字节字符
            int keyMaxLength = text.Max(x => Encoding.Default.GetByteCount(x.Key ?? string.Empty));
            int totalMaxLength = (int)Math.Ceiling(keyMaxLength / (double)TabLength) * TabLength;

            totalMaxLength = Math.Max(totalMaxLength, keyMaxLength + SpaceLength);

            OnLog(level, name, text.Select(x => $"{x.Key}{ new string(Utility.CharWhiteSpace, totalMaxLength - x.Key.Length)}: {x.Value}").ToArray());
        }

        private void OnLog(LogLevel level, string name, IEnumerable<KeyValuePair<string, string>> text, string title)
        {
            OnLog(level, name, title);
            OnLog(level, name, text);
        }

        private void OnLog(LogLevel level, IEnumerable<KeyValuePair<string, string>> text, string title) => OnLog(level, null, text, title);

        private void OnLog(LogLevel level, string name, IEnumerable<string> text, string title)
        {
            OnLog(level, name, title);
            OnLog(level, name, text);
        }

        private void OnLog(LogLevel level, string name, StringBuilder text) => OnLog(level, name, text.ToString());

        private void OnLog(LogLevel level, IEnumerable<string> text, string title) => OnLog(level, null, text, title);

        public void Debug(string name, string text) => OnLog(LogLevel.Debug, name, text);

        public void Debug(string text) => Debug(null, text);

        public void Debug(string name, IEnumerable<string> text) => OnLog(LogLevel.Debug, name, text);

        public void Debug(IEnumerable<string> text) => Debug(null, text);

        public void Debug(string name, StringBuilder text) => OnLog(LogLevel.Debug, name, text);

        public void Debug(StringBuilder text) => Debug(null, text);

        public void Debug(string name, IEnumerable<KeyValuePair<string, string>> text) => OnLog(LogLevel.Debug, name, text);

        public void Debug(IEnumerable<KeyValuePair<string, string>> text) => Debug(null, text);

        public void Error(string name, string text) => OnLog(LogLevel.Error, name, text);

        public void Error(string text) => Error(null, text);

        public void Error(string name, StringBuilder text) => OnLog(LogLevel.Error, name, text);

        public void Error(StringBuilder text) => Error(null, text);

        public void Error(string name, IEnumerable<string> text) => OnLog(LogLevel.Error, name, text);

        public void Error(IEnumerable<string> text) => Error(null, text);

        public void Error(string name, IEnumerable<KeyValuePair<string, string>> text) => OnLog(LogLevel.Error, name, text);

        public void Error(IEnumerable<KeyValuePair<string, string>> text) => Error(null, text);

        public void Info(string name, string text) => OnLog(LogLevel.Info, name, text);

        public void Info(string text) => Info(null, text);

        public void Info(string name, IEnumerable<string> text) => OnLog(LogLevel.Info, name, text);

        public void Info(IEnumerable<string> text) => Info(null, text);

        public void Info(string name, StringBuilder text) => OnLog(LogLevel.Info, name, text);

        public void Info(StringBuilder text) => Info(null, text);

        public void Info(string name, IEnumerable<KeyValuePair<string, string>> text) => OnLog(LogLevel.Info, name, text);

        public void Info(IEnumerable<KeyValuePair<string, string>> text) => Info(null, text);

        public void Warning(string name, string text) => OnLog(LogLevel.Warning, name, text);

        public void Warning(string text) => Warning(null, text);

        public void Warning(string name, IEnumerable<string> text) => OnLog(LogLevel.Warning, name, text);

        public void Warning(IEnumerable<string> text) => Warning(null, text);

        public void Warning(string name, StringBuilder text) => OnLog(LogLevel.Warning, name, text);

        public void Warning(StringBuilder text) => Warning(null, text);

        public void Warning(string name, IEnumerable<KeyValuePair<string, string>> text) => OnLog(LogLevel.Warning, name, text);

        public void Warning(IEnumerable<KeyValuePair<string, string>> text) => Warning(null, text);
    }
}