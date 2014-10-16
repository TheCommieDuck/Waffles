using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waffles
{
	using State = System.String;
	using Symbol = System.Char;

	class FiniteStateAutomaton : Automaton<StateSymbolPair, HashSet<State>>
	{
		public FiniteStateAutomaton(HashSet<State> states, HashSet<Symbol> alphabet, FSATransitionFunction transFunc, State start, HashSet<State> final)
		{
			this.States = states;
			this.Alphabet = alphabet;
			this.TransitionFunction = transFunc;
			this.StartState = start;
			this.FinalStates = final;

			VerifyAutomaton();
		}

		public FiniteStateAutomaton(FiniteStateAutomaton automaton)
		{
			this.States = new HashSet<State>(automaton.States);
			this.Alphabet = new HashSet<Symbol>(automaton.Alphabet);
			this.TransitionFunction = new FSATransitionFunction(automaton.TransitionFunction);
			this.StartState = automaton.StartState;
			this.FinalStates = new HashSet<State>(automaton.FinalStates);
		}

		public override void VerifyAutomaton()
		{
			//invalid start state
			if (!this.States.Contains(StartState))
				throw new ArgumentOutOfRangeException("Invalid start state", (Exception)null);

			//if any final states are invalid
			if (this.States.Intersect(FinalStates).Count() != FinalStates.Count)
				throw new ArgumentOutOfRangeException("Invalid final states", (Exception)null);

			//todo: validate delta
		}

		public override bool ContainsEpsilonMoves()
		{
			return TransitionFunction.Keys.Any(s => s.Symbol == Symbols.Epsilon);
		}

		public override bool IsDeterministic(bool verbose = false)
		{
			//return false if either: a) any state has multiple transitions on a single symbol
			//b) any state has < |Σ| transitions (then either it's missing a transition..or more, by the pigeonhole principle - then we've got a))
			//c) any epsilon moves
			if (TransitionFunction.Any(s => s.Value.Count > 1))
			{
				if (verbose)
					Console.WriteLine("Not deterministic - multiple states per (State, Symbol)!");
				return false;
			}

			if (TransitionFunction.Any(s => s.Key.Symbol == Symbols.Epsilon))
			{
				if (verbose)
					Console.WriteLine("Not deterministic - has epsilon moves!");
				return false;
			}

			if (TransitionFunction.Keys.Count != (States.Count * Alphabet.Count))
			{
				if (verbose)
					Console.WriteLine("Not deterministic - not enough transitions!");
				return false;
			}

			//we can assume that the transition function is valid (no invalid states/symbols), so next we just need to check that every (state, symbol) pair is represented
			foreach (var transition in TransitionFunction.Keys.GroupBy(k => k.State))
			{
				if (transition.Select(s => s.Symbol).Count() != Alphabet.Count)
				{
					if (verbose)
						Console.WriteLine("Alphabet has {0} symbols, but found {1} symbols to transition on!",
							Alphabet.Count, transition.Select(s => s.Symbol).Count());
					return false;
				}
			}
			return true;
		}

		public override Automaton<StateSymbolPair, HashSet<State>> CreateDeterministicAutomaton()
		{
			//return if we're already deterministic.
			if (IsDeterministic())
				return new FiniteStateAutomaton(this);

			FSATransitionFunction map = new FSATransitionFunction();
			//ep-closure
			HashSet<State> startStates = GetStatesAccessibleFrom(StartState, Symbols.Epsilon);
			Stack<HashSet<State>> statesToVisit = new Stack<HashSet<State>>();
			List<HashSet<State>> newStates = new List<HashSet<State>>();

			statesToVisit.Push(startStates);

			newStates.Add(startStates);

			HashSet<State> finalStates = new HashSet<State>();
			//consider the case our start state(s) are final
			if (startStates.Intersect(FinalStates).Count() != 0)
			{
				Console.WriteLine("Start state(s) are final; adding..");
				finalStates.Add(AutomatonHelper.CreateSetOfStates(startStates));
			}

			while (statesToVisit.Count > 0)
			{
				HashSet<State> stateSet = statesToVisit.Pop();

				//consider each symbol in our alphabet; the transition from S => S is going to be the union of every possible accessible state
				foreach (Symbol symbol in Alphabet)
				{
					//if the map contains this key, then we've already visited this state - don't do it again
					if (!map.ContainsKey(new StateSymbolPair(AutomatonHelper.CreateSetOfStates(stateSet), symbol)))
					{
						HashSet<State> closure = new HashSet<State>();

						foreach (State state in stateSet)
							closure.UnionWith(GetStatesAccessibleFrom(state, symbol));
						Console.WriteLine("From state {0}, on input {2}, the states are {1}", AutomatonHelper.CreateSetOfStates(stateSet),
							AutomatonHelper.CreateSetOfStates(closure), symbol);

						HashSet<State> stateToAdd = new HashSet<State>() { closure.Count == 0 ? AutomatonHelper.CreateSetOfStates(
                            Symbols.ErrorState.ToString()) : AutomatonHelper.CreateSetOfStates(closure)};

						//so now closure is every possible state reachable from some symbol. If it's empty, it's the error state
						Console.WriteLine("Adding transition from state {0} on symbol {1}", AutomatonHelper.CreateSetOfStates(stateSet),
							symbol);
						map.Add(new StateSymbolPair(AutomatonHelper.CreateSetOfStates(stateSet), symbol), stateToAdd);

						//keep a track of the sets we have
						newStates.Add(stateToAdd);

						//if we have a final state, the set of states is final
						if (closure.Intersect(FinalStates).Count() != 0)
							finalStates.Add(AutomatonHelper.CreateSetOfStates(closure));

						//now we need to visit this set of states and find where it goes
						if (closure.Count != 0)
							statesToVisit.Push(closure);
					}
				}
			}

			HashSet<State> states = new HashSet<State>();
			//the set of states is the unique keys in the mapping
			foreach (var state in map.Keys.GroupBy(k => k.State))
				states.Add(state.Key);

			//the final states are any of the 'new' states which contain a final state

			return new FiniteStateAutomaton(states, Alphabet, map,
				AutomatonHelper.CreateSetOfStates(startStates), finalStates);
		}

		public HashSet<State> GetStatesAccessibleWithAnyInput(State state)
		{
			HashSet<State> states = new HashSet<State>();
			//first add any epsilon states (this is recursive)
			states.UnionWith(GetStatesAccessibleFrom(state, Symbols.Epsilon));
			var allTransitions = TransitionFunction.Where((k, v) => k.Key.State == state);
			if (allTransitions.Count() == 0)
				return states;
			else
			{
				foreach (HashSet<State> accessibleStates in allTransitions.Select((k, v) => k.Value))
					states.UnionWith(accessibleStates);
			}
			return states;
		}

		public HashSet<State> GetStatesAccessibleFrom(State state, char input, List<StateSymbolPair> pastStates = null)
		{
			//avoid infinite loops of epsilon
			if (pastStates == null)
				pastStates = new List<StateSymbolPair>();
			else
			{
				//this is the state we immediately transitioned from
				int pastID = pastStates.Count - 1;
				while (pastID >= 0 && pastStates[pastID].Symbol == Symbols.Epsilon)
				{
					//if the last state we visited is the same as the state we started in here (i.e. X=>X), then it's a loop. Similarly
					//if we have an arbitrary number of epsilon moves but still reach the same state, same issue
					if (pastStates[pastID].State == state)
					{
						Console.WriteLine("Infinitely looping on epsilon transitions. Returning..");
						return new HashSet<State>(); //so we can't get anywhere new from here
					}
					pastID--;
				}
			}

			HashSet<State> ret = new HashSet<State>();

			if (input == Symbols.Epsilon)
				ret.Add(state);

			HashSet<State> nextStates = null, nextEpsilonStates = null;

			//add all the states we can get to from here.
			TransitionFunction.TryGetValue(new StateSymbolPair(state, Symbols.Epsilon), out nextEpsilonStates);
			TransitionFunction.TryGetValue(new StateSymbolPair(state, input), out nextStates);

			if (nextStates != null)
				ret = nextStates;

			if (nextEpsilonStates != null)
			{
				foreach (State epState in nextEpsilonStates)
				{
					StateSymbolPair currentState = new StateSymbolPair(state, Symbols.Epsilon);
					List<StateSymbolPair> updatedPastStates = new List<StateSymbolPair>(pastStates);
					updatedPastStates.Add(currentState);
					ret.UnionWith(GetStatesAccessibleFrom(epState, input, updatedPastStates));
				}
			}

			return ret;
		}

		public HashSet<State> GetStatesAccessibleFrom(State state, string input)
		{
			if (input.Length == 0)
				return GetStatesAccessibleFrom(state, Symbols.Epsilon);
			HashSet<State> allStates = new HashSet<State>();
			//keep a track of all our current pebbles (which are states mapped to the input remaining
			Stack<KeyValuePair<State, string>> pebbles = new Stack<KeyValuePair<State, State>>();
			pebbles.Push(new KeyValuePair<State, string>(state, input));
			while (pebbles.Count > 0)
			{
				var current = pebbles.Pop();
				if (current.Value.Length == 0)
					allStates.UnionWith(GetStatesAccessibleFrom(current.Key, Symbols.Epsilon));
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

