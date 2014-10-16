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
	class Program
	{
		static void Main(string[] args)
		{
            FSATransitionFunction transition = new FSATransitionFunction();
            transition.Add(1, 'a', 3);
            transition.Add(1, Symbols.Epsilon, 3);
            transition.Add(2, 'a', 1, 2);
            transition.Add(2, 'c', 3);
            transition.Add(3, 'b', 1, 2);

			FiniteStateAutomaton automaton = new FiniteStateAutomaton(
				AutomatonHelper.CreateStates(3), 
				new HashSet<char>(new []{'a', 'b', 'c'}),
				transition,
				"1", AutomatonHelper.CreateFinalStates(3));

			Console.WriteLine(automaton.IsWordInLanguage("ab"));
			FiniteStateAutomaton dfa = (FiniteStateAutomaton)automaton.CreateDeterministicAutomaton();
			Console.WriteLine(dfa.IsDeterministic(true));

			Game1 game = new Game1();
			game.Run();
		}
	}
}
