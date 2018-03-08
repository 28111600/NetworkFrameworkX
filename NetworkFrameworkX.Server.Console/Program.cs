using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                write.Append(string.Format($"[{{0}} {style}{{1}}{reset}]: {{2}}", Utility.GetTimeString(DateTime.Now), e.LevelText, e.Text));
            } else {
                write.Append(string.Format($"[{{0}} {style}{{1}}{reset}][{{2}}]: {{3}}", Utility.GetTimeString(DateTime.Now), e.LevelText, e.Name, e.Text));
            }

            XConsole.WriteLine(write);
            XConsole.Render(true, true);
        }

        private static void Main(string[] arguments)
        {
            Server<Config> server = new Server<Config>(arguments.Length > 0 ? arguments[0] : Environment.CurrentDirectory);

            server.DataReceived += (sender, e) => {
#if false
                server.Logger.Debug($"Received: {e.IPAddress.ToString()} / {e.Text}");
#endif
            };

            server.Log += Log;

            server.ClientPreLogin += (sender, e) => {
                if ((server.Config as Config).Password == e.Args.GetString("password")) {
                    XConsole.WriteLine("Password checked.");
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

            string[] nameList = new string[] { "last", "lesskey", "linux64", "llvm-diff-3.5", "llvm-rtdyld-3.5", "lockfile", "lscpu", "lwp-request", "lastb", "lesspipe", "linux-boot-prober", "llvm-dis-3.5", "llvm-size-3.5", "logger", "lsdiff", "lzcat", "lastlog", "let", "linuxinfo", "llvm-dwarfdump-3.5", "llvm-stress-3.5", "login", "lsinitramfs", "lzcmp", "lc", "lexgrog", "linux-version", "llvm-extract-3.5", "llvm-symbolizer-3.5", "loginctl", "lslocks", "lzdiff", "lcf", "lft", "llc-3.5", "llvm-link-3.5", "llvm-tblgen-3.5", "logname", "lsmod", "lzegrep", "ld", "lft.db", "lli-3.5", "llvm-mc-3.5", "ln", "logout", "lsof", "lzfgrep", "ld.bfd", "libnetcfg", "lli-child-target-3.5", "llvm-mcmarkup-3.5", "lnstat", "look", "lspci", "lzgrep", "ldd", "libtoolize", "llvm-ar-3.5", "llvm-nm-3.5", "local", "lorder", "lspgpot", "lzless", "ld.gold", "line", "llvm-as-3.5", "llvm-objdump-3.5", "locale", "ls", "lsusb", "lzma", "less", "link", "llvm-bcanalyzer-3.5", "llvm-profdata-3.5", "localectl", "lsattr", "lwp-download", "lzmainfo", "lessecho", "links2", "llvm-config-3.5", "llvm-ranlib-3.5", "localedef", "lsblk", "lwp-dump", "lzmore", "lessfile", "linux32", "llvm-cov-3.5", "llvm-readobj-3.5", "locate", "lsb_release", "lwp-mirror", "a2p", "arm-none-eabi-size", "aclocal", "arm-none-eabi-strings", "aclocal-1.14", "arm-none-eabi-strip", "acpi", "as", "acpi_listen", "asan_symbolize", "addpart", "asan_symbolize-3.5", "addr2line", "aspell", "al", "aspell-import", "al2", "at", "alias", "atq", "apropos", "atrm", "apt", "autoconf", "apt-cache", "autoconf2.64", "apt-cdrom", "autoheader", "apt-config", "autoheader2.64", "apt-extracttemplates", "autom4te", "apt-ftparchive", "autom4te2.64", "apt-get", "automake", "aptitude", "automake-1.14", "aptitude-create-state-bundle", "autopoint", "aptitude-curses", "autoreconf", "aptitude-run-state-bundle", "autoreconf2.64", "apt-key", "autoscan", "apt-listchanges", "autoscan2.64", "apt-mark", "autoupdate", "apt-show-versions", "autoupdate2.64", "apt-sortpkgs", "avr-addr2line", "ar", "avr-ar", "arch", "avr-as", "aria2c", "avr-c++", "arm-none-eabi-addr2line", "avr-c++filt", "arm-none-eabi-ar", "avr-cpp", "arm-none-eabi-as", "avr-elfedit", "arm-none-eabi-c++", "avr-g++", "arm-none-eabi-c++filt", "avr-gcc", "arm-none-eabi-cpp", "avr-gcc-4.8.1", "arm-none-eabi-elfedit", "avr-gcc-ar", "arm-none-eabi-g++", "avr-gcc-nm", "arm-none-eabi-gcc", "avr-gcc-ranlib", "arm-none-eabi-gcc-4.8", "avr-gcov", "arm-none-eabi-gcc-ar", "avr-gprof", "arm-none-eabi-gcc-nm", "avr-ld", "arm-none-eabi-gcc-ranlib", "avr-ld.bfd", "arm-none-eabi-gcov", "avr-nm", "arm-none-eabi-gprof", "avr-objcopy", "arm-none-eabi-ld", "avr-objdump", "arm-none-eabi-ld.bfd", "avr-ranlib", "arm-none-eabi-nm", "avr-readelf", "arm-none-eabi-objcopy", "avr-size", "avr-strings", "arm-none-eabi-ranlib", "avr-strip", "arm-none-eabi-readelf", "awk" };

            for (int i = 0; i < nameList.Length; i++) {
                string name = nameList[i];
                server.AddCommand(new Function()
                {
                    Name = name,
                    Comment = name,
                    Func = (x, y) => {
                        y.Logger.Info($"Call {name}");
                        return 0;
                    }
                });
            }

            for (int i = 0; i < 77; i++) {
                string name = $"n{i.ToString("00")}";
                server.AddCommand(new Function()
                {
                    Name = name,
                    Comment = name,
                    Func = (x, y) => {
                        y.Logger.Info($"Call {name}");
                        return 0;
                    }
                });
            }
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