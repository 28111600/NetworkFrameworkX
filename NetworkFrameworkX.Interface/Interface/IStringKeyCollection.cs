using System;
using System.Collections.Generic;

namespace NetworkFrameworkX.Interface
{
    public interface IStringKeyCollection<T> : IDictionary<string, T>
    {
        bool All(Func<T, bool> match);

        void ForEach(Action<T> action);

        void ParallelForEach(Action<T> action);
    }
}