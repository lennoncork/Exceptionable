using System.Collections.Immutable;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis.CSharp;

namespace Exceptionable.Analyzers.Tests
{
    public class ContractResolverTests
    {
        private const string source = @"
using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    sealed class DoesNotThrowAttribute : Attribute { }
}

[Exceptionable.Throws(""System.IO.IOException"")]
class Base
{
    public virtual void Inherited() { }
    public virtual void Overridden() { }
}
class Derived : Base
{
    public override void Inherited() { }
    [Exceptionable.Throws(""System.ArgumentException"")]
    public override void Overridden() { }
}
interface IParent
{
    [Exceptionable.Throws(""System.IO.IOException"")]
    void Execute();
}
interface IChild : IParent { }
class Implementation : IChild
{
    public void Execute() { }
}";

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
            const string intersectionSource = @"
using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
    [AttributeUsage(AttributeTargets.Method)]
    sealed class DoesNotThrowAttribute : Attribute { }
}
interface IApi
{
    [Exceptionable.Throws(""System.IO.IOException"")]
    void Execute();
}
class Implementation : IApi
{
    [Exceptionable.Throws(""System.Exception"")]
    public void Execute() { }
}";
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
            const string propertySource = @"
using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Method)]
    sealed class DoesNotThrowAttribute : Attribute { }
}
[Exceptionable.DoesNotThrow]
class C
{
    [Exceptionable.Throws(""System.IO.IOException"")]
    public int Value { [Exceptionable.DoesNotThrow] get => 0; set { } }

    [Exceptionable.Throws(""System.InvalidOperationException"")]
    public event Action Changed;
}";
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
    }
}