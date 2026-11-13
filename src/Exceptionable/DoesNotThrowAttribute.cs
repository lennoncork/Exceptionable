using System;

namespace Exceptionable
{
    /// <summary>
    /// Declares that no exception escapes the attributed class, struct, member, property, or event.
    /// On a delegate parameter, declares that the callback does not throw when the method invokes it.
    /// Interface-wide contracts are not supported; annotate each interface member.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Parameter,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class DoesNotThrowAttribute : Attribute
    {
    }
}