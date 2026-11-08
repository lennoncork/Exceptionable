// Verifies Get Effective Contract Property Attribute Applies To Accessors Until Overridden.

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
    [Exceptionable.Throws("System.IO.IOException")]
    public int Value { [Exceptionable.DoesNotThrow] get => 0; set { } }

    [Exceptionable.Throws("System.InvalidOperationException")]
    public event Action Changed;
}

