using System.Collections.Immutable;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis.CSharp;

namespace Exceptionable.Analyzers.Tests
{
    public class ContractResolverTests
    {
        private static readonly string source = ExceptionableAnalyzerTests.GetTestCase("ContractResolverTests.SharedTypes.Source1");

        [Test]
        public void GetEffectiveContract_MethodDeclarationOverridesContainingTypeContract()
        {
            CSharpCompilation compilation = TestCompilation.Create(source);
            ContractResolver resolver = new(new ExceptionTypeResolver(compilation));
            ExceptionContract contract = resolver.GetEffectiveContract(
                TestCompilation.GetMethod(compilation, "Derived", "Overridden"));

            Assert.That(contract.DeclaredTypes.Single().ToDisplayString(), Is.EqualTo("System.ArgumentException"));
        }

        [Test]
        public void GetEffectiveContract_InheritsContainingTypeContractWhenMethodHasNone()
        {
            CSharpCompilation compilation = TestCompilation.Create(source);
            ContractResolver resolver = new(new ExceptionTypeResolver(compilation));
            ExceptionContract contract = resolver.GetEffectiveContract(
                TestCompilation.GetMethod(compilation, "Derived", "Inherited"));

            Assert.That(contract.DeclaredTypes.Single().ToDisplayString(), Is.EqualTo("System.IO.IOException"));
        }

        [Test]
        public void GetImplementedInterfaceContracts_ResolvesInheritedInterfaceMember()
        {
            CSharpCompilation compilation = TestCompilation.Create(source);
            ContractResolver resolver = new(new ExceptionTypeResolver(compilation));
            ImmutableArray<ExceptionContract> contracts = resolver.GetImplementedInterfaceContracts(
                TestCompilation.GetMethod(compilation, "Implementation", "Execute"));

            Assert.That(contracts.Length, Is.EqualTo(1));
            Assert.That(contracts[0].DeclaredTypes.Single().ToDisplayString(), Is.EqualTo("System.IO.IOException"));
        }

        [Test]
        public void IsExceptionPermitted_IntersectsMethodAndInterfaceContracts()
        {
            string intersectionSource = ExceptionableAnalyzerTests.GetTestCase("IsExceptionPermitted_IntersectsMethodAndInterfaceContracts.Source2");
            CSharpCompilation compilation = TestCompilation.Create(intersectionSource);
            ContractResolver resolver = new(new ExceptionTypeResolver(compilation));
            Microsoft.CodeAnalysis.IMethodSymbol method = TestCompilation.GetMethod(compilation, "Implementation", "Execute");
            Microsoft.CodeAnalysis.INamedTypeSymbol ioException = compilation.GetTypeByMetadataName("System.IO.IOException")!;
            Microsoft.CodeAnalysis.INamedTypeSymbol exception = compilation.GetTypeByMetadataName("System.Exception")!;

            Assert.That(resolver.IsExceptionPermitted(method, ioException), Is.True);
            Assert.That(resolver.IsExceptionPermitted(method, exception), Is.False);
        }

        [Test]
        public void GetEffectiveContract_PropertyAttributeAppliesToAccessorsUntilOverridden()
        {
            string propertySource = ExceptionableAnalyzerTests.GetTestCase("GetEffectiveContract_PropertyAttributeAppliesToAccessorsUntilOverridden.Source3");
            CSharpCompilation compilation = TestCompilation.Create(propertySource);
            ContractResolver resolver = new(new ExceptionTypeResolver(compilation));

            Assert.That(
                resolver.GetEffectiveContract(TestCompilation.GetMethod(compilation, "C", "get_Value")).IsDoesNotThrow,
                Is.True);
            Assert.That(
                resolver.GetEffectiveContract(TestCompilation.GetMethod(compilation, "C", "set_Value"))
                    .DeclaredTypes.Single().Name,
                Is.EqualTo("IOException"));
            Assert.That(
                resolver.GetEffectiveContract(TestCompilation.GetMethod(compilation, "C", "add_Changed"))
                    .DeclaredTypes.Single().Name,
                Is.EqualTo("InvalidOperationException"));
        }

        [Test]
        public void GetDirectContract_GenericThrowsContributesItsTypeArgument()
        {
            string genericSource = ExceptionableAnalyzerTests.GetTestCase("GetDirectContract_GenericThrowsContributesItsTypeArgument.Source4");
            CSharpCompilation compilation = TestCompilation.Create(genericSource);
            ContractResolver resolver = new(new ExceptionTypeResolver(compilation));
            ExceptionContract contract = resolver.GetDirectContract(
                TestCompilation.GetMethod(compilation, "C", "Execute"));

            Assert.That(
                contract.DeclaredTypes.Select(type => type.ToDisplayString()).ToArray(),
                Is.EqualTo(new[] { "System.IO.IOException", "System.ArgumentException" }));
        }

        [Test]
        public void GetEffectiveContract_RedeclaredInterfaceMemberKeepsBaseContract()
        {
            string redeclaredSource = ExceptionableAnalyzerTests.GetTestCase("GetEffectiveContract_RedeclaredInterfaceMemberKeepsBaseContract.Source5");
            CSharpCompilation compilation = TestCompilation.Create(redeclaredSource);
            ContractResolver resolver = new(new ExceptionTypeResolver(compilation));

            ExceptionContract inherited = resolver.GetEffectiveContract(
                TestCompilation.GetMethod(compilation, "IDerived", "Execute"));
            ExceptionContract replaced = resolver.GetEffectiveContract(
                TestCompilation.GetMethod(compilation, "IReplaced", "Execute"));

            Assert.That(inherited.DeclaredTypes.Single().ToDisplayString(), Is.EqualTo("System.IO.IOException"));
            Assert.That(replaced.DeclaredTypes.Single().ToDisplayString(), Is.EqualTo("System.ArgumentException"));
        }

        [Test]
        public void GetEffectiveContract_DisjointRedeclaredContractsAllowNothing()
        {
            string disjointSource = ExceptionableAnalyzerTests.GetTestCase("GetEffectiveContract_DisjointRedeclaredContractsAllowNothing.Source6");
            CSharpCompilation compilation = TestCompilation.Create(disjointSource);
            ContractResolver resolver = new(new ExceptionTypeResolver(compilation));
            ExceptionContract contract = resolver.GetEffectiveContract(
                TestCompilation.GetMethod(compilation, "IDerived", "Execute"));

            Assert.That(contract.IsDoesNotThrow, Is.True);
        }

        [Test]
        public void GetEffectiveContract_ExplicitContractReplacesInferredThrows()
        {
            string implicitSource = ExceptionableAnalyzerTests.GetTestCase("GetEffectiveContract_ExplicitContractReplacesInferredThrows.Source7");
            CSharpCompilation compilation = TestCompilation.Create(implicitSource);
            Microsoft.CodeAnalysis.INamedTypeSymbol argumentNull =
                compilation.GetTypeByMetadataName("System.ArgumentNullException")!;
            ContractResolver resolver = new(
                new ExceptionTypeResolver(compilation),
                method => method.MethodKind == Microsoft.CodeAnalysis.MethodKind.Constructor
                    ? [argumentNull]
                    : []);
            Microsoft.CodeAnalysis.INamedTypeSymbol host = compilation.GetTypeByMetadataName("Host")!;

            ExceptionContract unmarked = resolver.GetEffectiveContract(constructor(host, "string"));
            ExceptionContract doesNotThrow = resolver.GetEffectiveContract(constructor(host, "int"));
            ExceptionContract ioException = resolver.GetEffectiveContract(constructor(host, "bool"));
            ExceptionContract exception = resolver.GetEffectiveContract(constructor(host, "long"));
            ExceptionContract method = resolver.GetEffectiveContract(
                TestCompilation.GetMethod(compilation, "Host", "Run"));

            Assert.That(unmarked.DeclaredTypes.Select(type => type.ToDisplayString()).ToArray(),
                Is.EqualTo(new[] { "System.ArgumentNullException" }));
            Assert.That(doesNotThrow.IsDoesNotThrow, Is.True);
            Assert.That(ioException.DeclaredTypes.Select(type => type.ToDisplayString()).ToArray(),
                Is.EqualTo(new[] { "System.IO.IOException" }));
            Assert.That(exception.DeclaredTypes.Select(type => type.ToDisplayString()).ToArray(),
                Is.EqualTo(new[] { "System.Exception" }));
            Assert.That(method.HasContract, Is.False);
        }

        private static Microsoft.CodeAnalysis.IMethodSymbol constructor(
            Microsoft.CodeAnalysis.INamedTypeSymbol type,
            string parameterType)
        {
            return type.Constructors.Single(candidate =>
                candidate.Parameters.Length == 1 &&
                candidate.Parameters[0].Type.ToDisplayString() == parameterType);
        }
    }
}