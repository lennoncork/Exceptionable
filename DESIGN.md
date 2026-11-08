# Exceptionable — Exception Contract Analyzer Design

## 1. Vision

Exceptionable is a Roslyn analyzer that lets libraries declare which
exceptions their APIs may throw and lets callers verify that those exceptions
are handled or propagated. The design borrows the declarative,
compiler-visible approach of .NET nullable analysis: annotations describe
contracts, and analysis checks whether code honors them. Nullable analysis is
the primary reference for project-level and editor configuration.

The analyzer is static analysis, not runtime exception interception. It does
not change program behavior or guarantee that every possible exception is
known. Its first goal is a useful, predictable contract for explicitly
declared exceptions and common synchronous control flow.

The project name is **Exceptionable**. The initial distribution consists of
two separate NuGet packages:

- **Exceptionable** provides the public attributes as a normal
  compile-time library for projects that annotate APIs.
- **Exceptionable.Analyzers** provides the Roslyn analyzer and its analyzer
  build assets. It is installed as an analyzer, not as a runtime dependency.

The package boundary must keep the attributes usable without the analyzer and
the analyzer deployable independently. The analyzer recognizes the public
attributes from Exceptionable as well as equivalent
source-defined attributes, so adoption can be incremental.

## 2. Mandatory requirements

The requirements in this section are binding for all implementation,
packaging, and documentation work. They are not suggestions or open design
options. If another section conflicts with a requirement here, this section
takes precedence.

1. **Project identity:** The project is named **Exceptionable**.
2. **Separate NuGet packages:** Produce two distinct packages:
   - **Exceptionable** contains the public attributes used in
     consumer source code.
   - **Exceptionable.Analyzers** contains the analyzer and its analyzer
     deployment assets.
   The attributes package must be independently usable without the analyzer;
   the analyzer package must be separately installable as an analyzer.
3. **Analysis controls:** Provide a project-level `<Exceptionable>` XML/MSBuild
   property and `.editorconfig` settings to turn analysis on or off. Support
   `enable` and `disable` values. A scoped `.editorconfig` setting overrides
   the project-level setting. If neither setting is present, analysis is
   disabled. Per-diagnostic severity settings configure or suppress
   diagnostics only after analysis is enabled; they must not enable analysis
   by themselves.
4. **Nullable as the primary reference:** Follow .NET nullable analysis as
   the primary precedent for analysis enablement, configuration behavior,
   and IDE/build integration, while honoring the explicit Exceptionable
   configuration names and precedence above.
5. **Target support:** Support C# consumer projects targeting .NET 10 and
    .NET Framework 4.8.1 (`net481`). The attributes library targets both
    frameworks; the analyzer targets .NET Standard 2.0 so it can load in
    compatible Roslyn hosts.
6. **Build runner:** Use Cake as the build runner. Build, test, and package
    workflows must be orchestrated through Cake.
7. **Release versioning:** Derive package versions from Git history using
  GitVersion. Publish packages for published GitHub Releases, including
  prereleases, using GitVersion's stable or prerelease package version.

## 3. Goals

- Provide source-level attributes for declaring exception behavior on methods
  and types.
- Publish the attributes and analyzer in separate NuGet packages.
- Report when a declared exception is neither caught nor allowed to escape a
  method that declares it.
- Report when an implementation throws an exception outside its declared
  contract.
- Check calls to annotated APIs in the caller's context.
- Work as a normal Roslyn analyzer in the IDE and during builds, with
  configurable diagnostic severity.
- Let projects and editor configuration enable or disable Exceptionable
  analysis, following the configuration model of .NET nullable analysis.
- Define conservative behavior for code the analyzer cannot prove.

## 4. Non-goals for the initial release

- Runtime checks, exception wrapping, or generated try/catch blocks.
- Inferring a complete exception contract for arbitrary code, including
  framework and third-party APIs.
- Proving behavior across reflection, native calls, dynamic dispatch, or
  arbitrary control flow.
- Modeling asynchronous, iterator, or interprocedural behavior beyond the
  explicitly listed later phases.
- Replacing C#'s existing exception semantics or requiring every method to
  declare every exception it might indirectly encounter.

## 5. Attribute contract

The attributes are metadata consumed by the analyzer; they have no runtime
effect. They should be public, sealed, and documented in the
**Exceptionable** package, which is referenced by consumer code
that applies them. The **Exceptionable.Analyzers** package contains the
analyzer deployment assets, not a second copy of the public attribute API.
The analyzer must also recognize source-defined attributes so projects can
adopt the analyzer without requiring the attributes package.

### `[DoesNotThrow]`

Apply to a method or class to state that the annotated member does not allow an
exception to escape. On a class, the contract applies to its eligible instance
and static methods unless a member has a more specific contract.

This is a strong promise: an implementation must not let an exception escape.
It does not mean that no exception can occur inside the method; a method may
throw and handle an exception internally.

### `[Throws("Exception")]`

Apply to a method or class to declare an exception type that may escape. The
string is a type name, not an exception message. To keep the requested concise
form usable while avoiding silent guessing, resolve names as follows:

1. A fully qualified metadata name (for example,
   `"System.IO.IOException"`) resolves exactly.
2. A short name (for example, `"Exception"`) resolves only when it identifies
   one exception type in the compilation. The built-in
   `"Exception"` shorthand resolves to `System.Exception`.
3. An unresolved or ambiguous name is an attribute-usage diagnostic; it must
   not silently disable analysis.

The declared type covers derived exception types. Multiple `[Throws(...)]`
attributes may be applied to declare multiple exception families.

The design deliberately keeps the requested string syntax in the first
contract version. It is less robust than a `Type`-based attribute argument;
an implementation should isolate name resolution so a future version can
consider a strongly typed form without changing analysis semantics.

### Attribute usage and precedence

- Both attributes are valid on methods and classes and are inherited where
  C# attribute inheritance applies.
- Accessor declarations may carry method-targeted attributes; analyzer call
  checks include property get/set and event add/remove operations.
- The analyzer recognizes the canonical `Exceptionable.DoesNotThrowAttribute`
  and `Exceptionable.ThrowsAttribute` identities, whether supplied by the
  package or defined in consumer source. A same-named attribute in another
  namespace is unrelated.
- A method-level `[Throws]` list overrides the class-level exception list for
  that method.
- A method-level `[DoesNotThrow]` overrides a class-level `[Throws]` list.
- A method-level `[Throws]` overrides class-level `[DoesNotThrow]`.
- Applying both attributes at the same target is invalid and reported.
- Constructors and property/event accessors are included where the attribute
  target and Roslyn symbol model permit them. Operators and local functions
  are out of scope initially.

Precedence must be implemented centrally and covered by tests, rather than
being independently reinterpreted by each diagnostic.

## 6. Meaning of the contract

Treat a method's contract as one of:

- **No-throw:** no exception may escape this method.
- **Declared set:** one or more exception types may escape; subtypes are
  included.
- **Unspecified:** legacy/default behavior; no new contract diagnostic is
  produced for that method.

For a call with a known contract, an exception is handled if a reachable
`catch` clause catches its type or a base type. A declared exception can also
be propagated if the enclosing method's effective contract permits that
exception type. An exception is unhandled when neither condition holds.

For no-throw methods, all known escaping exceptions are violations; catches
that consume them are permitted. An explicit `throw` that cannot be shown to
be caught is a violation. Returning normally is not required: non-exceptional
termination such as an infinite loop is outside the contract.

These meanings apply to synchronous, non-iterator methods. `[Throws]` on an
`async` method does not currently describe exceptions observed when awaiting
its returned task, and a contract on an iterator does not describe exceptions
observed during enumeration. The analyzer skips those method bodies and their
known call sites rather than treating deferred exceptions as immediate ones.
It also skips calls whose return type is `Task`, `ValueTask`, `IEnumerable`,
`IEnumerator`, `IAsyncEnumerable`, or `IAsyncEnumerator`, since the analyzer
does not yet distinguish eager from deferred implementations of these APIs.
Such annotations do not provide caller diagnostics until deferred-execution
semantics are designed.

The analyzer must not claim soundness for exceptions without a known contract.
In particular, an unannotated call is not assumed to be non-throwing or
`System.Exception`-throwing. This avoids flooding existing code with
unactionable diagnostics and makes annotations the opt-in boundary.

## 7. Initial analysis scope

Analyze synchronous method bodies, constructors, and accessors with semantic
information from Roslyn. The initial implementation should recognize:

- Explicit `throw` and `throw;` statements.
- Calls, object creation, and property getter/setter accesses whose target
  has an effective `[Throws]` contract, plus event add/remove accessors.
- Catches with exact, base-type, or catch-all (`catch` without a type)
  coverage.
- Propagation permitted by the enclosing method's effective declared set.
- `finally` blocks conservatively: an exception thrown from a `finally` block
  can replace a pending exception and is not caught by the same `try`.
- Contracts on interface methods at calls statically bound through an
  interface-typed value. Source implementation methods are checked against
  every implemented interface contract, including explicit implementations,
  inherited interface members, and multiple interfaces mapped to one method.

Use Roslyn's operation, control-flow, and symbol APIs rather than text
matching. Analyze operations only in reachable control-flow blocks. A catch
proves handling only when the operation is in the corresponding `try` body and
the catch type covers the exception. A non-constant exception filter does not
prove handling. When type or flow information is unknown, prefer not to report
a definite violation; report an unsupported/unknown case only if a later
diagnostic is introduced specifically for it.

An interface call uses the contract declared by the statically selected
interface member; calls through different interfaces use their respective
contracts. An implementation must satisfy all known interface contracts it
implements, so conflicting declarations constrain the implementation to the
intersection of their permitted exception sets. Such declarations are not
themselves conflicts when placed on separate interfaces. Explicit and implicit
implementations are mapped with Roslyn symbols, and inherited interface
members retain the contract on the member's declaring interface. An interface
contract is trusted for external implementations; a source method inherited
from a base class that does not itself implement the interface is not
validated against a derived class's interface contract. General class
virtual/override dispatch remains unsupported because the selected
implementation and its contract are not known at the call site.

For `throw expression`, report the expression's static exception type before
the implicit conversion to `System.Exception` required by C#. For `throw;`,
use the enclosing catch type; a bare `catch` is treated as
`System.Exception`. Catch-all clauses handle known exceptions when they
enclose the operation in the `try` body, but do not handle a rethrow from
their own handler.

### Deferred analysis cases

Keep the first release focused and document its limits. Add explicit support
in later phases for:

- `async` methods, including exceptions observed when awaiting a task, and
  task-returning calls. An eventual model must distinguish invocation from
  `await`, handle catches around `await`, and propagate only exceptions
  observed by the caller.
- Iterators, where exceptions can arise during `MoveNext` or asynchronous
  enumeration rather than when the iterator method is called. A model must
  account for enumeration catches and deferred caller contracts.
- Lambdas and delegates; general class virtual/override dispatch remains
  unsupported because the runtime target may differ from the statically
  selected method.
- Non-constant exception filters and complex rethrow/data-flow cases.
- Generic exception types or exception type parameters, if introduced.
- External APIs with contracts distributed through reference assemblies.

Until a case is supported, it must not produce misleading certainty. Async
and iterator bodies and their recognized deferred call sites are skipped;
custom awaitables, custom enumerable abstractions, and external iterator
implementations that cannot be identified from source remain outside the
current guarantees. Document these limitations in help text and tests.

## 8. Diagnostics

Use a stable diagnostic ID prefix (for example, `EXC`) and include IDs,
titles, messages, categories, default severities, and help links in the
implementation. The exact prefix should be confirmed before publishing a
package.

| Proposed ID | Condition | Initial default |
|---|---|---|
| EXC001 | A known exception escapes a `[DoesNotThrow]` method or type. | Warning |
| EXC002 | A known exception escapes a `[Throws]` method but is not in its declared set. | Warning |
| EXC003 | A call to an annotated method may throw an exception that is neither caught nor permitted by the caller's contract. | Warning |
| EXC004 | `[Throws]` names no exception type, or its name is ambiguous. | Error |
| EXC005 | `[DoesNotThrow]` and `[Throws]` conflict on the same target. | Error |

Diagnostics should identify the relevant throw or call site. Where useful,
include the exception type and the contract that was violated. Avoid
diagnostics for exception sources with no known contract in the initial
release.

## 9. Analysis configuration

Analysis is controlled independently of the attributes: a project may retain
exception annotations while temporarily disabling diagnostics. Follow
nullable analysis as the primary model, using a project-level MSBuild property
and per-directory/file `.editorconfig` configuration.

The project-level switch is the `<Exceptionable>` property:

```xml
<PropertyGroup>
  <Exceptionable>enable</Exceptionable>
</PropertyGroup>
```

`enable` turns Exceptionable analysis on and `disable` turns it off. If the
property is omitted, analysis is disabled, following the opt-in default of
nullable analysis. The analyzer package must expose this property to the
analyzer through compiler-visible build properties so the same setting applies
in IDE and command-line builds.

An `.editorconfig` can override the project switch for files under its scope:

```editorconfig
[*]
exceptionable_analysis = enable

[Legacy/**/*.cs]
exceptionable_analysis = disable
```

The supported values are `enable` and `disable`. A scoped
`exceptionable_analysis` value takes precedence over `<Exceptionable>`; when
neither is set, analysis is disabled. When analysis is enabled,
`dotnet_diagnostic.EXCxxx.severity` remains the standard per-diagnostic
severity and suppression mechanism:

```editorconfig
dotnet_diagnostic.EXC001.severity = warning
dotnet_diagnostic.EXC003.severity = none
```

Disabling Exceptionable analysis suppresses all Exceptionable diagnostics for
the affected compilation or file, regardless of per-rule severity settings.
Per-rule settings do not turn analysis on when the master switch is disabled.

## 10. Suggested architecture

Keep the public attributes independent of analyzer implementation details.
Organize the analyzer package into these logical parts:

1. **Attribute declarations** — `DoesNotThrowAttribute` and repeatable
   `ThrowsAttribute` in Exceptionable, with the documented targets
   and inheritance behavior.
2. **Contract reader** — finds source or metadata attributes, resolves
   exception names to `INamedTypeSymbol`, applies inheritance and precedence,
   and returns a single normalized contract representation.
3. **Exception-flow analysis** — identifies known exception sources,
   propagates them through supported control flow, and determines whether
   catches or the enclosing contract handle them.
4. **Diagnostics** — declares rules and reports locations and arguments;
   analysis enablement and per-rule severity follow standard Roslyn
   configuration.
5. **Tests** — analyzer tests for valid and invalid code, attribute lookup,
   inheritance, and documented limitations.

The implementation keeps `ExceptionableAnalyzer` as the Roslyn registration
entry point. A compilation-start action creates an `ExceptionableCompilationContext`
that owns the compilation, `AnalysisOptions`, `ExceptionTypeResolver`,
`ContractResolver`, and `ReachabilityAnalysis`. These objects and any symbols
they reference are scoped to that compilation; none are stored in static
state.

The `Contracts/` area contains the normalized `ExceptionContract`,
`ContractResolver`, `ExceptionTypeResolver`, and attribute/symbol helpers.
`Analysis/` contains separate declaration, throw, and call handlers, plus
exception-flow, operation-target, method-policy, and reachability components.
`Diagnostics/` owns the descriptors. Keep rule handlers focused on one
diagnostic path and share semantic helpers rather than duplicating contract
resolution or operation traversal.

Test contract resolution, exception-name resolution, configuration, exception
flow, target-method selection, method policy, and reachability in focused unit
test classes. Retain analyzer-level tests for combined Roslyn behavior,
diagnostic IDs, arguments, and source locations. Use compilation-start actions
to resolve framework symbols once per compilation, and register only the
operation/symbol actions needed for supported constructs. Package the analyzer
in the conventional NuGet analyzer assets location and verify both packages
in a consumer project.

## 11. Implementation roadmap

### Phase 0 — Establish the analyzer project

- Create separate Exceptionable and Exceptionable.Analyzers
  projects and NuGet packages.
- Configure and verify support for C# projects targeting .NET 10 and net481,
  and establish Cake as the build runner for build, test, and package workflows.
- Implement and test `<Exceptionable>` and `.editorconfig` enablement, including
  file-scoped overrides and per-rule severity.
- Select and document supported .NET, C#, Roslyn, and IDE versions.
- Add analyzer test infrastructure and a minimal test project.
- Verify attribute references, analyzer loading, and configuration in a
  sample consumer project.

### Phase 1 — Attributes and contract reading

- Implement both attributes and their usage metadata.
- Resolve `[Throws]` names according to the rules above.
- Implement contract precedence and inheritance.
- Add EXC004 and EXC005 tests, including malformed and ambiguous names.

### Phase 2 — Direct exception declarations

- Analyze explicit throw statements and known throws from annotated calls.
- Implement catch matching, declared-set matching, and method-level
  diagnostics (EXC001 and EXC002).
- Add tests for exact types, base classes, derived exceptions, catch-all,
  rethrow, and unreachable code.

### Phase 3 — Caller validation

- Check calls to annotated methods against caller catches and declared
  contracts (EXC003).
- Cover constructors, accessors, class-level contracts, and precedence.
- Honor contracts at interface-typed call sites and validate source interface
  implementations against all mapped interface contracts.
- Test that unannotated calls do not produce speculative diagnostics.

### Phase 4 — Quality and usability

- Add diagnostic documentation, configuration examples, and release guidance.
- Evaluate code fixes only where a safe fix is clear (for example, adding a
  compatible `[Throws]` annotation); never generate a catch that silently
  suppresses an exception.
- Measure analyzer performance on representative projects and avoid repeated
  symbol resolution or whole-compilation rescans.

### Phase 5 — Advanced exception behavior

- Design and test async/await and iterator semantics before enabling rules
  for those constructs.
- Consider override compatibility and metadata-based contracts.
- Revisit the string-based API and consider a strongly typed alternative
  while preserving existing annotations.

## 12. Testing strategy and acceptance criteria

Use Roslyn analyzer test helpers with compilable source snippets and verify
both diagnostics and their source locations. Include positive and negative
tests for each rule.

Minimum acceptance criteria for the initial usable release:

- An annotated throw that is caught does not produce an escaping-exception
  diagnostic.
- An annotated exception that is neither caught nor declared is diagnosed at
  the call or throw site.
- A declared exception, including its derived types, may propagate through a
  caller with a compatible contract.
- A `[DoesNotThrow]` implementation that lets a known exception escape is
  diagnosed, while one that catches it is accepted.
- Conflicting or unresolved annotations produce clear attribute diagnostics.
- The two NuGet packages can be built and consumed independently for their
  documented purposes.
- `<Exceptionable>enable</Exceptionable>` enables analysis and
  `<Exceptionable>disable</Exceptionable>` disables it.
- `.editorconfig` can enable or disable analysis for its scope and overrides
  the project-level switch.
- Per-rule severity settings can suppress or configure diagnostics when
  analysis is enabled, but do not enable analysis by themselves.
- Unannotated call sites remain unaffected.
- The analyzer builds, loads in a consumer project, and runs without
  diagnostics caused by the analyzer itself.
- Tests explicitly capture unsupported cases so future phases can expand
  coverage without changing undocumented assumptions.

## 13. Implementation decisions and open questions

The initial implementation confirms these decisions:

- The attributes target .NET 10 and .NET Framework 4.8.1; the analyzer targets
  .NET Standard 2.0 and uses Roslyn 4.14.
- Diagnostics use the `EXC` prefix. EXC001-EXC003 are enabled warnings and
  EXC004-EXC005 are enabled errors.
- Class-level contracts apply to methods, constructors, and accessors.
- Interface contracts apply at interface-typed call sites. Source
  implementations are checked against each mapped interface contract;
  external implementations and methods inherited from a non-implementing
  base class are trusted rather than proven. Class virtual/override
  dispatch remains unsupported.
- Ambiguous short exception names are errors; attribute strings do not use
  source-file `using` directives.
- GitVersion derives package versions from Git history. Stable and prerelease
  GitHub Releases both publish their GitVersion-generated package versions.

Async/await and iterator contracts remain open. `[Throws]` currently describes
only synchronous exceptions; it does not describe faults observed by awaiting
tasks or enumerating iterators. Task-like and standard enumerable-returning
calls are skipped to avoid reporting deferred exceptions as immediate ones.
Class override compatibility also remains open, so those call sites stay
unsupported.
