using System.Linq;
using Exceptionable.Analyzers.Analysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Tests
{
    public class ReachabilityAnalysisTests
    {
        [Test]
        public void IsReachable_RecognizesReachableAndUnreachableThrowOperations()
        {
            const string source = @"
using System;
class C
{
    void M()
    {
        throw new InvalidOperationException();
        throw new ArgumentException();
    }
}";
            var compilation = TestCompilation.Create(source);
            var method = TestCompilation.GetMethod(compilation, "C", "M");
            var throwOperations = compilation.SyntaxTrees.Single().GetRoot()
                .DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Select(node => (IThrowOperation)compilation.GetSemanticModel(node.SyntaxTree).GetOperation(node)!)
                .ToArray();
            var reachability = new ReachabilityAnalysis();

            Assert.That(reachability.IsReachable(throwOperations[0], method), Is.True);
            Assert.That(reachability.IsReachable(throwOperations[1], method), Is.False);
        }
    }
}