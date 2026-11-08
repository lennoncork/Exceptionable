using System.Linq;
using Exceptionable.Analyzers.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Tests
{
    public class ExceptionFlowAnalysisTests
    {
        [Test]
        public void IsCaught_CatchInEnclosingTryHandlesTheException()
        {
            const string source = @"
using System;
class C
{
    void M()
    {
        try { throw new InvalidOperationException(); }
        catch (InvalidOperationException) { }
    }
}";
            var compilation = TestCompilation.Create(source);
            var throwOperation = getThrowOperation(compilation);
            var exceptionType = compilation.GetTypeByMetadataName("System.InvalidOperationException")!;

            Assert.That(ExceptionFlowAnalysis.IsCaught(throwOperation, exceptionType), Is.True);
        }

        [Test]
        public void IsCaught_CatchDoesNotCatchAnExceptionThrownFromItsOwnHandler()
        {
            const string source = @"
using System;
class C
{
    void M()
    {
        try { }
        catch { throw new InvalidOperationException(); }
    }
}";
            var compilation = TestCompilation.Create(source);
            var throwOperation = getThrowOperation(compilation);
            var exceptionType = compilation.GetTypeByMetadataName("System.InvalidOperationException")!;

            Assert.That(ExceptionFlowAnalysis.IsCaught(throwOperation, exceptionType), Is.False);
        }

        [Test]
        public void GetRethrownExceptionType_UsesCatchTypeAndCatchAllBaseType()
        {
            const string source = @"
using System;
class C
{
    void Typed()
    {
        try { }
        catch (InvalidOperationException) { throw; }
    }

    void CatchAll()
    {
        try { }
        catch { throw; }
    }
}";
            var compilation = TestCompilation.Create(source);
            var throws = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Select(node => (IThrowOperation)compilation.GetSemanticModel(node.SyntaxTree).GetOperation(node)!)
                .ToArray();

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
            const string source = @"
using System;
class CustomException : Exception { }
class C
{
    void M() { throw new CustomException(); }
}";
            var compilation = TestCompilation.Create(source);
            var throwOperation = getThrowOperation(compilation);

            Assert.That(
                ExceptionFlowAnalysis.GetThrownExceptionType(throwOperation.Exception!)?.Name,
                Is.EqualTo("CustomException"));
        }

        private static IThrowOperation getThrowOperation(Compilation compilation)
        {
            var node = compilation.SyntaxTrees.Single().GetRoot()
                .DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Single();
            return (IThrowOperation)compilation.GetSemanticModel(node.SyntaxTree).GetOperation(node)!;
        }
    }
}