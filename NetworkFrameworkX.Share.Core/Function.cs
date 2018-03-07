using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Share
{
    public enum MessageFlag
    {
        RequestPublicKey,
        SendPublicKey,
        RequestValidate,
        ResponseValidate,
        RefuseValidate,
        SendAESKey,
        SendClientPublicKey,
        GotKey,
        Message
    }

    [Serializable]
    public class MessageBody
    {
        public MessageFlag Flag { get; set; }

        public byte[] Content { get; set; }

        public string Guid { get; set; }

        public long TimeStamp { get; set; }
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

        public bool GetBool(string name) => GetString(name) == bool.TrueString ? true : false;

        public float GetFloat(string name) => float.Parse(GetString(name));

        public int GetInt(string name) => int.Parse(GetString(name));

        public string GetString(string name)
        {
            if (base.ContainsKey(name) && !string.IsNullOrWhiteSpace(base[name])) {
                return base[name].Trim();
            } else {
                return string.Empty;
            }
        }

        public void Put(string name, bool value) => Put(name, value ? bool.TrueString : bool.FalseString);

        public void Put(string name, float value) => Put(name, value.ToString());

        public void Put(string name, int value) => Put(name, value.ToString());

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