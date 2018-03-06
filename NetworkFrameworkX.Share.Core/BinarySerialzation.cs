using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Share
{
    internal class BinarySerialzation : ISerialzation<IEnumerable<byte>>
    {
        private static IFormatter serializer = new BinaryFormatter();

        public bool Save<T>(T value, string path)
        {
            IEnumerable<byte> result = Serialize(value);

            File.WriteAllBytes(path, result.ToArray());

            return true;
        }

        public T Load<T>(string path, LoadMode mode = LoadMode.LoadOnly) where T : new()
        {
            T result = default(T);
            if (File.Exists(path)) {
                IEnumerable<byte> input = File.ReadAllBytes(path);

                result = Deserialize<T>(input);
            } else {
                if (mode.In(LoadMode.CreateWhenNull, LoadMode.CreateAndSaveWhenNull)) {
                    result = new T();

                    if (mode.In(LoadMode.CreateAndSaveWhenNull)) { Save(result, path); }
                }
            }

            return result;
        }

        public T Deserialize<T>(IEnumerable<byte> input)
        {
            T obj;
            try {
                using (var ms = new MemoryStream(input.ToArray())) {
                    obj = (T)serializer.Deserialize(ms);
                }
            } catch (Exception er) {
                throw new Exception(er.Message);
            }
            return obj;
        }

        public IEnumerable<byte> Serialize<T>(T obj)
        {
            byte[] buff;
            try {
                using (var ms = new MemoryStream()) {
                    serializer.Serialize(ms, obj);
                    ms.Flush();
                    buff = ms.GetBuffer();
                }
            } catch (Exception er) {
                throw new Exception(er.Message);
            }
            return buff;
        }
    }
}