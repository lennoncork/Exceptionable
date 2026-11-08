using System.Linq;
using Exceptionable.Analyzers.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Tests
{
    public class ReachabilityAnalysisTests
    {
        [Test]
        public void IsReachable_RecognizesReachableAndUnreachableThrowOperations()
        {
            string source = ExceptionableAnalyzerTests.GetTestCase("IsReachable_RecognizesReachableAndUnreachableThrowOperations.Source1");
            var compilation = TestCompilation.Create(source);
            var throwOperations = compilation.SyntaxTrees.Single().GetRoot()
                .DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Select(node => (IThrowOperation)compilation.GetSemanticModel(node.SyntaxTree).GetOperation(node)!)
                .ToArray();
            var reachability = new ReachabilityAnalysis();

            Assert.That(reachability.IsReachable(throwOperations[0]), Is.True);
            Assert.That(reachability.IsReachable(throwOperations[1]), Is.False);
        }

        [Test]
        public void IsReachable_KeepsConstructorBodySeparateFromFieldInitializer()
        {
            string source = ExceptionableAnalyzerTests.GetTestCase("IsReachable_KeepsConstructorBodySeparateFromFieldInitializer.Source2");
            var compilation = TestCompilation.Create(source);
            var syntaxRoot = compilation.SyntaxTrees.Single().GetRoot();
            var model = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);
            IOperation initializer = model.GetOperation(
                syntaxRoot.DescendantNodes().OfType<EqualsValueClauseSyntax>().Single())!;
            IThrowOperation[] throws = [.. syntaxRoot.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Select(node => (IThrowOperation)model.GetOperation(node)!)];
            var reachability = new ReachabilityAnalysis();

            Assert.That(reachability.IsReachable(initializer), Is.True);
            Assert.That(reachability.IsReachable(throws[0]), Is.True);
            Assert.That(reachability.IsReachable(throws[1]), Is.False);
        }
    }
}