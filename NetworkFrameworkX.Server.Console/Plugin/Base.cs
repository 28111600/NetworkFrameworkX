using System;
using System.Collections.Generic;
using System.Linq;
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

        public void OnDestroy()
        {
        }

        public void OnLoad()
        {
            Function commandSay = new Function()
            {
                Name = "say",
                Comment = "Say something",
                Func = (args, caller) => {
                    string say = args.GetString("args");
                    if (!say.IsNullOrEmpty()) {
                        string name = caller.Type.In(CallerType.Console) ? ServerName : ((caller as IServerUser)?.Name ?? "unknwon");
                        string text = $"{name}: {say}";

                        this.Server.Logger.Info(text);
                        this.Server.UserList.ForEach(x => x.Logger.Info(text));
                    }

                    return 0;
                }
            };
            this.Server.AddCommand(commandSay);

            Function commandWho = new Function()
            {
                Name = "w",
                Comment = "Show who is logged on",
                Func = (args, caller) => {
                    var list = this.Server.UserList
                        .Select((x) => new KeyValuePair<string, string>(x.Value.Name, $"{x.Value.LoginTime.GetDateTimeString()} ({x.Value.NetAddress})"));

                    caller.Logger.Info("Logged on");
                    caller.Logger.Info(list);

                    return 0;
                }
            };
            this.Server.AddCommand(commandWho);

            Function commandPing = new Function()
            {
                Name = "ping",
                Comment = "Ping",
                Func = (x, caller) => {
                    if (caller.Type == CallerType.Client) {
                        Arguments args = new Arguments();
                        args.Put("tick", Environment.TickCount);
                        caller.CallFunction("ping", args);
                        return 0;
                    } else {
                        caller.Logger.Error("Just for client");
                        return -1;
                    }
                }
            };
            this.Server.AddCommand(commandPing);

            Function funcPing = new Function()
            {
                Name = "ping",
                Func = (x, caller) => {
                    int tick = x.GetInt("tick");
                    if (tick > 0) {
                        int timespan = Environment.TickCount - tick;
                        caller.Logger.Info($"Ping : {timespan} ms");
                        return 0;
                    } else {
                        return -1;
                    }
                }
            };
            this.Server.AddFunction(funcPing);
        }
    }
}