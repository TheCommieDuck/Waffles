using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using State = System.String;
using Symbol = System.Char;


namespace Waffles
{
    using StatePair = System.Collections.Generic.KeyValuePair<State, Symbol>;

    public static class DictExtension
    {
        public static void Add(this Dictionary<StatePair, HashSet<State>> dict, int startState, char input, params int[] moveTo)
        {
            dict.Add(new StatePair(startState.ToString(), input), Automaton.CreateFinalStates(moveTo));
        }
    }

	class Automaton
	{
		//neatness
		public class TransitionMap : Dictionary<StatePair, HashSet<State>>
        {
            public TransitionMap(TransitionMap other)
            {
                foreach (var set in other)
                    this.Add(set.Key, new HashSet<State>(set.Value));
            }

            public TransitionMap()
            {
            }
        }

        public static HashSet<State> CreateStates(int count)
        {
            IEnumerable<int> range = Enumerable.Range(1, count);
            return new HashSet<State>(range.Select(i => i.ToString()));//.Aggregate((i, j) => i.ToString() + " " + j);
        }

        public static HashSet<State> CreateFinalStates(params int[] states)
        {
            return new HashSet<State>(states.Select(i => i.ToString()));
        }

        public static string CreateSetOfStates(HashSet<string> states)
        {
            return CreateSetOfStates(states.ToArray());
        }

        public static string CreateSetOfStates(params string[] states)
        {
            if (states.Count() == 0)
                return null;
            if (states.Count() == 1)
                return "{" + states[0] + "}";
            return "{" + string.Join(", ", states) + "}";
        }

		public const Symbol Epsilon = 'ε';

		public const Symbol ErrorState = '∅';

		public HashSet<State> States { get; private set; }

		public HashSet<Symbol> Alphabet { get; private set; }

		public TransitionMap TransitionFunction { get; private set; }

		public State StartState { get; private set; }

		public HashSet<State> FinalStates { get; private set; }

		public Automaton(HashSet<State> states, HashSet<Symbol> alphabet, TransitionMap transFunc, State start, HashSet<State> final)
		{
			this.States = states;
			this.Alphabet = alphabet;
			this.TransitionFunction = transFunc;
			this.StartState = start;
			this.FinalStates = final;

			VerifyAutomaton();
		}

        public Automaton(Automaton automaton)
        {
            this.States = new HashSet<State>(automaton.States);
            this.Alphabet = new HashSet<Symbol>(automaton.Alphabet);
            this.TransitionFunction = new TransitionMap(automaton.TransitionFunction);
            this.StartState = automaton.StartState;
            this.FinalStates = new HashSet<State>(automaton.FinalStates);
        }

		public void VerifyAutomaton()
		{
			//invalid start state
			if (!this.States.Contains(StartState))
				throw new ArgumentOutOfRangeException("Invalid start state", (Exception)null);

			//if any final states are invalid
			if (this.States.Intersect(FinalStates).Count() != FinalStates.Count)
				throw new ArgumentOutOfRangeException("Invalid final states", (Exception)null);

			//todo: validate delta
		}

		public bool UsesEpsilonMoves()
		{
			return TransitionFunction.Keys.Any(s => s.Value == Automaton.Epsilon);
		}

		public bool IsDeterministic(bool verbose=false)
		{
			//return false if either: a) any state has multiple transitions on a single symbol
			//b) any state has < |Σ| transitions (then either it's missing a transition..or more, by the pigeonhole principle - then we've got a))
			//c) any epsilon moves
			if (TransitionFunction.Any(s => s.Value.Count > 1))
			{
				if(verbose)
					Console.WriteLine("Not deterministic - multiple states per (State, Symbol)!");
				return false;
			}

			if (TransitionFunction.Any(s => s.Key.Value == Automaton.Epsilon))
			{
				if (verbose)
					Console.WriteLine("Not deterministic - has epsilon moves!");
				return false;
			}
				
			if(TransitionFunction.Keys.Count != (States.Count * Alphabet.Count))
			{
				if (verbose)
					Console.WriteLine("Not deterministic - not enough transitions!");
				return false;
			}

			//we can assume that the transition function is valid (no invalid states/symbols), so next we just need to check that every (state, symbol) pair is represented
			foreach (var transition in TransitionFunction.Keys.GroupBy(k => k.Key))
			{
				if (transition.Select(s => s.Value).Count() != Alphabet.Count)
				{
					if (verbose)
						Console.WriteLine("Alphabet has {0} symbols, but found {1} symbols to transition on!", Alphabet.Count, transition.Select(s => s.Value).Count());
					return false;
				}
			}
			return true;
		}

        public Automaton CreateDeterministicAutomaton()
        {
            //return if we're already deterministic.
            if (IsDeterministic())
                return new Automaton(this);

            //todo: generate (minimal) symbols
            //todo: generate finish states
            TransitionMap map = new TransitionMap();
			//ep-closure
			HashSet<State> startStates = GetStatesAccessibleFrom(StartState, Automaton.Epsilon);
			Stack<HashSet<State>> statesToVisit = new Stack<HashSet<State>>();
			List<HashSet<State>> newStates = new List<HashSet<State>>();

			statesToVisit.Push(startStates);

			newStates.Add(startStates);

			HashSet<State> finalStates = new HashSet<State>();
			//consider the case our start state(s) are final
			if (startStates.Intersect(FinalStates).Count() != 0)
			{
				Console.WriteLine("Start state(s) are final; adding..");
				finalStates.Add(Automaton.CreateSetOfStates(startStates));
			}

			while(statesToVisit.Count > 0)
			{
				HashSet<State> stateSet = statesToVisit.Pop();

				//consider each symbol in our alphabet; the transition from S => S is going to be the union of every possible accessible state
				foreach(Symbol symbol in Alphabet)
				{
					//if the map contains this key, then we've already visited this state - don't do it again
					if (!map.ContainsKey(new StatePair(Automaton.CreateSetOfStates(stateSet), symbol)))
					{
						HashSet<State> closure = new HashSet<State>();

						foreach (State state in stateSet)
							closure.UnionWith(GetStatesAccessibleFrom(state, symbol));
						Console.WriteLine("From state {0}, on input {2}, the states are {1}", Automaton.CreateSetOfStates(stateSet), Automaton.CreateSetOfStates(closure), symbol);

						HashSet<State> stateToAdd = new HashSet<State>() { closure.Count == 0 ? Automaton.CreateSetOfStates(Automaton.ErrorState.ToString()) : 
							Automaton.CreateSetOfStates(closure)};

						//so now closure is every possible state reachable from some symbol. If it's empty, it's the error state
						Console.WriteLine("Adding transition from state {0} on symbol {1}", Automaton.CreateSetOfStates(stateSet), symbol);
						map.Add(new StatePair(Automaton.CreateSetOfStates(stateSet), symbol), stateToAdd);

						//keep a track of the sets we have
						newStates.Add(stateToAdd);

						//if we have a final state, the set of states is final
						if (closure.Intersect(FinalStates).Count() != 0)
							finalStates.Add(Automaton.CreateSetOfStates(closure));

						//now we need to visit this set of states and find where it goes
						if (closure.Count != 0)
							statesToVisit.Push(closure);
					}
				}
			}

			HashSet<State> states = new HashSet<State>();
			//the set of states is the unique keys in the mapping
			foreach (var state in map.Keys.GroupBy(k => k.Key))
				states.Add(state.Key);

			//the final states are any of the 'new' states which contain a final state

            return new Automaton(states, Alphabet, map, 
                Automaton.CreateSetOfStates(startStates), finalStates);
        }

        public HashSet<State> GetStatesAccessibleWithAnyInput(State state)
        {
            HashSet<State> states = new HashSet<State>();
            //first add any epsilon states (this is recursive)
            states.UnionWith(GetStatesAccessibleFrom(state, Automaton.Epsilon));
            var allTransitions = TransitionFunction.Where((k, v) => k.Key.Key == state);
            if (allTransitions.Count() == 0)
                return states;
            else
            {
                foreach(HashSet<State> accessibleStates in allTransitions.Select((k, v) => k.Value))
                    states.UnionWith(accessibleStates);
            }
            return states;
        }

        public HashSet<State> GetStatesAccessibleFrom(State state, char input, List<StatePair> pastStates=null)
        {
            //avoid infinite loops of epsilon
            if (pastStates == null)
                pastStates = new List<StatePair>();
            else
            {
                //this is the state we immediately transitioned from
                int pastID = pastStates.Count - 1;
                while (pastID >= 0 && pastStates[pastID].Value == Automaton.Epsilon)
                {
                    //if the last state we visited is the same as the state we started in here (i.e. X=>X), then it's a loop. Similarly
                    //if we have an arbitrary number of epsilon moves but still reach the same state, same issue
                    if (pastStates[pastID].Key == state)
                    {
                        Console.WriteLine("Infinitely looping on epsilon transitions. Returning..");
                        return new HashSet<State>(); //so we can't get anywhere new from here
                    }
                    pastID--;
                }
            }

            HashSet<State> ret = new HashSet<State>();

			if (input == Automaton.Epsilon)
				ret.Add(state);

			HashSet<State> nextStates = null, nextEpsilonStates = null;

			//add all the states we can get to from here.
            TransitionFunction.TryGetValue(new KeyValuePair<State, Symbol>(state, Automaton.Epsilon), out nextEpsilonStates);
			TransitionFunction.TryGetValue(new KeyValuePair<State, Symbol>(state, input), out nextStates);

            if (nextStates != null)
                ret = nextStates;

            if (nextEpsilonStates != null)
            {
                foreach (State epState in nextEpsilonStates)
                {
                    StatePair currentState = new StatePair(state, Automaton.Epsilon);
                    List<StatePair> updatedPastStates = new List<StatePair>(pastStates);
                    updatedPastStates.Add(currentState);
                    ret.UnionWith(GetStatesAccessibleFrom(epState, input, updatedPastStates));
                }
            }

            return ret;
        }

        public HashSet<State> GetStatesAccessibleFrom(State state, string input)
        {
            if (input.Length == 0)
                return GetStatesAccessibleFrom(state, Automaton.Epsilon);
            HashSet<State> allStates = new HashSet<State>();
            //keep a track of all our current pebbles (which are states mapped to the input remaining
            Stack<KeyValuePair<State, string>> pebbles = new Stack<KeyValuePair<State, State>>();
            pebbles.Push(new KeyValuePair<State, string>(state, input));
            while (pebbles.Count > 0)
            {
                var current = pebbles.Pop();
                if (current.Value.Length == 0)
                    allStates.UnionWith(GetStatesAccessibleFrom(current.Key, Automaton.Epsilon));
                else
                {
                    string newInput = new string(current.Value.Skip(1).ToArray());
                    foreach (State s in GetStatesAccessibleFrom(current.Key, current.Value.First()))
                        pebbles.Push(new KeyValuePair<State, string>(s, newInput));
                }
            }
            return allStates;
        }

		public bool IsWordInLanguage(String input)
		{
			return IsWordInLanguage(input, StartState);
		}

		public bool IsWordInLanguage(String input, State startState)
		{
            //if we can finish now, finish now
            if (input.Length == 0)
                return FinalStates.Contains(startState);

            return GetStatesAccessibleFrom(startState, input).Intersect(FinalStates).Count() > 0;
		}
	}
}
