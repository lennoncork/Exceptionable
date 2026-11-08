using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class ConstructorHost
    {
        internal static ImmutableArray<IMethodSymbol> GetMethods(ISymbol symbol)
        {
            return symbol switch
            {
                IMethodSymbol method => [method],
                IFieldSymbol field => getInitializerConstructors(field.ContainingType, field.IsStatic),
                IPropertySymbol property => getInitializerConstructors(property.ContainingType, property.IsStatic),
                IEventSymbol eventSymbol => getInitializerConstructors(eventSymbol.ContainingType, eventSymbol.IsStatic),
                _ => [],
            };
        }

        private static ImmutableArray<IMethodSymbol> getInitializerConstructors(INamedTypeSymbol type, bool isStatic)
        {
            ImmutableArray<IMethodSymbol>.Builder builder = ImmutableArray.CreateBuilder<IMethodSymbol>();
            foreach (IMethodSymbol constructor in type.Constructors)
            {
                if (constructor.IsStatic != isStatic)
                {
                    continue;
                }

                if (!isStatic && !runsInstanceInitializers(constructor))
                {
                    continue;
                }

                builder.Add(constructor);
            }

            return builder.ToImmutable();
        }

        private static bool runsInstanceInitializers(IMethodSymbol constructor)
        {
            if (isRecordCopyConstructor(constructor))
            {
                return false;
            }

            ConstructorDeclarationSyntax[] declarations = [.. constructor.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .OfType<ConstructorDeclarationSyntax>()];
            if (declarations.Length == 0)
            {
                return true;
            }

            return declarations.Any(declaration =>
                declaration.Initializer is null ||
                !declaration.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer));
        }

        private static bool isRecordCopyConstructor(IMethodSymbol constructor)
        {
            // Only the compiler-generated copy constructor skips field initializers.
            // A constructor the user wrote with the same signature still runs them.
            return constructor.IsImplicitlyDeclared &&
                constructor.ContainingType.IsRecord &&
                constructor.Parameters.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(
                    constructor.Parameters[0].Type,
                    constructor.ContainingType);
        }
    }
}