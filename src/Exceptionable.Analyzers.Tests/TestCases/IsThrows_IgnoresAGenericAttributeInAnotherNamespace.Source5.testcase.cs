// Verifies Is Throws Ignores AGeneric Attribute In Another Namespace.

using System;
namespace Other
{
    [AttributeUsage(AttributeTargets.Method)]
    sealed class ThrowsAttribute<T> : Attribute { }
}
class C
{
    [Other.Throws<System.IO.IOException>]
    public void M() { }
}

