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
        IServerConfig Config { get; }

        IUserCollection<IServerUser> UserList { get; }

        void ForceLogout(IServerUser user);

        IList<string> PluginList { get; }

        long Traffic_In { get; }

        long Traffic_Out { get; }

        FunctionCollection FunctionTable { get; }

        FunctionCollection CommandTable { get; }

        int CallCommand(string name, IArguments args, ICaller caller = null);

        int CallCommand(string name, ICaller caller = null);

        string GetFolderPath(FolderPath path);

        ISerialzation<string> JsonSerialzation { get; }
    }
}