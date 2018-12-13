using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkFrameworkX.Interface
{
    public class StringKeyCollection<T> : Dictionary<string, T>, IStringKeyCollection<T>
    {
        public void ForEach(Action<T> action) => this.Values.ToList().ForEach(action);

        public void ParallelForEach(Action<T> action) => Parallel.ForEach(this.Values, action);

        public bool All(Func<T, bool> match) => this.Values.All(match);
    }
}