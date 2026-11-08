# Exceptionable

Exceptionable consists of two packages:

- `Exceptionable` provides `[Throws("System.IO.IOException")]` and
  `[DoesNotThrow]` attributes for consumer APIs.
- `Exceptionable.Analyzers` checks explicitly declared synchronous exception
  contracts.

As checked on 2026-11-07, the only version listed on NuGet.org for both
[Exceptionable](https://www.nuget.org/packages/Exceptionable/) and
[Exceptionable.Analyzers](https://www.nuget.org/packages/Exceptionable.Analyzers/)
is the prerelease `0.0.1-feat-initial-exceptionable.1`. Pin that version when
installing the published packages:

```xml
<ItemGroup>
  <PackageReference Include="Exceptionable" Version="0.0.1-feat-initial-exceptionable.1" />
  <PackageReference Include="Exceptionable.Analyzers"
                    Version="0.0.1-feat-initial-exceptionable.1"
                    PrivateAssets="all" />
</ItemGroup>
```

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

Rule severity uses standard `dotnet_diagnostic.EXCxxx.severity` settings and
does not enable analysis by itself.

The analyzer package targets .NET Standard 2.0 for compatibility with Roslyn
hosts. The attributes library supports .NET 10 and .NET Framework 4.8.1
consumers. The analyzer checks interface contracts at interface-typed call
sites and validates source implementations against all mapped interface
contracts. It trusts external implementations and does not validate a method
inherited from a base class that does not itself implement the interface.
General class virtual/override dispatch remains unsupported.

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
against those local packages, checking that the packaged analyzer reports its
expected EXC002 and EXC003 diagnostics. GitVersion derives local package
versions from Git history; those build outputs are not evidence of a public
release. Published stable and prerelease GitHub Releases are published to
NuGet.org. Use GitVersion-compatible SemVer tags, such as `v1.0.0` or
`v0.1.0-alpha1`, to produce stable and prerelease package versions.
