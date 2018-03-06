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

        public static ServerConfig Load(string path) => JsonSerialzation.Load<ServerConfig>(path, LoadMode.CreateAndSaveWhenNull);

        public bool Save(string path) => JsonSerialzation.Save(this, path);
    }
}