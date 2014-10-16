using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waffles
{
    using State = System.String;
    using Symbol = System.Char;

    public abstract class Automaton<TransitionOn, TransitionTo>
    {
        public HashSet<State> States { get; protected set; }

        public HashSet<Symbol> Alphabet { get; protected set; }

        public BaseTransitionFunction<TransitionOn, TransitionTo> TransitionFunction { get; protected set; }

        public State StartState { get; protected set; }

		public HashSet<State> FinalStates { get; protected set; }

        public abstract void VerifyAutomaton();

        public abstract bool ContainsEpsilonMoves();

        public abstract bool IsDeterministic(bool verbose);

        public abstract Automaton<TransitionOn, TransitionTo> CreateDeterministicAutomaton();
    }
}
