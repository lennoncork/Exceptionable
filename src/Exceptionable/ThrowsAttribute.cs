namespace Exceptionable
{
    using System;

    /// <summary>Declares an exception type that may escape the attributed type or method.</summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method,
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