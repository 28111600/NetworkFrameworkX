using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    public partial class Server<TConfig>
    {
        public FunctionCollection CommandTable { get; private set; } = new FunctionCollection();

        public FunctionInfoCollection CommandInfoListWithPlugin
        {
            get {
                FunctionInfoCollection list = new FunctionInfoCollection();
                foreach (var item in (FunctionInfoCollection)this.CommandTable) {
                    list.Add(item.Key, item.Value);
                }

                foreach (var plugin in this.pluginList.Values) {
                    foreach (var item in plugin.CommandInfoList) {
                        list.Add(item.Key, item.Value);
                    }
                }
                return list;
            }
        }

        public bool AddCommand(IFunction func) => this.CommandTable.Add(func);

        public int CallCommand(string name, IArguments args, ICaller caller = null)
        {
            if (this.CommandTable.ContainsKey(name)) {
                return this.CommandTable.Call(name, args, caller ?? this);
            } else {
                foreach (var plugin in this.pluginList.Values) {
                    if (plugin.CommandList.Contains(name)) {
                        return plugin.CallCommand(name, args, caller ?? this);
                    }
                }
            }

            return -1;
        }

        public int CallCommand(string name, ICaller caller = null) => CallCommand(name, new Arguments(), caller);

        public int CallFunction(string name, IArguments args = null) => this.CallFunction(name, args ?? new Arguments(), this);

        public new int CallFunction(string name, IArguments args, ICaller caller = null)
        {
            if (this.FunctionTable.ContainsKey(name)) {
                return this.FunctionTable.Call(name, args, caller ?? this);
            } else {
                foreach (var plugin in this.pluginList.Values) {
                    if (plugin.FunctionList.Contains(name)) {
                        return plugin.CallFunction(name, args, caller ?? this);
                    }
                }
            }

            return -1;
        }

        public void HandleCommand(string command, ICaller caller)
        {
            if (caller.Type.In(CallerType.Console)) {
                this.History.Add(command);
            }
            if (caller.Type.In(CallerType.Console, CallerType.Client)) {
                List<string> commandTextList = command.Split(Utility.CharWhiteSpace).ToList();

                commandTextList.RemoveAll((s) => string.IsNullOrWhiteSpace(s));
                if (commandTextList.Count > 0 && !string.IsNullOrWhiteSpace(commandTextList[0])) {
                    if (this.CommandInfoListWithPlugin.ContainsKey(commandTextList[0])) {
                        Arguments args = new Arguments();

                        for (int i = 1; i < commandTextList.Count; i++) {
                            args.Put((i - 1).ToString(), commandTextList[i]);
                        }

                        args.Put("args", command.Substring(commandTextList[0].Length).Trim());

                        CallCommand(commandTextList[0], args, caller);

                        if (commandTextList[0] != "say" && caller.Type == CallerType.Client) {
                            IUser user = caller as IUser;
                            if (caller != null) {
                                this.Logger.Info(string.Format(this.lang.UserCommand, user.Name, command));
                            }
                        }
                    } else {
                        caller.Logger.Error(this.lang.UnknownCommand);
                    }
                }
            }
        }

        private void InitializeCommand()
        {
            LoadTestCommand();
            LoadInternalCommand();
        }

        private void LoadInternalCommand()
        {
            /*
             * {"Call":"command","Args":{"command":"cmd arg1 arg2 ..."}}
             */
            this.AddFunction(new Function()
            {
                Name = "command",
                Comment = null,
                Func = (args, caller) => {
                    if (caller.Type.In(CallerType.Client)) {
                        IServerUser user = (IServerUser)caller;

                        string command = args.GetString("command");
                        if (!string.IsNullOrWhiteSpace(command)) {
                            this.HandleCommand(command, caller);
                        }
                    }
                    return 0;
                }
            });

            this.AddFunction(new Function()
            {
                Name = "heartbeat",
                Func = (args, caller) => {
                    return 0;
                }
            });

            this.AddCommand(new Function()
            {
                Name = "plugin-unload",
                Comment = "Unload plugin",
                Func = (args, caller) => {
                    string name = args.GetString("0");

                    if (name.IsNullOrWhiteSpace()) {
                        caller.Logger.Info($"Missing parameter: [pluginName]");
                        return -1;
                    }

                    var plugin = this.pluginList.FirstOrDefault(x => x.Key == name).Value;

                    if (plugin == null) {
                        caller.Logger.Info($"Plugin cannot be found: {name}");
                        return -1;
                    }

                    string pluginName = plugin.Name;

                    if (plugin.UnLoadable) {
                        plugin.Config.Enabled = false;
                        SavePluginConfig(plugin);
                        if (UnLoadPlugin(plugin)) {
                            caller.Logger.Info($"Plugin unload successfully: {pluginName}");
                            this.pluginList.Remove(pluginName);
                        } else {
                            caller.Logger.Error($"Plugin unload failed: {pluginName}!");
                        }
                    } else {
                        this.Logger.Error($"Plugin cannot be unloaded: {pluginName}");
                    }

                    return 0;
                }
            });

            this.AddCommand(new Function()
            {
                Name = "plugin-load",
                Comment = "Load plugin",
                Func = (args, caller) => {
                    string name = args.GetString("0");

                    if (name.IsNullOrWhiteSpace()) {
                        caller.Logger.Info($"Missing parameter: [pluginName]");
                        return -1;
                    }

                    FileInfo file = new FileInfo(Path.Combine(GetFolderPath(FolderPath.Plugin), name));

                    if (file.Exists) {
                        return this.LoadPlugin(file.FullName, true) ? 0 : -1;
                    } else {
                        this.Logger.Error($"Plugin cannot be found: {name}");
                        return -1;
                    }
                }
            });

            this.AddCommand(new Function()
            {
                Name = "plugin-list",
                Comment = "Show plugin list",
                Func = (args, caller) => {
                    DirectoryInfo folderPlugin = new DirectoryInfo(GetFolderPath(FolderPath.Plugin));

                    var list = new List<string>();
                    foreach (DirectoryInfo folder in folderPlugin.GetDirectories()) {
                        foreach (FileInfo file in folder.GetFiles(PATTERN_DLL)) {
                            if (PluginLoader.TryGetPluginName(file.FullName, out string name)) {
                                var plugin = this.pluginList.FirstOrDefault(x => x.Value.AssemblyPath == file.FullName).Value;

                                list.Add($"{file.Directory.Name}{Path.DirectorySeparatorChar}{file.Name} (Name: {name}, Status: {(plugin != null ? "Enabled" : "Disabled")})");
                            }
                        }
                    }

                    caller.Logger.Info("- Plugin List");
                    caller.Logger.Info(list);

                    return 0;
                }
            });
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void LoadTestCommand()
        {
            AddCommand(new Function()
            {
                Name = "test-rsa",
                Comment = "Test RSA",
                Func = (args, caller) => {
                    string input = args.GetString("args"); input = input.IsNullOrEmpty() ? "TEST TEST TEST 1234567890 你好!" : input;
                    byte[] encryptData = RSAHelper.Encrypt(input, this.RSAKey);
                    byte[] decryptData = RSAHelper.Decrypt(encryptData, this.RSAKey);

                    var dict = new Dictionary<string, string>();
                    dict.Add("input", input);
                    dict.Add("encrypt", BitConverter.ToString(encryptData).Replace("-", string.Empty));
                    dict.Add("decrypt", decryptData.GetString());
                    caller.Logger.Info("Test RSA");
                    caller.Logger.Info(dict);

                    return 0;
                }
            });
        }
    }
}