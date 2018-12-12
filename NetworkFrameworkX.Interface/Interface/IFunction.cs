using System;

namespace NetworkFrameworkX.Interface
{
    public interface IFunction : IFunctionInfo
    {
        Func<IArguments, ICaller, int> Func { get; set; }
    }

    public interface IFunctionInfo
    {
        string Comment { get; set; }

        string Name { get; set; }

        CallerType Permission { get; set; }
    }
}