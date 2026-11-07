namespace Exceptionable
{
    using System;

    /// <summary>Declares that no exception escapes the attributed type or method.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class DoesNotThrowAttribute : Attribute
    {
    }
}