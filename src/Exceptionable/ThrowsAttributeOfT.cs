namespace Exceptionable
{
    using System;

    /// <summary>
    /// Declares an exception type that may escape the attributed class, struct, member, property, or event.
    /// Interface-wide contracts are not supported; annotate each interface member.
    /// Applying this generic attribute requires a target framework that supports generic attributes (.NET 7 or later).
    /// </summary>
    /// <typeparam name="TException">The exception type that may escape. It must derive from <see cref="Exception"/>.</typeparam>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Event,
        Inherited = true,
        AllowMultiple = true)]
    public sealed class ThrowsAttribute<TException> : Attribute
        where TException : Exception
    {
    }
}