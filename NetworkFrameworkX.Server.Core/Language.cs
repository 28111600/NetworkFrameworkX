using System;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    [Serializable]
    public class Language
    {
        public string Name = "zh-CN";

        [NonSerialized]
        public string Initializing = "初始化";

        public string Port = "端口：{0}";
        public string LoadLanguage = "加载语言: {0}";
        public string LoadPlugin = "加载插件: {0}";

        public string GenerateKeys = "正在生成密钥";

        public string ServerStop = "正在关闭";
        public string ServerStart = "正在启动";
        public string LoadConfig = "加载配置文件";
        public string SaveConfig = "保存配置文件";
        public string UnknownCommand = "未知命令";
        public string UserCommand = "用户 {0} 调用命令：{1}";
        public string PortNoAvailabled = "端口 {0} 被占用";
        public string ServerClosed = "服务器关闭";
        public string ClientLogin = "用户 {0} 登入";
        public string ClientLogout = "用户 {0} 退出";

        public string PluginLoadError = "插件加载错误";
        public string PluginNameDuplicate = "插件加载重复: {0}";

        public string ClientLostConnection = "用户 {0} 失去连接";

        private static JsonSerialzation JsonSerialzation = new JsonSerialzation();

        public static Language Load(string path) => JsonSerialzation.Load<Language>(path, LoadMode.CreateAndSaveWhenNull);

        public bool Save(string path) => JsonSerialzation.Save(this, path);
    }
}