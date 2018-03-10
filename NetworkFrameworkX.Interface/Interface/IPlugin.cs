using System;

namespace NetworkFrameworkX.Interface
{
    [Serializable]
    public class PluginConfig
    {
        public bool Enabled { get; set; } = true;
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
    }
}