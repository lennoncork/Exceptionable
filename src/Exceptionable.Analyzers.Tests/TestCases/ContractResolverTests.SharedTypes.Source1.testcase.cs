// Verifies Contract Resolver Tests.Shared Types.

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

[Exceptionable.Throws("System.IO.IOException")]
class Base
{
    public virtual void Inherited() { }
    public virtual void Overridden() { }
}
class Derived : Base
{
    public override void Inherited() { }
    [Exceptionable.Throws("System.ArgumentException")]
    public override void Overridden() { }
}
interface IParent
{
    [Exceptionable.Throws("System.IO.IOException")]
    void Execute();
}
interface IChild : IParent { }
class Implementation : IChild
{
    public void Execute() { }
}

