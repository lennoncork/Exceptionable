// Verifies Get Direct Contract Generic Throws Contributes Its Type Argument.

using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute<T> : Attribute { }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
}
class C
{
    [Exceptionable.Throws<System.IO.IOException>]
    [Exceptionable.Throws("System.ArgumentException")]
    public void Execute() { }
}

