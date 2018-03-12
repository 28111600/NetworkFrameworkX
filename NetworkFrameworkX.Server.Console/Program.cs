using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            LoadTestCommand(server);

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

        [Conditional("DEBUG")]
        private static void LoadTestCommand(IServer server)
        {
            server.Logger.Info("+------------------------------+");
            server.Logger.Info("| Day     | Meal     | Price   |");
            server.Logger.Info("|---------|----------|---------|");
            server.Logger.Info("| Monday  | pasta    | $6      |");
            server.Logger.Info("| Tuesday | chicken  | $8      |");
            server.Logger.Info("+------------------------------+");

            server.AddCommand(new Function()
            {
                Name = "test-color",
                Comment = "Color output test",
                Func = (args, caller) => {
                    const string colorCharList = "0123456789ABCDEF";
                    const string testText = "The quick brown fox jumps over the lazy dog";

                    var dict = new Dictionary<string, string>();
                    for (int i = 0; i < colorCharList.Length; i++) {
                        dict.Add(i.ToString(), $"&{colorCharList[i]}{testText}");
                    }

                    caller.Logger.Info(dict);
                    return 0;
                }
            });

            server.AddCommand(new Function()
            {
                Name = "test-ascii-art",
                Comment = "generation",
                Func = (args, caller) => {
                    const string rose = " &4-<&2@";

                    string[] cat = new string[]
                    {   @"     _                                ",
                        @"    { \,'     )\._.,--....,'``.       ",
                        @"   {_`/      /,   _.. \   _\  (`._ ,. ",
                        @"            `._.-(,_..'--(,_..'`-.;.' ",
                        @"                                      "
                    };

                    for (int i = 0; i < cat.Length; i++) {
                        caller.Logger.Info(cat[i]);
                    }

                    const int x = 8, y = x;
                    for (int i = 0; i < x; i++) {
                        StringBuilder sb = new StringBuilder();
                        for (int j = 0; j < y; j++) {
                            sb.Append(rose);
                        }
                        if (i % 2 == 0) {
                            sb.Insert(0, Utility.StringWhiteSpace, 2);
                        }
                        caller.Logger.Info(sb);
                    }

                    return 0;
                }
            });

            server.AddCommand(new Function()
            {
                Name = "var",
                Comment = "Various",
                Func = (x, y) => {
                    var variables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
                    var dic = new Dictionary<string, string>();
                    foreach (var key in variables.Keys) {
                        dic.Add(key.ToString(), variables[key].ToString());
                    }
                    y.Logger.Info(dic);

                    return 0;
                }
            });
        }
    }
}