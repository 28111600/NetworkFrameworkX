using System.Collections.Generic;
using System.Text;

namespace NetworkFrameworkX.Interface
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public interface ILogger
    {
        void Info(string name, string text);

        void Info(string text);

        void Info(string name, IEnumerable<string> text);

        void Info(IEnumerable<string> text);

        void Info(string name, StringBuilder text);

        void Info(StringBuilder text);

        void Info(string name, IEnumerable<KeyValuePair<string, string>> text);

        void Info(IEnumerable<KeyValuePair<string, string>> text);

        void Debug(string name, string text);

        void Debug(string text);

        void Debug(string name, IEnumerable<string> text);

        void Debug(IEnumerable<string> text);

        void Debug(string name, StringBuilder text);

        void Debug(StringBuilder text);

        void Debug(string name, IEnumerable<KeyValuePair<string, string>> text);

        void Debug(IEnumerable<KeyValuePair<string, string>> text);

        void Warning(string name, string text);

        void Warning(string text);

        void Warning(string name, IEnumerable<string> text);

        void Warning(IEnumerable<string> text);

        void Warning(string name, StringBuilder text);

        void Warning(StringBuilder text);

        void Warning(string name, IEnumerable<KeyValuePair<string, string>> text);

        void Warning(IEnumerable<KeyValuePair<string, string>> text);

        void Error(string name, string text);

        void Error(string text);

        void Error(string name, IEnumerable<string> text);

        void Error(IEnumerable<string> text);

        void Error(string name, StringBuilder text);

        void Error(StringBuilder text);

        void Error(string name, IEnumerable<KeyValuePair<string, string>> text);

        void Error(IEnumerable<KeyValuePair<string, string>> text);
    }
}