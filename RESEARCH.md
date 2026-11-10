# Exceptionable Analyzer Research Guide

This document is a source map for designing and implementing **Exceptionable**,
a Roslyn analyzer modeled after .NET nullable analysis. It records useful
patterns from existing analyzer repositories and authoritative documentation,
with links pinned to repository revisions where possible. Use the linked
material as examples, not as requirements: analyzer APIs, supported Roslyn
versions, and packaging needs must be chosen for this project.

For the intended feature set and staged implementation, see
[DESIGN.md](./DESIGN.md).

## Recommended research order

1. Read the [Microsoft analyzer and code-fix tutorial](#microsoft-roslyn-documentation)
   for the SDK concepts and lifecycle.
2. Read [nullable analysis attributes](#nullable-analysis-the-primary-model)
   for the primary API-contract model Exceptionable is adapting.
3. Use the [Roslyn Analyzer Cookbook](#roslynanalyzercookbook) for a small,
   concrete analyzer and its tests.
4. Use [StyleCop.Analyzers](#stylecopanalyzers) for production rule
   conventions, configuration, localization, and compatibility decisions.
5. Use [Roslynator](#roslynator) for a large analyzer suite, package layout,
   and mature testing/developer workflows.
6. Before implementing exception-flow analysis, search the official Roslyn
   APIs and these repositories for symbol resolution, operations, control-flow
   graphs, analyzer actions, and test coverage; do not infer exception behavior
   from syntax alone.

## Nullable analysis: the primary model

The closest precedent for Exceptionable's contract-annotation approach is C#
Nullable static analysis and the attributes in
`System.Diagnostics.CodeAnalysis`. Nullable analysis tracks a flow state
(`not-null` or `maybe-null`) and uses annotations to express API preconditions
and postconditions. This lets a library communicate richer contracts than
nullable type syntax alone and improves diagnostics at call sites.

The official reference explains the attribute families and their meanings:
[Attributes interpreted by the compiler: Nullable static analysis](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/nullable-analysis).
Useful examples include:

- **Preconditions:** [`AllowNullAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.allownullattribute)
  and [`DisallowNullAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.disallownullattribute).
- **Postconditions:** [`MaybeNullAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.maybenullattribute)
  and [`NotNullAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.notnullattribute).
- **Conditional postconditions:** [`MaybeNullWhenAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.maybenullwhenattribute)
  and [`NotNullWhenAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.notnullwhenattribute).
- **Control-flow effects:** [`DoesNotReturnAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.doesnotreturnattribute)
  and [`DoesNotReturnIfAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.doesnotreturnifattribute).

The [nullable reference types overview](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types)
describes nullable contexts and compiler flow analysis; the
[nullable compiler options](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/language)
document the project-level configuration surface.

### What Exceptionable should borrow

- Treat annotations as machine-readable API contracts, not merely comments.
- Define each annotation's precondition/postcondition or control-flow meaning
  precisely, including how it affects callers and implementations.
- Make analysis opt-in/configurable at project level and support standard
  per-diagnostic severity configuration, analogous to nullable enablement and
  warning configuration. Per-file control is `.editorconfig`. The nullable
  analogy does not include a source directive; see
  [source directives](#source-directives-and-nullable).
- Validate annotations and test the resulting flow behavior with paired
  positive and negative consumer examples.
- Prefer compiler-visible metadata so contracts can be consumed across
  project boundaries, subject to Exceptionable's chosen attribute packaging.
- When an interface member publishes a contract, require the source
  implementation to declare an equivalent contract. Nullable analysis already
  does this for nullability annotations and for `[DoesNotReturn]`.

### Interface and override contract matching

The C# compiler reports a dedicated family of warnings when an implementation
or override does not repeat the nullability of the member it implements. The
catalog is
[Nullable reference type warnings](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings).
The closest examples for Exceptionable are:

- [CS8612](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings)-[CS8617](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings)
  and [CS8766](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings)-[CS8769](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings):
  the nullability of a type, return, or parameter does not match an
  implemented or implicitly implemented member.
- [CS8770](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings):
  a method lacks `[DoesNotReturn]` to match an implemented or overridden
  member. This is the closest attribute-level precedent: the interface (or
  override) published an annotation, and the implementation must declare it
  too.

EXC009 follows that declaration-matching pattern for `[Throws]` and
`[DoesNotThrow]` on interface implementations. It compares declared contracts,
not inferred throws. It does not change class virtual/override replacement,
which [DESIGN.md](./DESIGN.md) still treats as an explicit replacement rather
than a mismatch. Do not treat the compiler's override warnings as a
requirement to diagnose Exceptionable override replacement.

### Important difference from nullable analysis

The nullable attributes are interpreted by the C# compiler's built-in nullable
analysis. The documentation explicitly notes that adding those attributes
provides semantic information to callers but does **not** enable additional
checks on the annotated method's implementation. Exceptionable is a separate
Roslyn analyzer and its design intentionally includes both caller-side checks
and checks that an implementation honors its declared exception contract.
Do not assume nullable compiler behavior automatically applies to Exceptionable;
implement and test each side of the contract.

Also, nullability has language-level type states and defined flow semantics,
whereas C# has no checked-exception system. Exceptionable must define its own
known-exception sources, propagation rules, catch coverage, and conservative
unknown behavior. Nullable attributes are a model for annotation-driven API
contracts and configuration—not a ready-made exception-flow algorithm.

## Source directives and `#nullable`

A `#exceptionable` directive that behaves like `#nullable` is not available to
this analyzer. `#nullable` is a C# language directive. Exceptionable can keep
the project-and-editorconfig enablement model. It cannot add a directive the
compiler will accept, and it cannot track a nullable-style context from one
line to the next.

### What `#nullable` controls

`#nullable enable`, `#nullable disable`, and `#nullable restore` set the
nullable annotation and warning contexts from that line until the next
`#nullable` directive or the end of the file. `restore` returns the selected
flags to the project `<Nullable>` setting. Each directive may apply to both
flags or only `annotations` or `warnings`. The
[preprocessor reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives#nullable-context)
and the
[nullable reference types specification](https://github.com/dotnet/csharplang/blob/29df547564c4ffc51b1dedf8369dc91e8f0ba854/proposals/csharp-8.0/nullable-reference-types-specification.md)
define that behavior, in the nullable compiler directives section.

The compiler parses `#nullable` as its own syntax
(`NullableDirectiveTrivia`) and stores the resulting context per source
position. Analyzers can read the result with
[`SemanticModel.GetNullableContext`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel.getnullablecontext?view=roslyn-dotnet-4.7.0).
EXC007 reads the annotation flag at the constructor declaration. A
non-nullable reference parameter is `NullableAnnotation.NotAnnotated`.
`[AllowNull]`, documented with the other nullable analysis attributes linked
above, still allows a caller to pass null, so that parameter keeps the
inferred `ArgumentNullException` contract described in DESIGN.md. There is no
API to define another context of that kind.

Roslyn's directive parser, at
[`DirectiveParser.cs`](https://github.com/dotnet/roslyn/blob/6d158e8441092b11dcd98fe2b540f51e5ae24608/src/Compilers/CSharp/Portable/Parser/DirectiveParser.cs),
handles only the directives the language knows: `#if`, `#elif`, `#else`,
`#endif`, `#region`, `#endregion`, `#define`, `#undef`, `#error`, `#warning`,
`#line`, `#pragma`, script `#r` and `#load`, `#nullable`, `#!`, and `#:`.
`#nullable` is the `NullableKeyword` case and calls `ParseNullableDirective`.
Any other word after `#` becomes `BadDirectiveTrivia` with CS1024
(`ERR_PPDirectiveExpected`). The
[preprocessor error catalog](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/preprocessor-errors)
uses `#import` as the example of that error.

The C# standard's
[pre-processing directives](https://github.com/dotnet/csharpstandard/blob/f885375267570784d8d529d94893555494781abb/standard/lexical-structure.md#65-pre-processing-directives)
name a closed set: conditional compilation, line, diagnostic, region, pragma,
and nullable. `#nullable` is
[§6.5.9](https://github.com/dotnet/csharpstandard/blob/f885375267570784d8d529d94893555494781abb/standard/lexical-structure.md#659-nullable-directive).
`#exceptionable` is not in that grammar. Roslyn's extra script and file-based
forms (`#r`, `#load`, `#!`, `#:`) are still fixed cases in the same parser.
An analyzer does not register a case.

### Forms that do not add the directive

- `#exceptionable enable` is CS1024. The parser reports it before analyzers
  run, including when the line sits in disabled `#if` text, because disabled
  text is still scanned for directives.
- `#pragma exceptionable disable` is warning CS1633 (`WRN_IllegalPragma`).
  The standard's
  [pragma section](https://github.com/dotnet/csharpstandard/blob/f885375267570784d8d529d94893555494781abb/standard/lexical-structure.md#6510-pragma-directives)
  leaves pragma text to the compiler and says an unrecognized pragma may warn
  but must not be a compile-time error. It also says a pragma must not change
  program semantics. Roslyn does not hand that text to analyzers. Its
  [pragma documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives#pragmas)
  says the compiler must already support the pragma. The parser's recovery
  builds a warning-pragma node, which is not a stable custom-pragma syntax.
- `#:exceptionable enable` uses the ignored-directive prefix from the C# 14
  [ignored directives](https://github.com/dotnet/csharplang/blob/93d55a09e48c7f36f312bffe2e5b83e8d18031b1/proposals/csharp-14.0/ignored-directives.md)
  speclet. The language ignores `#:` so file-based-app tooling can read a
  header. The same speclet requires the directive before the first token and
  before any `#if`, and it says Roslyn errors when the file is part of a
  project. That error is CS9298. It also says unrecognized directives should
  still be rejected so official .NET tooling can reserve them. The user-facing
  preprocessor page describes project-based `#:` use more loosely as a
  warning; the error code and `ParseIgnoredDirective` are the behavior for
  Exceptionable's project compilations. A header-only token in a file-based
  app is not a `#nullable` region.
- `#define` and `#undef` must appear before the first token (CS1032) and only
  introduce conditional-compilation symbols. They do not carry an
  enable/disable state through later lines.

Adding a real directive would be a C# language and Roslyn compiler change.
The ignored-directive speclet says new directive spellings belong with the
language design team. An analyzer package cannot ship that change.

### Per-file enablement that already exists

[DESIGN.md](./DESIGN.md) section 9 is the configuration contract:
`<Exceptionable>` for the project, and `exceptionable_analysis = enable` or
`disable` in `.editorconfig` for a file or directory. A recognized editor
value overrides the project property in either direction. That is the
file-scoped part of what `#nullable` does relative to `<Nullable>`. It does
not select a span inside one file.

`AnalysisOptions.IsEnabled` reads those settings per syntax tree, and the
diagnostic paths consult it before reporting. Attributes are still metadata.
A disabled file's `[Throws]` or `[DoesNotThrow]` remains visible to an enabled
caller. `#nullable disable annotations` is different: it changes how
annotations in that region are interpreted. Exceptionable has no separate
annotation context, because its annotations are ordinary attributes.

### In-source suppression is not an enable switch

`#pragma warning disable EXC001` and `#pragma warning restore EXC001` suppress
listed analyzer diagnostics across a span. That is the supported in-source
mechanism, and unknown ids are accepted by `#pragma warning`. It does not
enable analysis for a file whose `<Exceptionable>` and `exceptionable_analysis`
settings leave analysis off. DESIGN requires that per-diagnostic suppression
not turn the master switch on. A pragma must name each id; a later rule is
not covered until the pragma lists it.

A comment convention such as `// exceptionable: disable` could be scanned from
trivia and could imitate regions and `restore`. It would be an Exceptionable
comment rule, not a preprocessor directive: the compiler would not check it,
and it would be easy to confuse with a commented-out line. Do not treat that
convention as `#nullable` behavior unless DESIGN adds it as its own
configuration rule.

## Cross-repository practices

### Start with a clear analyzer host and packaging target

The projects demonstrate different valid choices:

- The Cookbook is a solution-local example and integrates the analyzer project
  into the host solution without making a NuGet package.
- StyleCop targets `netstandard1.1`, describing compatibility with older
  Roslyn hosts as a reason for that target.
- Roslynator has a NuGet analyzer package and places analyzer assemblies in
  `analyzers/dotnet/...` paths, including Roslyn-version-specific variants.

These are examples, not a target-framework ranking. Choose the supported
Roslyn/IDE/build hosts first, then select target frameworks and package layout
to match. In particular, keep the library containing the public exception
attributes and the analyzer's deployment model explicit in the design.

### Keep rule metadata centralized and diagnostics stable

The Cookbook centralizes `DiagnosticDescriptor` instances, which keeps IDs
and rule metadata discoverable and simplifies tests. StyleCop also illustrates
the production-facing parts of a descriptor: localized title and message,
category, severity, default enablement, and help link. StyleCop documents
standard Roslyn severity configuration rather than inventing a separate
configuration mechanism.

For this project, treat diagnostic IDs, messages, locations, severity, and
configuration as a user-facing compatibility surface. Keep descriptor
definitions together and test the exact diagnostic ID, arguments, and source
location.

### Register narrow actions and use semantic APIs for contracts

The Cookbook contains a symbol-action example that examines property
attributes; StyleCop contains a syntax-action example for a particular syntax
kind. Both enable concurrent execution and state their generated-code
analysis policy. Use the narrowest action that has the required information.
For exception contracts, resolve attribute classes and exception types to
symbols and analyze operations/control flow where necessary; matching source
text or a short attribute name is not a robust substitute for semantic
identity.

Initialize compilation-specific symbol state in a compilation-scoped action.
Do not retain symbols between compilations. Explicitly decide whether
generated code is analyzed and enable concurrency only when analyzer state is
safe for concurrent calls.

### Test behaviors as compilations, including negative cases

The Cookbook demonstrates analyzer tests that compile source snippets and
assert expected diagnostic IDs, arguments, and locations; it also shows
supplying metadata references and package dependencies. StyleCop maintains
tests across multiple C# language-version projects. Roslynator publishes a
testing framework and documentation for analyzers, refactorings, and code
fixes.

For the exception analyzer, tests should exercise complete snippets rather
than isolated syntax assumptions: attribute resolution, inheritance and
precedence, caught and propagated exceptions, subtype matching, invalid
annotations, and no diagnostic for unknown/unannotated exception sources.
Pin language-version and reference-assembly inputs where they affect results.

### Treat package and contributor guidance as part of implementation

StyleCop and Roslynator show that a released analyzer needs installation and
configuration documentation, compatibility guidance, contribution/test
expectations, and package metadata in addition to analyzer code. Roslynator
also recommends discussing larger analyzer additions before implementation.
Use these guides when this repository moves from design into a distributable
package or accepts external contributions.

## Repository references

### StyleCopAnalyzers

Repository: [DotNetAnalyzers/StyleCopAnalyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)

- [README](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/958b4ec02ebb48b5fb432e61059e355444deae63/README.md)
  — installation, standard severity configuration, C# version guidance,
  release links, and contribution entry point. The README notes that fixes are
  provided where possible.
- [Analyzer project](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/958b4ec02ebb48b5fb432e61059e355444deae63/src/StyleCop.Analyzers/StyleCop.Analyzers.csproj)
  — target framework and analyzer project conventions.
- [SA1124 analyzer](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/958b4ec02ebb48b5fb432e61059e355444deae63/src/StyleCop.Analyzers/ReadabilityRules/SA1124DoNotUseRegions.cs)
  — focused syntax registration, descriptor setup, generated-code policy, and
  concurrent execution.
- [Analyzer test project](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/958b4ec02ebb48b5fb432e61059e355444deae63/test/StyleCop.Analyzers.Test/StyleCop.Analyzers.Test.csproj)
  — test-project setup; inspect the solution for language-version-specific
  test projects as well.
- [Contributing guide](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/958b4ec02ebb48b5fb432e61059e355444deae63/CONTRIBUTING.md)
  — contribution workflow.
- [Getting started](https://dotnetanalyzers.github.io/StyleCopAnalyzers/) and
  [configuration](https://dotnetanalyzers.github.io/StyleCopAnalyzers/Configuration.html)
  — user-facing installation and configuration guidance.

### RoslynAnalyzerCookbook

Repository: [tom-englert/RoslynAnalyzerCookbook](https://github.com/tom-englert/RoslynAnalyzerCookbook)

- [README and walkthrough](https://github.com/tom-englert/RoslynAnalyzerCookbook/blob/cf68f3289bc505b679bd1c655b4e25a35c80d681/README.md)
  — small analyzer scaffold, descriptor organization, semantic property
  analysis, tests, solution integration, dependency references, and pitfalls.
  The README explicitly describes its focus as solution-local analyzers while
  noting that most topics also apply to packages/extensions.
- [Solution integration props](https://github.com/tom-englert/RoslynAnalyzerCookbook/blob/cf68f3289bc505b679bd1c655b4e25a35c80d681/src/Directory.Build.props)
  — project-reference setup that loads the analyzer without referencing its
  output as a normal assembly.
- [Analyzer project](https://github.com/tom-englert/RoslynAnalyzerCookbook/blob/cf68f3289bc505b679bd1c655b4e25a35c80d681/src/SolutionAnalyzer/SolutionAnalyzer/SolutionAnalyzer.csproj)
  — minimal project and Roslyn package references.
- [Example analyzer](https://github.com/tom-englert/RoslynAnalyzerCookbook/blob/cf68f3289bc505b679bd1c655b4e25a35c80d681/src/SolutionAnalyzer/SolutionAnalyzer/EnforceDescriptionAnalyzer.cs)
  — symbol action and attribute inspection.
- [Diagnostic definitions](https://github.com/tom-englert/RoslynAnalyzerCookbook/blob/cf68f3289bc505b679bd1c655b4e25a35c80d681/src/SolutionAnalyzer/SolutionAnalyzer/Diagnostics.cs)
  — centralized descriptors.
- [Analyzer tests](https://github.com/tom-englert/RoslynAnalyzerCookbook/blob/cf68f3289bc505b679bd1c655b4e25a35c80d681/src/SolutionAnalyzer/SolutionAnalyzer.Test/EnforceDescriptionAnalyzerTest.cs)
  — source inputs, expected diagnostic assertions, locations, and references.

The Cookbook includes a version-map link when discussing which
`Microsoft.CodeAnalysis.*` version to reference. Treat that mapping as
third-party guidance and verify current host requirements before choosing a
dependency version.

### Roslynator

Repository: [dotnet/roslynator](https://github.com/dotnet/roslynator)

- [README](https://github.com/dotnet/roslynator/blob/b7ba8866006f39b4ffec30743b00cbf82070db5c/README.md)
  — analyzer NuGet packages, testing framework, configuration docs,
  developer docs, and guidance to discuss larger contributions first.
- [Analyzer project](https://github.com/dotnet/roslynator/blob/b7ba8866006f39b4ffec30743b00cbf82070db5c/src/Analyzers/Analyzers.csproj)
  and [code-fix project](https://github.com/dotnet/roslynator/blob/b7ba8866006f39b4ffec30743b00cbf82070db5c/src/Analyzers.CodeFixes/Analyzers.CodeFixes.csproj)
  — separation of analyzer logic and code fixes in a mature suite.
- [Analyzer package manifest](https://github.com/dotnet/roslynator/blob/b7ba8866006f39b4ffec30743b00cbf82070db5c/src/Analyzers.CodeFixes/Roslynator.Analyzers.nuspec)
  — package metadata and analyzer assembly placement.
- [Shared analyzer base](https://github.com/dotnet/roslynator/blob/b7ba8866006f39b4ffec30743b00cbf82070db5c/src/Analyzers/CSharp/Analysis/BaseDiagnosticAnalyzer.cs)
  — shared analyzer infrastructure; useful for discovering common setup
  patterns, not necessarily worth copying into a small initial project.
- [Contributing guide](https://github.com/dotnet/roslynator/blob/b7ba8866006f39b4ffec30743b00cbf82070db5c/CONTRIBUTING.md)
  — contribution expectations, including tests and changelog updates.
- Documentation: [configuration](https://josefpihrt.github.io/docs/roslynator/configuration),
  [guides](https://josefpihrt.github.io/docs/roslynator/category/guides),
  [testing](https://josefpihrt.github.io/docs/roslynator/testing), and
  [developer documentation](https://josefpihrt.github.io/docs/roslynator/developers).

Roslynator is most useful as a scale and packaging reference. Its additional
projects, version-specific assets, and shared abstractions are not a
recommended starting complexity for this analyzer.

## Microsoft Roslyn documentation

- [Tutorial: Write your first analyzer and code fix](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
  — official end-to-end introduction to analyzer diagnostics and optional
  code fixes.
- [Roslyn SDK documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
  — use as the entry point for current Roslyn SDK guidance.
- [Roslyn repository](https://github.com/dotnet/roslyn) — authoritative
  implementation source for compiler-platform APIs and tests. Search by API
  symbol and check the API version targeted by this project.

When implementing this design, specifically research the current documentation
and API source for `DiagnosticAnalyzer`, analyzer initialization and action
registration, `CompilationStartAction`, `AttributeData`,
`ISymbol.GetAttributes`, `IOperation`, `ControlFlowGraph`, and diagnostic
configuration. Use the Roslyn analyzer testing packages' documentation and
examples to verify expected diagnostics and locations. This list points to
search targets; it does not prescribe a particular Roslyn API version.

## Inferred contracts in compiled assemblies

A source generator adds source that is compiled with the rest of the project.
The [Roslyn SDK overview](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/#source-generators)
describes that as compile-time metaprogramming: code created at compile time
and added to the compilation. The Roslyn source-generator cookbook, at
[`source-generators.cookbook.md`](https://github.com/dotnet/roslyn/blob/04b3076a13a7fb376613cb17003059000db1beaa/docs/features/source-generators.cookbook.md),
puts code rewriting outside that design. Exceptionable therefore records an
inferred `[Throws]` contract as assembly metadata instead of editing the
member that throws.

The generator stores `ISymbol.GetDocumentationCommentId()`
([API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.getdocumentationcommentid)).
That value uses the compiler's XML documentation id, including `M:` for a
method and `#ctor` for a constructor
([ID strings](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings)).
A referencing compilation matches the same id on the metadata method. A
closed generic exception display name is not always a metadata type name, so
resolution of that recorded type can fail. DESIGN.md records that limit.

## Exception-handling smells

This section maps exception-handling smells onto Exceptionable's contract
model. It is input for the open questions in
[DESIGN.md](./DESIGN.md). It does not change current behavior.

Checked-exception catalogs from Java are names and examples, not
requirements. C# has no throws clause, and Exceptionable already limits
itself to explicit throws and known contracts.

### Instance constructors are expected to throw

The [constructor design guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/constructor)
say to throw from an instance constructor when construction cannot succeed.
That page reprints the 2nd edition and notes that a later edition exists;
the current [best practices for exceptions](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
still do not list instance constructors among methods that should not throw.
[Parameter design](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/parameter-design)
tells public members to throw `ArgumentException` or a subclass when
validation fails. `FileStream`, `Uri`, and `Regex` constructors throw for
reasons other than a bad argument.

[CA1065](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1065)
lists the methods that should not throw: property getters, event accessors,
`Equals`, `GetHashCode`, `ToString`, static constructors, finalizers,
`Dispose`, equality operators, and implicit conversions. Instance
constructors are absent. Property getters and event accessors are qualified:
`InvalidOperationException` (including `ObjectDisposedException`),
`NotSupportedException`, and, for indexers, `ArgumentException` and
`KeyNotFoundException` are accepted. The
[property guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/property)
say to avoid getter exceptions and then exempt indexers.

Exceptionable already publishes an escaping constructor throw as inferred
`[Throws]`, including throws in field and property initializers that the
constructor runs. EXC007 is the extra case for a nullable-redundant
`ArgumentNullException`. `[DoesNotThrow]` is already valid on a constructor
or on the type. That is the opt-in for a codebase that wants a no-throw
constructor. Inferring `[DoesNotThrow]` for every constructor would report
ordinary argument checks and failing construction as EXC001.

### Static constructors, finalizers, and Dispose

The same constructor page says not to throw from a static constructor. A
throw is wrapped and the type becomes unusable in the application domain.
Callers do not invoke the static constructor as a normal call, so recording
the inner exception as that constructor's `[Throws]` does not tell callers
what they can catch. A diagnostic belongs on the static constructor and on
static field initializers. Modeling `TypeInitializationException` on later
member access is a separate feature.

[CA1065](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1065)
says a finalizer throw causes the CLR to fail fast. Callers cannot catch it.
An explicit `[Throws]` on a finalizer would describe a contract the caller
never observes.

The [Dispose pattern](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern)
says to avoid throwing from `Dispose(bool)` except when the process is
already corrupted, and never to throw when `disposing` is false, because
that path runs from a finalizer. `Dispose` runs from `finally`, `using`,
and `foreach`. The C# specification says an exception that escapes a
`finally` replaces any exception already being propagated; see
[finally replacement](#finally-replacement-and-filter-loss) below.
`using` and `foreach` already treat `Dispose` as a call, so a known
`Dispose` contract is visible at those sites.

The same Dispose page says to throw `ObjectDisposedException` from members
that cannot be used after dispose. A class-level
`[Throws(typeof(ObjectDisposedException))]` already permits that on every
member. Treating it as an ambient permitted exception would hide it from
callers.

`IAsyncDisposable.DisposeAsync` waits on the async model. An exception in
an async method, including one before the first `await`, is stored on the
returned task.

### Publishing `[DoesNotThrow]` without a closed body

`[DoesNotThrow]` means no exception escapes. A body that calls an
unspecified member does not prove that. Many framework `Dispose`
implementations throw. Publishing `[DoesNotThrow]` for every source
`Dispose`, or for every member that merely lacks an explicit throw, would
make EXC006 report catches that are still required and would contradict the
rule that an unspecified call is not non-throwing.

A sound publication is the same bar EXC006 already uses: every reachable
operation is non-throwing or a `[DoesNotThrow]` call. Real `Dispose`
methods usually call unspecified cleanup and would not meet that bar. The
useful rule on those methods is a diagnostic when a known throw or known
throwing call escapes. External methods, and calls through an unannotated
interface such as `IDisposable`, stay unspecified.

If a default is added later, an explicit member `[Throws]` or
`[DoesNotThrow]` wins. A class-level `[Throws]` should not by itself permit
`Dispose`, a finalizer, or a static constructor to throw. Inferred
metadata stays ineligible for EXC009, as inferred throws are today.

`Equals`, `GetHashCode`, `ToString`, equality operators, and implicit
conversions are the same family as `Dispose`: CA1065 says they should not
throw, and a known escape is worth a diagnostic. Explicit conversion
operators are expected to throw `OverflowException` or
`InvalidCastException`, as the
[CA2201](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2201)
table describes. Property getters and event accessors need the allowed
exception families above, so a blanket no-throw default does not fit them.

### Finally replacement and filter loss

The [throw statement](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/statements#13106-the-throw-statement)
says that when a `finally` throws, processing of the current exception is
terminated. The [try statement](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/statements#1311-the-try-statement)
says the new exception propagates and the exception already being
propagated is lost. DESIGN.md already uses that rule for explicit throws.
[CA2219](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2219)
reports a throw in `finally`, a filter, or a fault. Its
[implementation](https://source.dot.net/Microsoft.CodeAnalysis.NetAnalyzers/Microsoft.CodeQuality.Analyzers/QualityGuidelines/DoNotRaiseExceptionsInExceptionClauses.cs.html)
only records an `IThrowOperation` inside `finally`. The comment says a C#
filter cannot throw. A filter can call a throwing method or contain a
throw expression. Exceptionable can already see a `[Throws]` call in
`finally`, including `Dispose` from `using` and `foreach`. That is the
contract-shaped extension of CA2219.

The same try-statement section says that when a filter expression throws,
that exception is caught and the filter evaluates to false. The original
exception keeps propagating. The
[exception-throwing guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exception-throwing)
and the
[user-filtered handler documentation](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/using-user-filtered-exception-handlers)
state the same runtime result: the filter failure is indistinguishable
from an explicit `false`. `ExceptionFlowAnalysis.IsCaught` treats only an
operation in the `try` body as handled, so a throw or known call in the
filter is eligible for EXC001, EXC002, EXC003, and inferred `[Throws]`.
An outer `catch` can also suppress that diagnostic when the filter
exception never leaves the filter. The spec result is that the filter
exception is discarded.

### Smells already owned by other analyzers

These affect quality and leave the exception type Exceptionable tracks
unchanged, or they are resource rules rather than contract rules:

- [CA2200](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2200)
  preserves the stack on `throw;`. The best-practices page shows `throw ex`
  resetting the stack. The thrown type is the same.
- [CA2201](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2201)
  rejects `Exception`, `SystemException`, `ApplicationException`, and
  runtime-reserved types such as `NullReferenceException` and
  `IndexOutOfRangeException`. `throw null` is already
  `NullReferenceException` in this analyzer. A reserved type is a weak
  contract, and CA2201 already reports the throw.
- [CA1031](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1031)
  rejects `catch (Exception)` and a bare `catch`, with a configurable
  disallowed-name list. A catch that handles a known contract still
  satisfies EXC003.
- CA2000, CA2213, and CA1816 track object lifetime and
  `GC.SuppressFinalize`. They are outside exception contracts.

### Literature names worth keeping

Eiji Adachi Barbosa, Alessandro Garcia, and coauthors describe swallowed
exceptions and destructive remapping in
[Categorizing Faults in Exception Handling](https://doi.org/10.1109/SBES.2014.19)
(SBES 2014). A swallowed exception is a `catch` that ignores the failure,
including a base-type catch that hides a more specific failure. Destructive
remapping throws a new exception from the message alone and drops the
original. They note that hiding the original is sometimes deliberate.

Roberta Coelho, Jonathan Rocha, and Hugo Melo list empty handlers, throws
from `finally`, and destructive wrapping in
[A Catalogue of Java Exception Handling Bad Smells and Refactorings](https://www.plopcon.org/pastplops/2018/papers/13.6.pdf)
(PLoP 2018). The refactorings assume Java's checked exceptions and
try-with-resources. Use the smell names, and keep the C# rules above as
the semantics.

Two catch rules could be built on known contracts later. Both need the
EXC006 closed-body bar, because an unspecified call can throw anything:

- An empty `catch` that handles a known escaping exception and does not
  observe it. Any other statement, including logging, has to count as
  handling or the diagnostic fights deliberate recovery.
- A `catch` type disjoint from every known escaping type. EXC006 already
  covers a body that cannot throw a tracked exception. This would cover a
  `catch (FileNotFoundException)` around a body whose only known escape is
  a different type.

Wrapping that drops `InnerException` changes which type escapes, and the
existing throw checks already see the new type. The lost inner exception
is the CA2200 and destructive-remapping issue above.

XML `<exception>` documentation is a weak contract source. It often lists
exceptions from unspecified callees. A later code fix can copy a
documentation entry into `[Throws]`. A mismatch warning would be noisy.

Using exceptions as ordinary control flow is real guidance and a poor
analyzer rule. A local `try`/`catch` of a declared exception is normal
handling, and the interprocedural pattern is out of scope.

### Priority if one of these is built

1. Stop treating a filter exception as an escaping contract, and report
   that the filter discarded it.
2. Report a known escaping exception in `finally`, including a `[Throws]`
   call and `Dispose` from `using` or `foreach`.
3. On a source static constructor or finalizer, report a known escaping
   throw or call. Leave the inner exception off the normal `[Throws]`
   metadata.
4. On source `Dispose` and `Dispose(bool)`, report known escapes. Publish
   `[DoesNotThrow]` only when the body meets the EXC006 bar. Leave
   external `Dispose` unspecified.
5. Apply that same diagnostic to `Equals`, `GetHashCode`, `ToString`,
   equality operators, and implicit conversions.
6. Add a closed-body dead `catch`, then an empty catch of a known
   contract, only after the noise is acceptable.

Instance constructors stay on throw inference. `[DoesNotThrow]` on the
constructor or the type remains the explicit no-throw constructor contract.

## How this research applies

Exceptionable already uses the split this guide recommends: the public
attributes are a normal library, and the analyzer is a separate Roslyn
package. [DESIGN.md](./DESIGN.md) is the behavior contract. Use the links
above when changing packaging, configuration, or analyzer lifecycle code.

- Keep descriptor and analyzer lifecycle patterns, and keep contract
  precedence in `ContractResolver`.
- Resolve annotation identity and exception types semantically. Inherited
  attributes, metadata names, and ambiguous string type names need the rules
  in DESIGN.md, not a short-name text match.
- Prove each supported exception-flow case with analyzer tests. Do not copy
  broad analyzer abstractions from large repositories before a concrete need
  appears.
- Use [exception-handling smells](#exception-handling-smells) before adding a
  no-throw default or a new catch diagnostic. Instance constructors stay on
  throw inference. The open items are in DESIGN.md.
- Recheck external sources when selecting package versions or publishing.
  The repository links here are pinned snapshots.
- GitVersion 6 renames the older deployment modes. `ContinuousDelivery` is the
  mode that promotes commit distance into the prerelease number.
  `ManualDeployment` leaves that distance in `+` build metadata, which NuGet
  does not keep as part of the package identity. See
  [Version incrementing](https://gitversion.net/docs/reference/version-increments)
  and the [deployment mode](https://gitversion.net/api/GitVersion.VersionCalculation/DeploymentMode/)
  names. The rule catalog in `AnalyzerReleases.Shipped.md` tracks diagnostic
  IDs. It is not the NuGet package version.
