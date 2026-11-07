---
name: build
description: Canonical build instructions for the repository.
scope: repository
version: 1.0
---

# Build Skill

## Description

This skill contains the canonical build instructions for the repository. Agents should follow these steps when validating or modifying code.

## Prerequisites
- Install a compatible .NET SDK (see `dotnet --info`).
- Restore local dotnet tools with `dotnet tool restore`.

## Commands
From the repository root (the directory containing `build.cake`):

```bash
dotnet tool restore
dotnet restore src/Exceptionable.slnx --source https://api.nuget.org/v3/index.json
dotnet cake --target=Build
dotnet cake --target=Analyzers
```

For a stable NuGet release, publish a GitHub Release with a `vMAJOR.MINOR.PATCH`
tag. GitVersion calculates the package versions, and the release workflow
verifies that both packages match the tag before pushing them to NuGet.org.

## Guidance
- Prefer the `dotnet cake` `Build` target to mirror CI.
- Run the `Analyzers` target to verify third-party analyzer diagnostics through `info` severity without applying fixes.
- Use explicit Cake targets such as `Build` and `Push` rather than calling `dotnet build` directly, except for one-off debugging.
- If package restore fails against internal feeds, add `https://api.nuget.org/v3/index.json` as a fallback source.
