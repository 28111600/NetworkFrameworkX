using System;

namespace NetworkFrameworkX.Interface
{
    public class Function : IFunction
    {
        public string Comment { get; set; }

        public Func<IArguments, ICaller, int> Func { get; set; }

        public string Name { get; set; }

        public CallerType Permission { get; set; }
    }
}