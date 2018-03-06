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
        IPEndPoint NetAddress { get; }

        string Guid { get; }
    }
}