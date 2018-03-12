using System.Net;

namespace NetworkFrameworkX.Interface
{
    public enum TerminalStatus
    {
        Disconnected,
        AskKey,
        Connected
    }

    public interface ITerminal
    {
        string Guid { get; }
    }
}