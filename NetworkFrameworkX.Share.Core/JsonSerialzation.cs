using System.IO;
using System.Web.Script.Serialization;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Share
{
    internal class JsonSerialzation : ISerialzation<string>
    {
        private static JavaScriptSerializer serializer = new JavaScriptSerializer();

        public bool Save<T>(T value, string path)
        {
            string result = Serialize(value);

            File.WriteAllText(path, result);

            return true;
        }

        public T Load<T>(string path, LoadMode mode = LoadMode.LoadOnly) where T : new()
        {
            T result = default(T);
            if (File.Exists(path)) {
                string input = File.ReadAllText(path);

                result = Deserialize<T>(input);
            } else {
                if (mode.In(LoadMode.CreateWhenNull, LoadMode.CreateAndSaveWhenNull)) {
                    result = new T();

                    if (mode.In(LoadMode.CreateAndSaveWhenNull)) { Save(result, path); }
                }
            }

            return result;
        }

        public T Deserialize<T>(string input) => serializer.Deserialize<T>(input);

        public string Serialize<T>(T value) => serializer.Serialize(value);
    }
}