using System;

namespace Exceptionable
{
    /// <summary>
    /// Declares that no exception escapes the attributed class, struct, member, property, or event.
    /// Interface-wide contracts are not supported; annotate each interface member.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Event,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class DoesNotThrowAttribute : Attribute
    {
    }
}