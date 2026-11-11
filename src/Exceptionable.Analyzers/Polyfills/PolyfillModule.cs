using System.Collections.Generic;

namespace Exceptionable.Analyzers.Polyfills
{
    /// <summary>
    /// Exception information for the external APIs of one namespace. Add a module per namespace
    /// and register it in <see cref="PolyfillCatalog"/>.
    /// </summary>
    internal abstract class PolyfillModule
    {
        /// <summary>Gets the namespace whose types this module describes. It is also the configuration key.</summary>
        internal abstract string Namespace { get; }

        internal abstract IEnumerable<PolyfillApi> Apis { get; }

        /// <summary>Describes every overload of <paramref name="memberName"/>.</summary>
        protected static PolyfillApi Api(string typeName, string memberName, params string[] exceptions) =>
            new(typeName, memberName, null, exceptions);

        /// <summary>Describes only the overloads of <paramref name="memberName"/> with <paramref name="parameterCount"/> parameters.</summary>
        protected static PolyfillApi Api(string typeName, string memberName, int parameterCount, params string[] exceptions) =>
            new(typeName, memberName, parameterCount, exceptions);
    }
}