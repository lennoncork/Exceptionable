using System.Collections.Generic;

namespace Exceptionable.Analyzers.Polyfills
{
    internal sealed class SystemIOPolyfills : PolyfillModule
    {
        internal override string Namespace => "System.IO";

        // FileNotFoundException and DirectoryNotFoundException derive from IOException.
        internal override IEnumerable<PolyfillApi> Apis { get; } =
        [
            Api("File", "ReadAllText", "System.IO.IOException", "System.UnauthorizedAccessException"),
            Api("File", "ReadAllLines", "System.IO.IOException", "System.UnauthorizedAccessException"),
            Api("File", "ReadAllBytes", "System.IO.IOException", "System.UnauthorizedAccessException"),
            Api("File", "WriteAllText", "System.IO.IOException", "System.UnauthorizedAccessException"),
            Api("File", "WriteAllBytes", "System.IO.IOException", "System.UnauthorizedAccessException"),
            Api("File", "Copy", "System.IO.IOException", "System.UnauthorizedAccessException"),
            Api("File", "Delete", "System.IO.IOException", "System.UnauthorizedAccessException"),
            Api("Directory", "CreateDirectory", "System.IO.IOException", "System.UnauthorizedAccessException"),
            Api("Directory", "GetFiles", "System.IO.IOException", "System.UnauthorizedAccessException"),
        ];
    }
}