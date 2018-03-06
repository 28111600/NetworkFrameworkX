namespace NetworkFrameworkX.Interface
{
    public interface IServerConfig
    {
        string ServerName { get; set; }

        int Port { get; set; }

        int Timeout { get; set; }

        bool Log { get; set; }

        string Language { get; set; }
    }
}