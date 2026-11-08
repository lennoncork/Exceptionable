// Verifies Get Effective Contract Explicit Contract Replaces Inferred Throws.

using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
    [AttributeUsage(AttributeTargets.Constructor)]
    sealed class DoesNotThrowAttribute : Attribute { }
}
class Host
{
    public Host(string name) { }
    [Exceptionable.DoesNotThrow]
    public Host(int value) { }
    [Exceptionable.Throws("System.IO.IOException")]
    public Host(bool flag) { }
    [Exceptionable.Throws("System.Exception")]
    public Host(long value) { }
    public void Run() { }
}

