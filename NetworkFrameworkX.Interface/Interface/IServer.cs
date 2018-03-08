using System;
using System.Collections.Generic;

namespace NetworkFrameworkX.Interface
{
    public enum ServerStatus
    {
        Connecting,
        Connected,
        Close
    }

    public enum FolderPath
    {
        Root,
        Config,
        Lang,
        Plugin,
        PluginDependency,
        Log
    }

    public enum FilePath
    {
        Config,
        History,
        Keys,
        PublicKey
    }

    public interface IArguments : IDictionary<string, string>
    {
        bool ContainsKey(params string[] key);

        void Put(string name, string value);

        void Put(string name, int value);

        void Put(string name, float value);

        void Put(string name, bool value);

        string GetString(string name);

        int GetInt(string name);

        float GetFloat(string name);

        bool GetBool(string name);
    }

    public interface IServer : ICaller, ITerminal, IUdpSender
    {
        IServerConfig Config { get; set; }

        IUserCollection<IServerUser> UserList { get; }

        long Traffic_In { get; }

        long Traffic_Out { get; }

        bool AddFunction(IFunction func);

        bool AddCommand(IFunction func);

        int CallCommand(string name, IArguments args, ICaller caller = null);

        int CallCommand(string name, ICaller caller = null);

        event EventHandler<ClientEventArgs<IServerUser>> ClientLogin;

        event EventHandler<ClientEventArgs<IServerUser>> ClientLogout;

        string GetFolderPath(FolderPath path);

        ISerialzation<string> JsonSerialzation { get; }
    }
}