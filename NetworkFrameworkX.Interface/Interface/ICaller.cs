namespace NetworkFrameworkX.Interface
{
    public enum CallerType
    {
        Unknown,
        Client,
        Console
    }

    public interface ITcpSender
    {
    }

    public interface ICaller
    {
        CallerType Type { get; }

        int CallFunction(string name, IArguments args = null);

        ILogger Logger { get; }
    }
}