using System;

namespace NetworkFrameworkX.Interface
{
    public interface IFunction
    {
        string Comment { get; set; }

        Func<IArguments, ICaller, int> Func { get; set; }

        string Name { get; set; }

        CallerType Permission { get; set; }
    }
}