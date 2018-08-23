using System;
using System.IO;
using NetworkFrameworkX.Interface;
using Newtonsoft.Json;

namespace NetworkFrameworkX.Share
{
    internal class BytesConverter : JsonConverter<byte[]>
    {
        public override byte[] ReadJson(JsonReader reader, Type objectType, byte[] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string json = reader.ReadAsString();
            return json.GetBytes();
        }

        public override void WriteJson(JsonWriter writer, byte[] value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            for (int i = 0; i < value.Length; i++) {
                writer.WriteValue(value[i]);
            }
            writer.WriteEndArray();
        }
    }

    internal class JsonSerialzation : ISerialzation<string>
    {
        private BytesConverter bytesConverter = new BytesConverter();

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

        public T Deserialize<T>(string input) => JsonConvert.DeserializeObject<T>(input);

        public string Serialize<T>(T value) => JsonConvert.SerializeObject(value, this.bytesConverter);
    }
}