# Exceptionable

Exceptionable consists of two packages:

- `Exceptionable` provides `[Throws<System.IO.IOException>]`,
  `[Throws("System.IO.IOException")]`, and `[DoesNotThrow]` attributes for
  consumer APIs.
- `Exceptionable.Analyzers` checks explicitly declared synchronous exception
  contracts.

Install the version published for the GitHub Release you want. GitVersion
assigns that version from the release tag, and package versions do not include
SemVer build metadata. Commits that are not on a release tag get a prerelease
number that changes with each commit, so later commits do not reuse an earlier
package identity.

```xml
<ItemGroup>
  <PackageReference Include="Exceptionable" Version="0.1.0" />
  <PackageReference Include="Exceptionable.Analyzers"
                    Version="0.1.0"
                    PrivateAssets="all" />
</ItemGroup>
```

Replace `0.1.0` with the version from the GitHub Release.
`AnalyzerReleases.Shipped.md` tracks diagnostic IDs and is not that package
version.

Reference `Exceptionable` wherever contracts are declared, and add
`Exceptionable.Analyzers` to projects where analysis should run. Keeping the
analyzer reference private prevents it from becoming a dependency of a
library's consumers; the attributes package does not depend on the analyzer.
The analyzer package is deployed as a Roslyn analyzer, so it runs in normal
command-line builds and IDE builds. Analysis is disabled by default. Enable it
with:

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

C# has no `#exceptionable` directive. That spelling is a compiler error.
Per-file control is `exceptionable_analysis`.

Rule severity uses standard `dotnet_diagnostic.EXCxxx.severity` settings and
does not enable analysis by itself.

The analyzer package targets .NET Standard 2.0 and is built against Roslyn
4.14. It loads in Visual Studio 2022 17.14 or later and in .NET SDK builds
that include that compiler or a newer one. The attributes library supports
.NET 10 and .NET Framework 4.8.1 consumers. `[Throws<TException>]` is a
generic attribute, so applying it requires .NET 7 or later. A .NET Framework
4.8.1 consumer uses the string form.

The analyzer checks interface contracts at interface-typed call
sites and validates source implementations against all mapped interface
contracts. It trusts external implementations and does not validate a method
inherited from a base class that does not itself implement the interface.
General class virtual/override dispatch remains unsupported. `base` access is
checked as the selected member. Attributes apply to classes and structs, not
to an interface type; annotate each interface member. A derived interface
member that redeclares a base member and has no contract of its own keeps
the base contract.

`[Throws<TException>]` requires `TException` to derive from `System.Exception`.
The string form accepts a metadata name, a dotted nested name, or an
unambiguous short name. The analyzer rejects a name or type argument that does
not identify a type derived from `System.Exception`.

`[Throws]` and `[DoesNotThrow]` may be applied to properties, indexers, and
events. An accessor attribute overrides that member contract. The analyzer
also checks user-defined operators and conversions, field and property
initializers, synchronous `foreach` and `using` pattern methods, and
deconstruction. `throw null` is reported as `NullReferenceException`.
`nameof`, local functions, and lambdas are not treated as throws of the
enclosing method. Diagnostic messages use namespace-qualified exception
names. Explicit throws are EXC001 or EXC002. Annotated calls, including a
call inside `[DoesNotThrow]`, are EXC003. A constructor that lets
`ArgumentNullException` escape is treated as
`[Throws(typeof(ArgumentNullException))]`. When nullable annotations make that
throw redundant, EXC007 warns to remove it.

Rule reference: [`docs/README.md`](./docs/README.md).

`[Throws]` currently covers synchronous exceptions only; it does not describe
exceptions observed when awaiting a task or enumerating an iterator. Async and
iterator bodies and recognized deferred call sites (including `Task`,
`ValueTask`, and standard enumerable return types) are skipped.
Non-constant exception filters do not prove handling. See
[`DESIGN.md`](./DESIGN.md) for the detailed semantics and boundaries.

Build and validate through Cake:

```powershell
dotnet cake build.cake --target=Validate
```

The validation target also packs both libraries and builds the consumer sample
for `net10.0` and `net481` against those local packages, checking that the
packaged analyzer reports its expected EXC002 and EXC003 diagnostics. The
`net481` build also restores the .NET Framework reference assemblies. GitVersion derives local package
versions from Git history; those build outputs are not evidence of a public
release. Published stable and prerelease GitHub Releases are published to
NuGet.org. Use GitVersion-compatible SemVer tags, such as `v1.0.0` or
`v0.1.0-alpha1`, to produce stable and prerelease package versions.
