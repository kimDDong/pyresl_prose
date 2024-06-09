using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Learning;
using System.Collections.Generic;
using System;
using Microsoft.ProgramSynthesis.Rules;
using System.Linq;
using System.Text.RegularExpressions;

namespace StringManipulation
{
    public class WitnessFunctions : DomainLearningLogic
    {
        private static Dictionary<Tuple<State, string, int, object>, HashSet<object>> cache;
        private static Dictionary<Tuple<State, string, int, object, object>, int> cache2;
        private int threshold = 20;

        private bool AlreadyHandled(State inputState, string operName, int paramIndex, object output, object value)
        {
            var index = new Tuple<State, string, int, object, object>(inputState, operName, paramIndex, output, value);
            if (!cache2.ContainsKey(index))
            {
                cache2[index] = 0;
            }
            cache2[index]++;
            if (cache2[index] > threshold)
            {
                return true;
            }
            return false;
        }

        private void AddIfNew(State inputState, string operName, int paramIndex, object output, object value, ref HashSet<object> values)
        {
            var index = new Tuple<State, string, int, object>(inputState, operName, paramIndex, output);

            if (!cache.ContainsKey(index))
            {
                cache[index] = new HashSet<object>();
            }
            if (!cache[index].Contains(value))
            {
                cache[index].Add(value);
                values.Add(value);
            }
        }

        public WitnessFunctions(Grammar grammar) : base(grammar)
        {
            cache = new Dictionary<Tuple<State, string, int, object>, HashSet<object>>();
            cache2 = new Dictionary<Tuple<State, string, int, object, object>, int>();
        }

        [WitnessFunction("Nth_str", 1)]
        internal DisjunctiveExamplesSpec WitnessNth_str(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.DisjunctiveExamples)
            {
                State inputState = example.Key;
                var vs = (object[])inputState[rule.Body[0]];

                var occurrences = new HashSet<int>();
                foreach (var val in example.Value)
                {
                    var strValue = (string)val;
                    for (int i = 0; i < vs.Length; i++)
                    {
                        if (vs[i] is string && ((string)vs[i]).Equals(strValue))
                        {
                            occurrences.Add(i);
                        }
                    }
                }
                if (occurrences.Count == 0) return null;
                result[inputState] = occurrences.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction("ConstStr", 0)]
        internal DisjunctiveExamplesSpec WitnessConstStr(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.DisjunctiveExamples)
            {
                State inputState = example.Key;
                result[inputState] = example.Value;
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction("ToUpperCase", 0)]
        internal DisjunctiveExamplesSpec WitnessToUpperCase(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            var operName = "ToUpperCase";
            var paramIndex = 0;
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.DisjunctiveExamples)
            {
                State inputState = example.Key;
                var strs = new HashSet<string>();

                foreach (var output_str in example.Value)
                {
                    var output_string = (string)output_str;
                    if (output_string.All(i => !('a' <= i && i <= 'z')) &&
                        output_string.Any(i => ('a' <= i && i <= 'z') || ('A' <= i && i <= 'Z')))
                    {
                        AddIfNew(inputState, operName, paramIndex, output_string, output_string, ref strs);
                        AddIfNew(inputState, operName, paramIndex, output_string, output_string.ToLower(), ref strs);
                    }
                }
                if (strs.Count == 0) return null;
                result[inputState] = strs;
            }
            return new DisjunctiveExamplesSpec(result);
        }

        // Implementations of other WitnessFunctions go here...

        static Regex[] UsefulRegexes = {
            // List of useful regex patterns
        };

        // Helper function to build matches for string
        static void BuildStringMatches(string x,
                                        out List<Tuple<Match, Regex>>[] leftMatches,
                                        out List<Tuple<Match, Regex>>[] rightMatches)
        {
            // Implementation goes here
            leftMatches = new List<Tuple<Match, Regex>>[x.Length + 1];
            rightMatches = new List<Tuple<Match, Regex>>[x.Length + 1];
            for (int p = 0; p <= x.Length; ++p)
            {
                leftMatches[p] = new List<Tuple<Match, Regex>>();
                rightMatches[p] = new List<Tuple<Match, Regex>>();
            }
            foreach (Regex r in UsefulRegexes)
            {
                foreach (Match m in r.Matches(x))
                {
                    leftMatches[m.Index + m.Length].Add(Tuple.Create(m, r));
                    rightMatches[m.Index].Add(Tuple.Create(m, r));
                }
            }
        }

        [WitnessFunction("RegexPosition", 1)]
        DisjunctiveExamplesSpec WitnessRegexPair(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            var operName = "RegexPosition";
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.DisjunctiveExamples)
            {
                State inputState = example.Key;
                var x = (string)inputState[rule.Body[0]];
                List<Tuple<Match, Regex>>[] leftMatches, rightMatches;
                BuildStringMatches(x, out leftMatches, out rightMatches);
                var regexes = new List<Tuple<Regex, Regex>>();
                foreach (int? pos in example.Value)
                {
                    foreach (var l in leftMatches[pos.Value])
                    {
                        foreach (var r in rightMatches[pos.Value])
                        {
                            if (!AlreadyHandled(inputState, operName, 11, pos, l.Item2) &&
                                !AlreadyHandled(inputState, operName, 12, pos, r.Item2))
                            {
                                regexes.Add(Tuple.Create(l.Item2, r.Item2));
                            }
                        }
                    }
                }
                if (regexes.Count == 0) return null;
                result[inputState] = regexes;
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction("RegexPosition", 2, DependsOnParameters = new[] { 1 })]
        DisjunctiveExamplesSpec WitnessKForRegexPair(GrammarRule rule, DisjunctiveExamplesSpec spec,
                                             ExampleSpec rrSpec)
        {
            var operName = "RegexPosition";
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.DisjunctiveExamples)
            {
                State inputState = example.Key;
                var x = (string)inputState[rule.Body[0]];
                var regexPair = (Tuple<Regex, Regex>)rrSpec.Examples[inputState];
                Regex left = regexPair.Item1;
                Regex right = regexPair.Item2;
                var rightMatches = right.Matches(x).Cast<Match>().ToDictionary(m => m.Index);
                var matchPositions = new List<int>();
                foreach (Match m in left.Matches(x))
                {
                    if (rightMatches.ContainsKey(m.Index + m.Length))
                        matchPositions.Add(m.Index + m.Length);
                }
                var ks = new HashSet<int?>();
                foreach (int? pos in example.Value)
                {
                    int occurrence = matchPositions.BinarySearch(pos.Value);
                    if (occurrence < 0) continue;
                    if (!AlreadyHandled(inputState, operName, 2, pos, occurrence) &&
                        !AlreadyHandled(inputState, operName, 2, pos, occurrence - matchPositions.Count))
                    {
                        ks.Add(occurrence);
                        ks.Add(occurrence - matchPositions.Count);
                    }
                }
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }
    }
}
