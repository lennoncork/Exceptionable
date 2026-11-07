# Exceptionable

Exceptionable consists of two packages:

- `Exceptionable` provides `[Throws("System.IO.IOException")]` and
  `[DoesNotThrow]` attributes for consumer APIs.
- `Exceptionable.Analyzers` checks explicitly declared synchronous exception
  contracts.

Install the attributes package wherever contracts are declared, and install
the analyzer package in projects where analysis should run. Analysis is
disabled by default. Enable it with:

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
consumers. Async methods, iterators, exception filters that are not constant
`true`, and virtual/interface dispatch are not analyzed yet; see
[`DESIGN.md`](./DESIGN.md) for the current semantics and limitations.

Build and validate through Cake:

```powershell
dotnet cake build.cake --target=Validate
```

The validation target also packs both libraries and builds the consumer sample
against those local packages. GitVersion derives package versions from Git
history. Publishing is triggered by a published stable GitHub Release whose
`vMAJOR.MINOR.PATCH` tag matches both package versions.
