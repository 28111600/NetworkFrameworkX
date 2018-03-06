using System.Collections.Generic;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server.Plugin
{
    internal sealed class Base : IPlugin
    {
        private const string ServerName = "Server";

        public string Name => "Base";

        public IServer Server { get; set; }

        public PluginConfig Config { get; private set; } = new PluginConfig();

        public string SerializeConfig() => null;

        public void DeserializeConfig(string text)
        {
        }

        public void OnDestory()
        {
        }

        public void OnLoad()
        {
            Function commandSay = new Function()
            {
                Name = "say",
                Comment = "Say something",
                Func = (args, caller) => {
                    if (args.ContainsKey("0")) {
                        string name = caller.Type.In(CallerType.Console) ? ServerName : ((caller as IServerUser)?.Name ?? "unknwon");
                        string say = args.GetString("args");
                        string text = $"{name}: {say}";

                        this.Server.Logger.Info(text);
                        this.Server.UserList.ForEach(x => x.Logger.Info(text));
                    }

                    return 0;
                }
            };
            this.Server.AddCommand(commandSay);

            Function commandLs = new Function()
            {
                Name = "ls",
                Comment = "List users",
                Func = (args, caller) => {
                    caller.Logger.Info("List users");

                    var dict = new Dictionary<string, string>();

                    this.Server.UserList.ForEach(x => {
                        dict.Add(x.Name, x.NetAddress.ToString());
                    });
                    caller.Logger.Info(dict);

                    return 0;
                }
            };
            this.Server.AddCommand(commandLs);
        }
    }
}