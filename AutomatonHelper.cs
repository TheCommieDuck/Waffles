using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waffles
{
    using State = System.String;
    using Symbol = System.Char;

    public static class DictExtension
    {
        public static void Add(this Dictionary<StateSymbolPair, HashSet<State>> dict, int startState, char input, params int[] moveTo)
        {
            dict.Add(new StateSymbolPair(startState.ToString(), input), AutomatonHelper.CreateFinalStates(moveTo));
        }
    }

    public static class Symbols
    {
        public const Symbol Epsilon = 'ε';

        public const Symbol ErrorState = '∅';
    }

    public static class AutomatonHelper
    {
        public static HashSet<State> CreateStates(int count)
        {
            IEnumerable<int> range = Enumerable.Range(1, count);
            return new HashSet<State>(range.Select(i => i.ToString()));//.Aggregate((i, j) => i.ToString() + " " + j);
        }

        public static HashSet<State> CreateFinalStates(params int[] states)
        {
            return new HashSet<State>(states.Select(i => i.ToString()));
        }

        public static string CreateSetOfStates(HashSet<State> states)
        {
            return CreateSetOfStates(states.ToArray());
        }

        public static string CreateSetOfStates(params State[] states)
        {
            if (states.Count() == 0)
                return null;
            if (states.Count() == 1)
                return "{" + states[0] + "}";
            return "{" + string.Join(", ", states) + "}";
        }
    }
}
