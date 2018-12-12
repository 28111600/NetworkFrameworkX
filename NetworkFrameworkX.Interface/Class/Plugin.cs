using System;
using System.Linq;

namespace NetworkFrameworkX.Interface
{
    public abstract class Plugin : MarshalByRefObject, IPlugin
    {
        public virtual string Name { get; protected set; }

        public IServer Server { get; set; }

        public PluginConfig Config { get; protected set; } = new PluginConfig();

        public string[] FunctionList => this.FunctionTable.Select(x => x.Value.Name).ToArray();

        public string[] CommandList => this.CommandTable.Select(x => x.Value.Name).ToArray();

        public FunctionCollection FunctionTable { get; protected set; } = new FunctionCollection();

        public FunctionCollection CommandTable { get; protected set; } = new FunctionCollection();

        public FunctionInfoCollection FunctionInfoList => (FunctionInfoCollection)this.FunctionTable;

        public FunctionInfoCollection CommandInfoList => (FunctionInfoCollection)this.CommandTable;

        public int CallFunction(string name, IArguments args, ICaller caller) => this.FunctionTable.Call(name, args, caller);

        public int CallCommand(string name, IArguments args, ICaller caller) => this.CommandTable.Call(name, args, caller);

        public virtual string SerializeConfig() => this.Server.JsonSerialzation.Serialize(this.Config);

        public virtual void DeserializeConfig(string text)
        {
            this.Config = this.Server.JsonSerialzation.Deserialize<PluginConfig>(text) ?? new PluginConfig();
        }

        public virtual void OnDestroy()
        {
        }

        public virtual void OnLoad()
        {
        }
    }
}