using System;

namespace Exceptionable
{
    /// <summary>
    /// Declares an exception type that may escape the attributed class, struct, member, property, or event.
    /// On a delegate parameter, declares an exception the callback may throw when the method invokes it.
    /// Interface-wide contracts are not supported; annotate each interface member.
    /// Prefer <see cref="ThrowsAttribute{TException}"/> when the exception type is available at compile time.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Parameter,
        Inherited = true,
        AllowMultiple = true)]
    public sealed class ThrowsAttribute : Attribute
    {
        /// <summary>Initializes the contract with an exception type name.</summary>
        /// <param name="exceptionType">A fully qualified metadata name, dotted nested name, or unambiguous short name.</param>
        public ThrowsAttribute(string exceptionType)
        {
            this.ExceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
        }

        /// <summary>Gets the exception type name declared by this attribute.</summary>
        public string ExceptionType { get; }
    }
}