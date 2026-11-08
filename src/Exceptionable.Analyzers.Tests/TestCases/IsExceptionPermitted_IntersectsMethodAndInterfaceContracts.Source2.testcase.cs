// Verifies Is Exception Permitted Intersects Method And Interface Contracts.

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
    [Exceptionable.Throws("System.IO.IOException")]
    void Execute();
}
class Implementation : IApi
{
    [Exceptionable.Throws("System.Exception")]
    public void Execute() { }
}

