using System;
using System.Collections.Generic;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Transformation.Text;
using Microsoft.ProgramSynthesis.VersionSpace;
using Microsoft.ProgramSynthesis.Learning;

using Microsoft.ProgramSynthesis.Compiler; // 필요한 네임스페이스 추가

namespace StringManipulationSynthesizer
{
    class Program
    {
        static void Main(string[] args)
        {
            var grammar = DSLCompiler.CompileGrammarFromFile("./grammar.grammar").Value;
            var engine = new SynthesisEngine(grammar);

            // 입력-출력 예제 정의
            var input = "example input string";
            var output = "expected output string";
            var examples = new Dictionary<State, object> {
                { State.CreateForExecution(grammar.InputSymbol, input), output }
            };

            // 프로그램 합성
            var scoreFeature = new RankingScore(grammar);
            var learner = new Learners(grammar);
            var result = learner.Learn(examples);
            var learnedProgram = result.RealizedPrograms.FirstOrDefault();

            Console.WriteLine(learnedProgram);
        }
    }
}
