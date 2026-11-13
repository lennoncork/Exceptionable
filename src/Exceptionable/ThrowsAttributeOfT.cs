using System;

namespace Exceptionable
{
    /// <summary>
    /// Declares an exception type that may escape the attributed class, struct, member, property, or event.
    /// On a delegate parameter, declares an exception the callback may throw when the method invokes it.
    /// Interface-wide contracts are not supported; annotate each interface member.
    /// Applying this generic attribute requires a target framework that supports generic attributes (.NET 7 or later).
    /// </summary>
    /// <typeparam name="TException">The exception type that may escape. It must derive from <see cref="Exception"/>.</typeparam>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Parameter,
        Inherited = true,
        AllowMultiple = true)]
    public sealed class ThrowsAttribute<TException> : Attribute
        where TException : Exception
    {
    }
}