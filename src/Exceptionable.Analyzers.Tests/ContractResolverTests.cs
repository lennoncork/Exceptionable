using System.Linq;
using Exceptionable.Analyzers.Contracts;

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
            var compilation = TestCompilation.Create(source);
            var resolver = new ContractResolver(new ExceptionTypeResolver(compilation));
            var contract = resolver.GetEffectiveContract(
                TestCompilation.GetMethod(compilation, "Derived", "Overridden"));

            Assert.That(contract.DeclaredTypes.Single().ToDisplayString(), Is.EqualTo("System.ArgumentException"));
        }

        [Test]
        public void GetEffectiveContract_InheritsContainingTypeContractWhenMethodHasNone()
        {
            var compilation = TestCompilation.Create(source);
            var resolver = new ContractResolver(new ExceptionTypeResolver(compilation));
            var contract = resolver.GetEffectiveContract(
                TestCompilation.GetMethod(compilation, "Derived", "Inherited"));

            Assert.That(contract.DeclaredTypes.Single().ToDisplayString(), Is.EqualTo("System.IO.IOException"));
        }

        [Test]
        public void GetImplementedInterfaceContracts_ResolvesInheritedInterfaceMember()
        {
            var compilation = TestCompilation.Create(source);
            var resolver = new ContractResolver(new ExceptionTypeResolver(compilation));
            var contracts = resolver.GetImplementedInterfaceContracts(
                TestCompilation.GetMethod(compilation, "Implementation", "Execute"));

            Assert.That(contracts.Length, Is.EqualTo(1));
            Assert.That(contracts[0].DeclaredTypes.Single().ToDisplayString(), Is.EqualTo("System.IO.IOException"));
        }
    }
}