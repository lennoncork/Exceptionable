// Verifies that a base method [DoesNotThrow] is not the override's contract,
// a base method [Throws] is, and a parent class [DoesNotThrow] is inherited.
using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    sealed class DoesNotThrowAttribute : Attribute { }
}
class Base
{
    [Exceptionable.DoesNotThrow]
    public virtual void Safe() { }
    [Exceptionable.Throws("System.IO.IOException")]
    public virtual void Fallible() { }
}
class Derived : Base
{
    public override void Safe() { }
    public override void Fallible() { }
}
[Exceptionable.DoesNotThrow]
class Parent
{
    public virtual void Work() { }
}
class Child : Parent
{
    public override void Work() { }
}
