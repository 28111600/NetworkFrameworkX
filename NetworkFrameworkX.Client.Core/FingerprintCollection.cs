using System;
using System.Collections.Generic;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Client
{
    [Serializable]
    internal class FingerprintCollection : Dictionary<string, string>
    {
        private static JsonSerialzation JsonSerialzation = new JsonSerialzation();

        public static FingerprintCollection Load(string path) => JsonSerialzation.Load<FingerprintCollection>(path, LoadMode.CreateAndSaveWhenNull);

        public bool Save(string path) => JsonSerialzation.Save(this, path);
    }
}