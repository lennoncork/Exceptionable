using System.Collections.Immutable;

namespace Exceptionable.Analyzers.Polyfills
{
    internal static class PolyfillCatalog
    {
        internal static ImmutableArray<PolyfillModule> Modules { get; } =
        [
            new SystemPolyfills(),
            new SystemCollectionsGenericPolyfills(),
            new SystemIOPolyfills(),
            new SystemLinqPolyfills(),
            new SystemThreadingTasksPolyfills(),
        ];
    }
}