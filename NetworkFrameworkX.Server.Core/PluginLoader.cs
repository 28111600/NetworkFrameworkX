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
            this.Loaded = this.RemoteTypeLoader.InitTypeLoader(assemblyPath);

            this.RemotePlugin = this.RemoteTypeLoader as RemotePluginLoader;
            this.AssemblyPath = assemblyPath;
        }

        public PluginLoader(IPlugin plugin)
        {
            this.RemotePlugin = plugin;
            this.Loaded = true;
        }

        public IServer Server { get => this.RemotePlugin.Server; set => this.RemotePlugin.Server = value; }

        public string Name => this.RemotePlugin.Name;

        public PluginConfig Config => this.RemotePlugin.Config;

        public string[] FunctionList => this.RemotePlugin.FunctionList;

        public string[] CommandList => this.RemotePlugin.CommandList;

        public FunctionInfoCollection FunctionInfoList => this.RemotePlugin.FunctionInfoList;

        public FunctionInfoCollection CommandInfoList => this.RemotePlugin.CommandInfoList;

        public void DeserializeConfig(string text) => this.RemotePlugin.DeserializeConfig(text);

        public void OnDestroy() => this.RemotePlugin.OnDestroy();

        public void OnLoad() => this.RemotePlugin.OnLoad();

        public int CallFunction(string name, IArguments args, ICaller caller) => this.RemotePlugin.CallFunction(name, args, caller);

        public int CallCommand(string name, IArguments args, ICaller caller) => this.RemotePlugin.CallCommand(name, args, caller);

        public string SerializeConfig() => this.RemotePlugin.SerializeConfig();

        public override bool Unload()
        {
            bool result = base.Unload();

            if (result) {
                this.Loaded = false;
            }
            return result;
        }

        public static bool ContainsType(string assemblyPath) => TryGetPluginName(assemblyPath, out string name);

        public static bool TryGetPluginName(string assemblyPath, out string name)
        {
            PluginLoader loader = new PluginLoader(assemblyPath);
            bool value = loader.Loaded;
            name = loader?.Name;
            loader.Unload();
            return value;
        }
    }

    internal class RemotePluginLoader : RemoteTypeLoader, IPlugin
    {
        public IPlugin Plugin { get; private set; }

        public IServer Server { get => this.Plugin.Server; set => this.Plugin.Server = value; }

        public string Name => this.Plugin?.Name;

        public PluginConfig Config => this.Plugin.Config;

        public string[] FunctionList => this.Plugin.FunctionList;

        public string[] CommandList => this.Plugin.CommandList;

        public FunctionInfoCollection FunctionInfoList => this.Plugin.FunctionInfoList;

        public FunctionInfoCollection CommandInfoList => this.Plugin.CommandInfoList;

        public void DeserializeConfig(string text) => this.Plugin.DeserializeConfig(text);

        public void OnDestroy() => this.Plugin.OnDestroy();

        public void OnLoad() => this.Plugin.OnLoad();

        public int CallFunction(string name, IArguments args, ICaller caller) => this.Plugin.CallFunction(name, args, caller);

        public int CallCommand(string name, IArguments args, ICaller caller) => this.Plugin.CallCommand(name, args, caller);

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