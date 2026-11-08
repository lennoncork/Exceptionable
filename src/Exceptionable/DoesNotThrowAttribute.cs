namespace Exceptionable
{
    using System;

    /// <summary>Declares that no exception escapes the attributed type, member, property, or event.</summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Event,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class DoesNotThrowAttribute : Attribute
    {
    }
}