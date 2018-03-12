using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;
using XDLib;

namespace NetworkFrameworkX.Server.Console
{
    internal class Program
    {
        private static XConsole XConsole = new XConsole() { Prefix = "Console>", Beep = true, StyleEscape = '&' };

        public static void Log(object sender, LogEventArgs e)
        {
            XConsole.Color foreColor = XConsole.Color.Reset;
            switch (e.Level) {
                case LogLevel.Debug:
                    foreColor = XConsole.Color.Blue;
                    break;

                case LogLevel.Info:
                    foreColor = XConsole.Color.Reset;
                    break;

                case LogLevel.Warning:
                    foreColor = XConsole.Color.Yellow;
                    break;

                case LogLevel.Error:
                    foreColor = XConsole.Color.Red;
                    break;
            }
            string reset = XConsole.ResetStyle();
            string style = XConsole.GetForeStyle(foreColor);
            StringBuilder write = new StringBuilder();
            if (string.IsNullOrWhiteSpace(e.Name)) {
                write.Append(e.Text.Split(Utility.CharNewLine)
                    .Select(x => string.Format($"[{{0}} {style}{{1}}{reset}]: {{2}}", Utility.GetTimeString(DateTime.Now), e.LevelText, x))
                    .Join(Environment.NewLine));
            } else {
                write.Append(e.Text.Split(Utility.CharNewLine)
                   .Select(x => string.Format($"[{{0}} {style}{{1}}{reset}][{{2}}]: {{3}}", Utility.GetTimeString(DateTime.Now), e.LevelText, e.Name, x))
                   .Join(Environment.NewLine));
            }

            XConsole.WriteLine(write);
            XConsole.Render(true, true);
        }

        private static void Main(string[] arguments)
        {
            Server<Config> server = new Server<Config>(arguments.Length > 0 ? arguments[0] : Environment.CurrentDirectory);

            server.Log += Log;

            server.ClientPreLogin += (sender, e) => {
                string username = e.Args.GetString("username");
                string password = e.Args.GetString("password");

                Config config = server.Config as Config;

                e.User.Name = username;

                if (config.Users.Any((x) => x.Username == username && x.Password == password)) {
                    e.User.Status = UserStatus.Online;
                } else {
                    e.User.Status = UserStatus.Offline;
                }
            };

            if (!System.Console.IsOutputRedirected) {
                System.Console.TreatControlCAsInput = true;
            }

            XConsole.OnAutoComplete += server.GetCommandName;
            XConsole.OnGetHistory += server.GetHistory;

            server.Start();

            LoadInternalCommand(server);
            LoadInternalPlugin(server);

            if (!System.Console.IsInputRedirected) {
                while (server != null && server.Status == ServerStatus.Connected) {
                    string input = XConsole.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input)) {
                        if (server.Status == ServerStatus.Connected) {
                            input = input.Trim();
                            server.HandleCommand(input, server);
                        }
                    }
                }
                System.Console.WriteLine();
            } else {
                while (server.Status == ServerStatus.Connected) {
                    System.Console.Read();
                }
            }
        }

        private static void LoadInternalCommand(Server<Config> server)
        {
            server.AddCommand(new Function()
            {
                Name = "exit",
                Comment = "Exit",
                Func = (args, caller) => {
                    if (caller.Type.In(CallerType.Console)) {
                        server.Stop(caller);
                    }
                    return 0;
                }
            });

            server.AddCommand(new Function()
            {
                Name = "save",
                Comment = "Save to config.json",
                Func = (args, caller) => {
                    server.SaveConfig(caller);

                    server.PluginList.ToList().ForEach(x => server.SavePluginConfig(x));

                    return 0;
                }
            });

            server.AddCommand(new Function()
            {
                Name = "load",
                Comment = "Load from config.json",
                Func = (args, caller) => {
                    server.LoadConfig(caller);
                    return 0;
                }
            });

            server.AddCommand(new Function()
            {
                Name = "history",
                Comment = "Show history",
                Func = (args, caller) => {
                    const int MaxShowHistory = 16;

                    caller.Logger.Info($"History");
                    int skip = Math.Max(server.GetHistory().Length - MaxShowHistory, 0);
                    var historyList = server.GetHistory().Skip(skip).Select((item, index) => $"{ skip + index + 1} {item}");

                    caller.Logger.Info(historyList);

                    return 0;
                }
            });

            server.AddCommand(new Function()
            {
                Name = "help",
                Comment = "Show help",
                Func = (args, caller) => {
                    if (args.ContainsKey("0")) {
                        string name = args.GetString("0");
                        if (server.CommandList.ContainsKey(name)) {
                            IFunction item = server.CommandList[name];
                            caller.Logger.Info($"Help - {item.Name}");

                            if (!item.Comment.IsNullOrEmpty()) {
                                var dict = new Dictionary<string, string>();

                                dict.Add("Comment", item.Comment);
                                dict.Add("Usage", item.Name);

                                caller.Logger.Info(dict);
                            }
                        }
                    } else {
                        caller.Logger.Info("Help");
                        var dict = new Dictionary<string, string>();

                        foreach (var item in server.CommandList) {
                            if (item.Value.Comment.IsNullOrEmpty()) {
                                dict.Add(item.Key, string.Empty);
                            } else {
                                dict.Add(item.Key, item.Value.Comment);
                            }
                        }
                        caller.Logger.Info(dict);
                    }
                    return 0;
                }
            });
        }

        private static void LoadInternalPlugin(Server<Config> server)
        {
            server.LoadPlugin(new Plugin.Base());
            server.LoadPlugin(new Plugin.ServerInfo());
        }
    }
}