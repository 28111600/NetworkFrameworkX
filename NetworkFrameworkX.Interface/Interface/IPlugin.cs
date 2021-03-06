using System;
using System.Collections.Generic;

namespace NetworkFrameworkX.Interface
{
    [Serializable]
    public sealed class PluginConfig : MarshalByRefObject
    {
        public bool Enabled { get; set; } = false;

        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }

    public interface IPlugin
    {
        IServer Server { get; set; }

        string Name { get; }

        PluginConfig Config { get; }

        void OnLoad();

        void OnDestroy();

        string SerializeConfig();

        void DeserializeConfig(string text);

        FunctionInfoCollection FunctionInfoList { get; }

        FunctionInfoCollection CommandInfoList { get; }

        string[] FunctionList { get; }

        string[] CommandList { get; }

        int CallFunction(string name, IArguments args, ICaller caller);

        int CallCommand(string name, IArguments args, ICaller caller);
    }
}