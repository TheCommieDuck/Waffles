using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waffles
{
    using State = System.String;
    using Symbol = System.Char;

    public struct StateSymbolPair
    {
        public State State;
        public Symbol Symbol;

        public StateSymbolPair(State state, Symbol symbol)
        {
            State = state;
            Symbol = symbol;
        }
    }
}
