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
        PluginConfig,
        PluginDependency,
        Log
    }

    public enum FilePath
    {
        Config,
        History,
        Keys
    }

    public interface IArguments : IDictionary<string, string>
    {
        bool ContainsKey(params string[] key);

        void Put(string name, string value);

        void Put<T>(string name, T value) where T : IConvertible;

        string GetString(string name);

        int GetInt(string name);

        long GetLong(string name);

        float GetFloat(string name);

        bool GetBool(string name);
    }

    public interface IServer : ICaller
    {
        IServerConfig Config { get; set; }

        IUserCollection<IServerUser> UserList { get; }

        IList<string> PluginList { get; }

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