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
			Automaton.TransitionMap transition = new Automaton.TransitionMap(){
				{new StatePair(1, 'a'), new HashSet<int>(){3}},
				{new StatePair(1, Automaton.Epsilon), new HashSet<int>(){3}},
				{new StatePair(2, 'a'), new HashSet<int>(){1, 2}},
				{new StatePair(2, 'c'), new HashSet<int>(){3}},
				{new StatePair(3, 'b'), new HashSet<int>(){1, 2}},
			};

			Automaton automaton = new Automaton(
				new HashSet<int>(new []{1, 2, 3}), 
				new HashSet<char>(new []{'a', 'b', 'c'}),
				transition,
				1, new HashSet<int>(){ 3 });

			Console.WriteLine(automaton.IsDeterministic());
			Console.WriteLine(automaton.IsWordInLanguage("ab"));
			Console.ReadLine();
		}
	}
}
