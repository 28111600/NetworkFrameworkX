using System.Net.Sockets;

namespace NetworkFrameworkX.Interface
{
    public enum CallerType
    {
        Unknown,
        Client,
        Console
    }

    public interface IUdpSender
    {
        UdpClient UdpClient { get; }

        void Send(string text, ITerminal terminal);

        void Send(byte[] bytes, ITerminal terminal);
    }

    public interface ICaller
    {
        CallerType Type { get; }

        int CallFunction(string name, IArguments args = null);

        ILogger Logger { get; }
    }
}