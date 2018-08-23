using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Share
{
    public enum MessageFlag
    {
        RequestPublicKey = 0,
        SendPublicKey = 1,
        RequestValidate = 2,
        ResponseValidate = 3,
        RefuseValidate = 4,
        SendAESKey = 5,
        SendClientPublicKey = 6,
        GotKey = 7,
        Message = 8
    }

    [Serializable]
    public class MessageBody
    {
        public MessageFlag Flag { get; set; }

        public byte[] Content { get; set; }

        public string Guid { get; set; }
    }

    [Serializable]
    public class CallBody
    {
        public Arguments Args { get; set; } = new Arguments();

        public string Call { get; set; }
    }

    [Serializable]
    public class Arguments : Dictionary<string, string>, IArguments
    {
        public Arguments()
        {
        }

        protected Arguments(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public new bool ContainsKey(string key) => base.ContainsKey(key) && !string.IsNullOrWhiteSpace(base[key]);

        public bool ContainsKey(params string[] key) => key.All(x => ContainsKey(x));

        public bool GetBool(string name) => bool.Parse(GetString(name));

        public float GetFloat(string name) => float.Parse(GetString(name));

        public int GetInt(string name) => int.Parse(GetString(name));

        public long GetLong(string name) => long.Parse(GetString(name));

        public string GetString(string name)
        {
            if (base.ContainsKey(name) && !base[name].IsNullOrWhiteSpace()) {
                return base[name].Trim();
            } else {
                return string.Empty;
            }
        }
        public void Put<T>(string name, T value) where T : IConvertible => Put(name, value.ToString(null));

        public void Put(string name, string value)
        {
            if (!name.IsNullOrEmpty() && !value.IsNullOrEmpty()) {
                if (!this.ContainsKey(name)) {
                    this.Add(name, value);
                } else {
                    this[name] = value;
                }
            }
        }
    }

    public class FunctionCollection : Dictionary<string, IFunction>
    {
        public bool Add(IFunction func)
        {
            if (ContainsKey(func.Name)) {
                return false;
            } else {
                Add(func.Name, func);
                return true;
            }
        }

        public int Call(string name, IArguments args, ICaller caller)
        {
            return ContainsKey(name) ? this[name].Func(args, caller) : -1;
        }
    }
}