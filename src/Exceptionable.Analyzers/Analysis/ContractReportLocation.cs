using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Analysis
{
    /// <summary>
    /// Finds the source declaration a contract diagnostic should name. An accessor with no
    /// contract of its own is reported on its property or event, which is where the attribute goes.
    /// </summary>
    internal static class ContractReportLocation
    {
        internal static Location? TryGet(
            IMethodSymbol method,
            ExceptionableCompilationContext compilationContext)
        {
            ISymbol target = method;
            if (method.AssociatedSymbol is ISymbol associated &&
                !compilationContext.ContractResolver.GetDirectContract(method).HasContract)
            {
                target = associated;
            }

            foreach (Location location in target.Locations)
            {
                if (location.IsInSource &&
                    location.SourceTree is SyntaxTree tree &&
                    compilationContext.Options.IsEnabled(tree))
                {
                    return location;
                }
            }

            return null;
        }
    }
}