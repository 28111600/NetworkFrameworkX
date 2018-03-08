using System;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    [Serializable]
    public class ServerConfig : IServerConfig
    {
        public string Language { get; set; } = "zh-CN";

        public int Port { get; set; } = 32768;

        public string ServerName { get; set; } = "default";

        public int Timeout { get; set; } = 3 * 1000;

        public bool Log { get; set; } = true;

        private static JsonSerialzation JsonSerialzation = new JsonSerialzation();

        public static T Load<T>(string path) where T : IServerConfig, new() => JsonSerialzation.Load<T>(path, LoadMode.CreateAndSaveWhenNull);

        public static bool Save<T>(T config, string path) where T : IServerConfig => JsonSerialzation.Save<T>(config, path);
    }
}