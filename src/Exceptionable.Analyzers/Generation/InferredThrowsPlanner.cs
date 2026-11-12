using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Exceptionable.Analyzers.Analysis;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Generation
{
    /// <summary>One inferred escaping exception for a member; equatable so generator steps can be cached.</summary>
    internal readonly struct InferredThrowEntry : IEquatable<InferredThrowEntry>
    {
        internal InferredThrowEntry(string memberId, string exceptionType)
        {
            this.MemberId = memberId;
            this.ExceptionType = exceptionType;
        }

        internal string MemberId { get; }

        internal string ExceptionType { get; }

        public bool Equals(InferredThrowEntry other) =>
            string.Equals(this.MemberId, other.MemberId, StringComparison.Ordinal) &&
            string.Equals(this.ExceptionType, other.ExceptionType, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is InferredThrowEntry other && this.Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(this.MemberId) * 31 +
            StringComparer.Ordinal.GetHashCode(this.ExceptionType);
    }

    /// <summary>Compares planned entry lists by value so an unchanged plan keeps later steps cached.</summary>
    internal sealed class InferredThrowEntrySetComparer : IEqualityComparer<ImmutableArray<InferredThrowEntry>>
    {
        internal static readonly InferredThrowEntrySetComparer Instance = new();

        private InferredThrowEntrySetComparer()
        {
        }

        public bool Equals(ImmutableArray<InferredThrowEntry> left, ImmutableArray<InferredThrowEntry> right)
        {
            if (left.IsDefault || right.IsDefault)
            {
                return left.IsDefault && right.IsDefault;
            }

            if (left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (!left[index].Equals(right[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(ImmutableArray<InferredThrowEntry> entries)
        {
            if (entries.IsDefault)
            {
                return 0;
            }

            int hash = entries.Length;
            foreach (InferredThrowEntry entry in entries)
            {
                hash = (hash * 31) + entry.GetHashCode();
            }

            return hash;
        }
    }

    /// <summary>Decides which members record inferred throws, independent of source generation.</summary>
    internal static class InferredThrowsPlanner
    {
        internal static ImmutableArray<InferredThrowEntry> GetEntries(
            ImmutableArray<SyntaxNode> throwNodes,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            ImmutableArray<IMethodSymbol> methods = DistinctExpandedMembers(throwNodes, compilation, cancellationToken);
            if (methods.IsEmpty)
            {
                return [];
            }

            // One resolver and one analysis cache for this compilation, shared by every member.
            var exceptionTypes = new ExceptionTypeResolver(compilation);
            var analysis = new InferredThrowAnalysis(compilation, new ReachabilityAnalysis(), exceptionTypes, MethodAnalysisPolicy.IsSupportedMethod);
            var contracts = new ContractResolver(exceptionTypes);
            ImmutableArray<InferredThrowEntry>.Builder entries = ImmutableArray.CreateBuilder<InferredThrowEntry>();
            foreach (IMethodSymbol method in methods)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!isExternallyConsumable(method, compilation) ||
                    contracts.GetEffectiveContract(method).HasContract)
                {
                    continue;
                }

                string? memberId = method.GetDocumentationCommentId();
                if (memberId is null)
                {
                    continue;
                }

                foreach (INamedTypeSymbol exceptionType in analysis.GetImplicitDeclaredTypes(method))
                {
                    entries.Add(new InferredThrowEntry(memberId, InferredThrowsMetadata.GetExceptionTypeName(exceptionType)));
                }
            }

            entries.Sort(static (left, right) =>
            {
                int member = string.CompareOrdinal(left.MemberId, right.MemberId);
                return member != 0
                    ? member
                    : string.CompareOrdinal(left.ExceptionType, right.ExceptionType);
            });
            return entries.ToImmutable();
        }

        /// <summary>
        /// Expands every throw to the method or initializer-running constructor that contains it,
        /// then returns each distinct member once.
        /// </summary>
        internal static ImmutableArray<IMethodSymbol> DistinctExpandedMembers(
            ImmutableArray<SyntaxNode> throwNodes,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            if (throwNodes.IsDefaultOrEmpty)
            {
                return [];
            }

            var methods = new List<IMethodSymbol>();
            var seen = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var models = new Dictionary<SyntaxTree, SemanticModel>();
            foreach (SyntaxNode throwNode in throwNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!models.TryGetValue(throwNode.SyntaxTree, out SemanticModel? model))
                {
                    model = compilation.GetSemanticModel(throwNode.SyntaxTree);
                    models.Add(throwNode.SyntaxTree, model);
                }

                foreach (IMethodSymbol method in expand(model.GetEnclosingSymbol(throwNode.SpanStart, cancellationToken)))
                {
                    if (seen.Add(method))
                    {
                        methods.Add(method);
                    }
                }
            }

            methods.Sort(static (left, right) => string.CompareOrdinal(
                left.GetDocumentationCommentId(),
                right.GetDocumentationCommentId()));
            return methods.ToImmutableArray();
        }

        private static bool isExternallyConsumable(IMethodSymbol method, Compilation compilation)
        {
            bool hasFriendAssembly = hasInternalsVisibleTo(compilation.Assembly);
            if (isPrivate(method.DeclaredAccessibility) ||
                requiresFriend(method.DeclaredAccessibility) && !hasFriendAssembly)
            {
                return false;
            }

            for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.ContainingType)
            {
                if (isPrivate(type.DeclaredAccessibility) ||
                    requiresFriend(type.DeclaredAccessibility) && !hasFriendAssembly)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool isPrivate(Accessibility accessibility) =>
            accessibility == Accessibility.Private;

        private static bool requiresFriend(Accessibility accessibility) =>
            accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal;

        private static bool hasInternalsVisibleTo(IAssemblySymbol assembly)
        {
            foreach (AttributeData attribute in assembly.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() ==
                        "System.Runtime.CompilerServices.InternalsVisibleToAttribute" &&
                    attribute.ConstructorArguments.Length > 0 &&
                    attribute.ConstructorArguments[0].Value is string)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<IMethodSymbol> expand(ISymbol? symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol method:
                    yield return method;
                    yield break;
                case IFieldSymbol field:
                    foreach (IMethodSymbol constructor in initializers(field.ContainingType, field.IsStatic))
                    {
                        yield return constructor;
                    }

                    yield break;
                case IPropertySymbol property:
                    if (property.GetMethod is not null)
                    {
                        yield return property.GetMethod;
                    }

                    if (property.SetMethod is not null)
                    {
                        yield return property.SetMethod;
                    }

                    foreach (IMethodSymbol constructor in initializers(property.ContainingType, property.IsStatic))
                    {
                        yield return constructor;
                    }

                    yield break;
            }
        }

        private static IEnumerable<IMethodSymbol> initializers(INamedTypeSymbol type, bool isStatic)
        {
            ImmutableArray<IMethodSymbol> constructors = isStatic ? type.StaticConstructors : type.Constructors;
            foreach (IMethodSymbol constructor in constructors)
            {
                if (ExecutableHostResolver.RunsInitializers(constructor))
                {
                    yield return constructor;
                }
            }
        }
    }
}