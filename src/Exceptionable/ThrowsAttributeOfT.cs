namespace Exceptionable
{
    using System;

    /// <summary>Declares an exception type that may escape the attributed type, member, property, or event.</summary>
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