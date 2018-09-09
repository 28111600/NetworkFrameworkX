using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    public partial class Server<TConfig>
    {
        private void InitializePlugin()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                AssemblyName assemblyName = new AssemblyName(args.Name);
                FileInfo assemblyFile = new FileInfo(Path.Combine(GetFolderPath(FolderPath.Plugin), $"{assemblyName.Name}.dll"));

                return assemblyFile.Exists ? Assembly.LoadFrom(assemblyFile.FullName) : null;
            };

            LoadAllPlugin();
        }

        private Dictionary<string, PluginLoader> pluginList = new Dictionary<string, PluginLoader>(StringComparer.OrdinalIgnoreCase);

        public IList<string> PluginList => this.pluginList.Keys.ToList();

        public void SavePluginConfig(string name)
        {
            if (this.pluginList.ContainsKey(name)) {
                SavePluginConfig(this.pluginList[name]);
            }
        }

        private void SavePluginConfig(PluginLoader plugin)
        {
            DirectoryInfo folder = new DirectoryInfo(Path.Combine(GetFolderPath(FolderPath.PluginConfig), plugin.Name));
            if (!folder.Exists) { folder.Create(); }

            string config = plugin.SerializeConfig();
            if (!config.IsNullOrEmpty()) {
                File.WriteAllText(Path.Combine(folder.FullName, FILE_CONFIG), config);
            }
        }

        public void LoadPlugin(IPlugin plugin, bool force = false) => LoadPlugin(new PluginLoader(plugin), force);

        internal void LoadPlugin(PluginLoader plugin, bool force = false)
        {
            if (plugin.Name.IsNullOrEmpty()) {
                this.Logger.Warning(this.lang.PluginLoadError);
                return;
            }

            if (this.pluginList.ContainsKey(plugin.Name)) {
                this.Logger.Warning(string.Format(this.lang.PluginNameDuplicate, plugin.Name));
                return;
            }

            plugin.Server = this;
            DirectoryInfo folder = new DirectoryInfo(Path.Combine(GetFolderPath(FolderPath.PluginConfig), plugin.Name));
            if (!folder.Exists) { folder.Create(); }

            FileInfo file = new FileInfo(Path.Combine(folder.FullName, FILE_CONFIG));
            if (!file.Exists) {
                SavePluginConfig(plugin);
            } else {
                string config = File.ReadAllText(file.FullName);
                plugin.DeserializeConfig(config);
            }

            if (plugin.Config.Enabled || force) {
                plugin.Server = this;
                this.pluginList.Add(plugin.Name, plugin);
                this.Logger.Info(string.Format(this.lang.LoadPlugin, plugin.Name));

                plugin.OnLoad();

                if (force) {
                    plugin.Config.Enabled = true;
                    SavePluginConfig(plugin);
                }
            }
        }

        public void LoadPlugin(string assemblyPath, bool force = false)
        {
            string[] path = new string[] { GetFolderPath(FolderPath.Plugin), GetFolderPath(FolderPath.PluginDependency) };
            PluginLoader loader = new PluginLoader(assemblyPath, path);
            LoadPlugin(loader, force);
        }

        public const string PATTERN_DLL = "*.dll";

        public void LoadAllPlugin()
        {
            DirectoryInfo folder = new DirectoryInfo(GetFolderPath(FolderPath.Plugin));

            foreach (FileInfo file in folder.GetFiles(PATTERN_DLL)) {
                LoadPlugin(file.FullName);
            }
        }

        public void RefreshPluginList()
        {
        }

        public bool UnLoadPlugin(IPlugin plugin)
        {
            plugin.OnDestroy();

            if (plugin is PluginLoader pluginLoader && pluginLoader.UnLoadable) {
                return pluginLoader.Unload();
            }

            return false;
        }

        public void UnLoadAllPlugin()
        {
            foreach (PluginLoader item in this.pluginList.Values) {
                UnLoadPlugin(item);
            }
        }
    }
}