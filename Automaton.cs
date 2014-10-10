using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using State = System.Int32;
using Symbol = System.Char;


namespace Waffles
{
	using StatePair = System.Collections.Generic.KeyValuePair<State, Symbol>;
	//generalised automaton - can create specific DFAs or NFAs (no epsilon moves)
	class Automaton
	{
		//neatness
		public class TransitionMap : Dictionary<StatePair, HashSet<State>> { }

		public const Symbol Epsilon = 'ε';

		public static HashSet<State> CreateNumberOfStates(int numberOfStates)
		{
			return new HashSet<State>(Enumerable.Range(0, numberOfStates));
		}

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

		public bool IsDeterministic()
		{
			//return false if either: a) any state has multiple transitions on a single symbol
			//b) any state has < |Σ| transitions (then either it's missing a transition..or more, by the pigeonhole principle - then we've got a))
			//c) any epsilon moves
			if(TransitionFunction.Any(s => s.Value.Count > 1 || s.Key.Value == Automaton.Epsilon) || TransitionFunction.Keys.Count == (States.Count * Alphabet.Count))
				return false;

			//we can assume that the transition function is valid (no invalid states/symbols), so next we just need to check that every (state, symbol) pair is represented
			foreach (var transition in TransitionFunction.Keys.GroupBy(k => k.Key))
			{
				IEnumerable<Symbol> symbols = transition.Select(s => s.Value);
				if (symbols.Count() != Alphabet.Count)
					return false;
			}
			return true;
		}

		public bool IsWordInLanguage(List<Symbol> input)
		{
			return IsWordInLanguage(input, StartState);
			//if we consider epsilon-moves last and ignore any X->ep->X moves, should avoid any infinite loop issues

		}

		public bool IsWordInLanguage(List<Symbol> input, State startState, List<StatePair> pastStates = null)
		{
			//also check our past transitions to avoid getting stuck in a loop of epsilon transitions
			if (pastStates == null)
				pastStates = new List<StatePair>();
			else
			{
				//this is the state we immediately transitioned from
				int pastID = pastStates.Count-1;
				while(pastID >= 0 && pastStates[pastID].Value == Automaton.Epsilon)
				{
					//if the last state we visited is the same as the state we started in here (i.e. X=>X), then it's a loop. Similarly
					//if we have an arbitrary number of epsilon moves but still reach the same state, same issue
					if (pastStates[pastID].Key == startState)
						return false;
					pastID--;
				}
			}

			HashSet<State> nextStates = null, nextEpsilonStates = new HashSet<State>();

			//add epsilon states if we have them; if we don't, and we have no regular transitions, return
			if (TransitionFunction.ContainsKey(new KeyValuePair<State, Symbol>(startState, Automaton.Epsilon)))
			{
				nextEpsilonStates = TransitionFunction[new KeyValuePair<State, Symbol>(startState, Automaton.Epsilon)];
			}

			//no epsilon moves, no input either
			if (input.Count == 0)
			{
				if (nextEpsilonStates.Count == 0)
					return FinalStates.Contains(startState);
			}
			else
			{
				TransitionFunction.TryGetValue(new KeyValuePair<State, Symbol>(startState, input.First()), out nextStates);
			}

			
			if((nextStates == null || nextStates.Count == 0) && nextEpsilonStates.Count == 0)
				 return false; //if we have input left but no moves, not accepted

			List<Symbol> newWord = new List<Symbol>(input.Skip(1));

			if (nextStates != null)
			{
				foreach (State state in nextStates)
				{
					//we're going to take the state we're currently in + the symbol we are consuming
					StatePair currentState = new StatePair(startState, input.First());
					List<StatePair> updatedPastStates = new List<StatePair>(pastStates);
					updatedPastStates.Add(currentState);

					//and now continue
					Console.WriteLine("At state {0}, using symbol {1} and transitioning to state {2} (input remaining is {3})", startState, input.First(), state, 
						new string(newWord.ToArray()));
					if (IsWordInLanguage(newWord, state, updatedPastStates))
						return true;
				}
			}

			foreach(State state in nextEpsilonStates)
			{
				StatePair currentState = new StatePair(startState, Automaton.Epsilon);
				List<StatePair> updatedPastStates = new List<StatePair>(pastStates);
				updatedPastStates.Add(currentState);
				Console.WriteLine("At state {0}, using symbol {1} and transitioning to state {2} (input remaining is {3})", startState, Automaton.Epsilon, state, new string(input.ToArray()));
				if (IsWordInLanguage(input, state, updatedPastStates))
					return true;
			}

			return false;
		}
	}
}
