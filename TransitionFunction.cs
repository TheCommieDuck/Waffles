using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waffles
{
    using State = System.String;

    public class BaseTransitionFunction<K, V> : Dictionary<K, V> { }

    public class FSATransitionFunction : BaseTransitionFunction<StateSymbolPair, HashSet<State>>
    {
        public FSATransitionFunction(BaseTransitionFunction<StateSymbolPair, HashSet<State>> other)
        {
            foreach (var set in other)
                this.Add(set.Key, new HashSet<State>(set.Value));
        }

        public FSATransitionFunction()
        {
        }
    }
}
