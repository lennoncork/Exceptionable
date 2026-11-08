// Verifies Get Effective Contract Redeclared Interface Member Keeps Base Contract.

using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
}
interface IBase
{
    [Exceptionable.Throws("System.IO.IOException")]
    void Execute();
}
interface IDerived : IBase
{
    void Execute();
}
interface IReplaced : IBase
{
    [Exceptionable.Throws("System.ArgumentException")]
    void Execute();
}

