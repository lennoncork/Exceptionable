using System.Linq;
using Exceptionable.Analyzers.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Tests
{
    public class ExceptionFlowAnalysisTests
    {
        [Test]
        public void IsCaught_CatchInEnclosingTryHandlesTheException()
        {
            string source = AnalyzerTestHarness.GetTestCase("IsCaught_CatchInEnclosingTryHandlesTheException.Source1");
            CSharpCompilation compilation = TestCompilation.Create(source);
            IThrowOperation throwOperation = getThrowOperation(compilation);
            INamedTypeSymbol exceptionType = compilation.GetTypeByMetadataName("System.InvalidOperationException")!;

            Assert.That(ExceptionFlowAnalysis.IsCaught(throwOperation, exceptionType), Is.True);
        }

        [Test]
        public void IsCaught_CatchDoesNotCatchAnExceptionThrownFromItsOwnHandler()
        {
            string source = AnalyzerTestHarness.GetTestCase("IsCaught_CatchDoesNotCatchAnExceptionThrownFromItsOwnHandler.Source2");
            CSharpCompilation compilation = TestCompilation.Create(source);
            IThrowOperation throwOperation = getThrowOperation(compilation);
            INamedTypeSymbol exceptionType = compilation.GetTypeByMetadataName("System.InvalidOperationException")!;

            Assert.That(ExceptionFlowAnalysis.IsCaught(throwOperation, exceptionType), Is.False);
        }

        [Test]
        public void GetRethrownExceptionType_UsesCatchTypeAndCatchAllBaseType()
        {
            string source = AnalyzerTestHarness.GetTestCase("GetRethrownExceptionType_UsesCatchTypeAndCatchAllBaseType.Source3");
            CSharpCompilation compilation = TestCompilation.Create(source);
            IThrowOperation[] throws = [.. compilation.SyntaxTrees.Single().GetRoot().DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Select(node => (IThrowOperation)compilation.GetSemanticModel(node.SyntaxTree).GetOperation(node)!)];

            Assert.That(
                ExceptionFlowAnalysis.GetRethrownExceptionType(throws[0], compilation)?.ToDisplayString(),
                Is.EqualTo("System.InvalidOperationException"));
            Assert.That(
                ExceptionFlowAnalysis.GetRethrownExceptionType(throws[1], compilation)?.ToDisplayString(),
                Is.EqualTo("System.Exception"));
        }

        [Test]
        public void GetThrownExceptionType_UsesThrownExpressionType()
        {
            string source = AnalyzerTestHarness.GetTestCase("GetThrownExceptionType_UsesThrownExpressionType.Source4");
            CSharpCompilation compilation = TestCompilation.Create(source);
            IThrowOperation throwOperation = getThrowOperation(compilation);

            Assert.That(
                ExceptionFlowAnalysis.GetThrownExceptionType(throwOperation.Exception!, compilation)?.Name,
                Is.EqualTo("CustomException"));
        }

        private static IThrowOperation getThrowOperation(Compilation compilation)
        {
            ThrowStatementSyntax node = compilation.SyntaxTrees.Single().GetRoot()
                .DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Single();
            return (IThrowOperation)compilation.GetSemanticModel(node.SyntaxTree).GetOperation(node)!;
        }
    }
}