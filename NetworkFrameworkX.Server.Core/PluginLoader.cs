using System.Reflection;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    internal class PluginLoader : TypeLoader, IPlugin
    {
        public IPlugin RemotePlugin { get; private set; }

        public bool Loaded { get; private set; } = false;

        public string[] AssemblyResolvePath { get; private set; }

        public PluginLoader(string assemblyPath, string[] assemblyResolvePath = null)
        {
            this.AssemblyResolvePath = assemblyResolvePath;
            this.RemoteTypeLoader = CreateRemoteTypeLoader(typeof(RemotePluginLoader), assemblyResolvePath);
            this.RemoteTypeLoader.InitTypeLoader(assemblyPath);

            this.RemotePlugin = this.RemoteTypeLoader as RemotePluginLoader;
            this.AssemblyPath = assemblyPath;
            this.Loaded = true;
        }

        public PluginLoader(IPlugin plugin)
        {
            this.RemotePlugin = plugin;
            this.Loaded = true;
        }

        public IServer Server { get => this.RemotePlugin.Server; set => this.RemotePlugin.Server = value; }

        public string Name => this.RemotePlugin.Name;

        public PluginConfig Config => this.RemotePlugin.Config;

        public void DeserializeConfig(string text) => this.RemotePlugin.DeserializeConfig(text);

        public void OnDestroy() => this.RemotePlugin.OnDestroy();

        public void OnLoad() => this.RemotePlugin.OnLoad();

        public string SerializeConfig() => this.RemotePlugin.SerializeConfig();

        public override bool Unload()
        {
            bool result = base.Unload();

            if (result) {
                this.Loaded = false;
            }
            return result;
        }
    }

    internal class RemotePluginLoader : RemoteTypeLoader, IPlugin
    {
        public IPlugin Plugin { get; private set; }

        public IServer Server { get => this.Plugin.Server; set => this.Plugin.Server = value; }

        public string Name => this.Plugin.Name;

        public PluginConfig Config => this.Plugin.Config;

        public void DeserializeConfig(string text) => this.Plugin.DeserializeConfig(text);

        public void OnDestroy() => this.Plugin.OnDestroy();

        public void OnLoad() => this.Plugin.OnLoad();

        public string SerializeConfig() => this.Plugin.SerializeConfig();

        protected override Assembly LoadAssembly(string assemblyPath)
        {
            Assembly asm = base.LoadAssembly(assemblyPath);

            if (TryGetInstance(asm, out IPlugin instance)) {
                this.Plugin = instance;
                return asm;
            } else {
                return null;
            }
        }
    }
}