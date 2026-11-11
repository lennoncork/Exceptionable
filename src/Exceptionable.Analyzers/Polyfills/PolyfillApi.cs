namespace Exceptionable.Analyzers.Polyfills
{
    /// <summary>
    /// Exception information for one external member group, matched by type and member name.
    /// </summary>
    internal sealed class PolyfillApi
    {
        internal PolyfillApi(string typeName, string memberName, int? parameterCount, string[] exceptions)
        {
            this.TypeName = typeName;
            this.MemberName = memberName;
            this.ParameterCount = parameterCount;
            this.Exceptions = exceptions;
        }

        /// <summary>Gets the type's metadata name without its namespace, such as <c>Dictionary`2</c>.</summary>
        internal string TypeName { get; }

        /// <summary>Gets the method or accessor metadata name, such as <c>First</c> or <c>get_Item</c>.</summary>
        internal string MemberName { get; }

        /// <summary>Gets the parameter count that limits the match, or <see langword="null"/> for every overload.</summary>
        internal int? ParameterCount { get; }

        /// <summary>Gets the namespace-qualified metadata names of the exceptions the member may throw.</summary>
        internal string[] Exceptions { get; }
    }
}