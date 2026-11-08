// Verifies Try Get Throws Type Reads The Exception Type Argument.

using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute<T> : Attribute { }
}
class C
{
    [Exceptionable.Throws<System.IO.IOException>]
    public void M() { }
}

