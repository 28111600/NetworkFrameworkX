using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NetworkFrameworkX.Interface
{
    public class Function : MarshalByRefObject, IFunction
    {
        public string Comment { get; set; }

        public Func<IArguments, ICaller, int> Func { get; set; }

        public string Name { get; set; }

        public CallerType Permission { get; set; }
    }

    public class FunctionInfo : IFunctionInfo
    {
        public string Comment { get; set; }

        public string Name { get; set; }

        public CallerType Permission { get; set; }
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

    [Serializable]
    public class FunctionInfoCollection : Dictionary<string, IFunctionInfo>
    {
        public static explicit operator FunctionInfoCollection(FunctionCollection s)
        {
            FunctionInfoCollection pairs = new FunctionInfoCollection();
            foreach (var item in s) {
                pairs.Add(item.Key, item.Value);
            }
            return pairs;
        }

        public FunctionInfoCollection()
        {
        }

        protected FunctionInfoCollection(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context) => base.GetObjectData(info, context);
    }
}