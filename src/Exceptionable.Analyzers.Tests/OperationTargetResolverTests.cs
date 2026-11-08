using System.Linq;
using Exceptionable.Analyzers.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Tests
{
    public class OperationTargetResolverTests
    {
        [Test]
        public void GetTargetMethods_ResolvesGetterSetterAndCompoundAccessors()
        {
            string source = ExceptionableAnalyzerTests.GetTestCase("GetTargetMethods_ResolvesGetterSetterAndCompoundAccessors.Source1");
            var compilation = TestCompilation.Create(source);
            var syntaxTree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var propertyReferences = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Where(node => node.Name.Identifier.ValueText == "Value")
                .Select(node => (IPropertyReferenceOperation)semanticModel.GetOperation(node)!)
                .ToArray();

            var getterTargets = OperationTargetResolver.GetTargetMethods(propertyReferences[0]).ToArray();
            var setterTargets = OperationTargetResolver.GetTargetMethods(propertyReferences[1]).ToArray();
            var compoundTargets = OperationTargetResolver.GetTargetMethods(propertyReferences[2]).ToArray();

            Assert.That(getterTargets.Select(method => method.MethodKind), Is.EqualTo(new[] { MethodKind.PropertyGet }));
            Assert.That(setterTargets.Select(method => method.MethodKind), Is.EqualTo(new[] { MethodKind.PropertySet }));
            Assert.That(
                compoundTargets.Select(method => method.MethodKind),
                Is.EqualTo(new[] { MethodKind.PropertyGet, MethodKind.PropertySet }));
        }
    }
}