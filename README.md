# Exceptionable

[![Build](https://github.com/lennoncork/Exceptionable/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/lennoncork/Exceptionable/actions/workflows/build.yml)
[![Exceptionable on NuGet](https://img.shields.io/nuget/v/Exceptionable)](https://www.nuget.org/packages/Exceptionable)
[![Exceptionable downloads](https://img.shields.io/nuget/dt/Exceptionable)](https://www.nuget.org/packages/Exceptionable)
[![MIT license](https://img.shields.io/github/license/lennoncork/Exceptionable)](./LICENSE)

Declare synchronous exception contracts in C# and have them checked during
builds and in the IDE.

Exceptionable consists of two NuGet packages:

- **`Exceptionable`** provides `[Throws<System.IO.IOException>]`,
  `[Throws("System.IO.IOException")]`, and `[DoesNotThrow]` attributes for
  declaring contracts.
- **`Exceptionable.Analyzers`** validates those contracts and reports
  diagnostics for explicit throws, annotated calls, and inferred contracts.

## Installation

Install matching versions of both packages. Use the version published for the
GitHub Release you want; package versions are derived from release tags by
GitVersion and do not include SemVer build metadata.

```xml
<ItemGroup>
  <PackageReference Include="Exceptionable" Version="0.1.0" />
  <PackageReference Include="Exceptionable.Analyzers"
                    Version="0.1.0"
                    PrivateAssets="all" />
</ItemGroup>
```

Replace `0.1.0` with the version from the GitHub Release. Commits that are not
on a release tag receive a changing prerelease number, so later commits do not
reuse an earlier package identity. `AnalyzerReleases.Shipped.md` tracks
diagnostic IDs; it is not the package version.

Reference `Exceptionable` wherever contracts are declared, and add
`Exceptionable.Analyzers` to projects where analysis should run. Keeping the
analyzer reference private prevents it from becoming a dependency of a
library's consumers. The attributes package does not depend on the analyzer.
The analyzer is deployed as a Roslyn analyzer and runs in command-line and IDE
builds.

## Configuration

Analysis is disabled by default. Enable it for a project with:

```xml
<PropertyGroup>
  <Exceptionable>enable</Exceptionable>
</PropertyGroup>
```

An `.editorconfig` can override the project setting for individual files:

```editorconfig
[*]
exceptionable_analysis = enable

[Legacy/**/*.cs]
exceptionable_analysis = disable
```

C# has no `#exceptionable` directive; that spelling is a compiler error.
Per-file control uses `exceptionable_analysis`.

Rule severity uses standard `dotnet_diagnostic.EXCxxx.severity` settings.
Severity settings do not enable analysis.

Framework exception polyfills (for example `File.ReadAllText` and LINQ
`First`) are on by default once analysis is enabled. Turn them off with
`<ExceptionablePolyfills>disable</ExceptionablePolyfills>`,
`exceptionable_polyfill = disable`, or per namespace such as
`exceptionable_polyfill.System.IO = disable`. See the
[polyfill documentation](./docs/README.md#framework-polyfills).

## Compatibility

The analyzer package targets .NET Standard 2.0 and is built against Roslyn
4.14. It loads in Visual Studio 2022 17.14 or later and in .NET SDK builds
that include that compiler or a newer one. The attributes library supports
.NET 10 and .NET Framework 4.8.1 consumers. `[Throws<TException>]` is a
generic attribute and requires .NET 7 or later; .NET Framework 4.8.1 consumers
should use the string form.

## Analyzer behavior

The analyzer checks explicit throws against declared contracts and annotated
calls against the caller's contract. A reachable uncaught throw is inferred
as a `[Throws]` contract for that member. An explicit `[Throws]` or
`[DoesNotThrow]` replaces inference. The source generator writes inferred
contracts into the compiled assembly, allowing a consuming project that
references that assembly and enables analysis to report EXC003 without the
library's source.

`[Throws<TException>]` requires `TException` to derive from
`System.Exception`. The string form accepts a metadata name, a dotted nested
name, or an unambiguous short name. The analyzer rejects a name or type
argument that does not identify a type derived from `System.Exception`.

`[Throws]` and `[DoesNotThrow]` may be applied to properties, indexers,
events, local functions, and delegate parameters. An accessor attribute
overrides the member contract. On a delegate parameter, the attribute is the
contract of the callback: a lambda, local function, or method group that
throws outside that contract is EXC010, and invoking the parameter uses the
same contract. A contract on a parameter that is not a delegate is EXC011.
The analyzer also checks user-defined operators and conversions, field and
property initializers, synchronous `foreach` and `using` pattern methods, and
deconstruction. `throw null` is reported as `NullReferenceException`.
`nameof` is not a call. A throw inside a local function or lambda is not a
throw of the enclosing method. A non-async local function is checked as its
own method, and invoking a lambda stored in one local reports that lambda's
known exceptions. A method with a body is checked at the call even when it
returns `Task` or `IEnumerable`, unless it is async or an iterator. Async
methods, iterators, and body-less deferred signatures stay deferred. An open
virtual call is checked only for the explicit `[Throws]` set on the selected
method. Diagnostic messages use namespace-qualified exception names. Explicit
throws are EXC001 or EXC002. Annotated calls, including a call inside
`[DoesNotThrow]`, are EXC003. When a body later throws a different known
exception, EXC002 reports the new throw and EXC008 reports the declared type
that no longer escapes. When nullable annotations make a constructor's
`ArgumentNullException` guard potentially redundant, EXC007 reports that
advisory and the throw is not inferred. Nullable annotations are not runtime
validation; keep the guard when callers outside nullable analysis need runtime
protection.

Interface contracts are checked at interface-typed call sites, and source
implementations are validated against all mapped interface contracts. A
source implementation must declare the same `[Throws]` or `[DoesNotThrow]`
contract as the interface member; a mismatch is EXC009, analogous to
nullable's interface-implementation mismatch warnings. The
analyzer trusts external implementations and does not validate a method
inherited from a base class that does not itself implement the interface.
An open virtual or unsealed override is checked only for the explicit
`[Throws]` set on the selected method. `base` access is checked as the
selected member. Attributes apply to classes and structs, not to an interface
type; annotate each interface member. A derived interface member that
redeclares a base member and has no contract of its own keeps the base
contract.

For diagnostic IDs and details, see the
[diagnostics reference](./docs/README.md).

## Limitations

`[Throws]` on an async method or iterator does not describe exceptions
observed when awaiting a task or enumerating a sequence. Those bodies are
skipped, including a throw before the first `yield`. A method with a body
that is not async and not an iterator is checked at the call, even when it
returns `Task` or `IEnumerable`. A body-less deferred signature stays
deferred. Non-constant exception filters do not prove handling. Polyfills do
not yet cover awaited tasks or deferred LINQ operators.

See [DESIGN.md](./DESIGN.md) for detailed semantics and boundaries.

## Build and validate

Run the repository's full validation target with Cake:

```powershell
dotnet cake build.cake --target=Validate
```

Validation builds, formats, and tests the solution, then packs both libraries
and builds the consumer sample for `net10.0` and `net481` against those local
packages. It verifies the expected EXC002 and EXC003 diagnostics from the
packaged analyzer. The `net481` build also restores the .NET Framework
reference assemblies.

GitVersion derives local package versions from Git history; local build
outputs are not evidence of a public release. Stable and prerelease GitHub
Releases are published to NuGet.org. Use GitVersion-compatible SemVer tags,
such as `v1.0.0` or `v0.1.0-alpha1`, to produce stable and prerelease package
versions.
