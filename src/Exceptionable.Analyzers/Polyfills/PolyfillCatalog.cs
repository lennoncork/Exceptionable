namespace Exceptionable.Analyzers.Polyfills
{
    internal static class PolyfillCatalog
    {
        internal static PolyfillModule[] Modules { get; } =
        [
            new SystemPolyfills(),
            new SystemCollectionsGenericPolyfills(),
            new SystemIOPolyfills(),
            new SystemLinqPolyfills(),
            new SystemThreadingTasksPolyfills(),
        ];
    }
}