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

For a NuGet release, publish a GitHub Release with a GitVersion-compatible
SemVer tag such as `v1.0.0` or `v0.1.0-alpha1`. GitVersion calculates package
versions; the release workflow builds and validates the packages before
pushing them to NuGet.org. GitHub prereleases are published as NuGet prerelease
versions. Non-release commits include the commit distance in the prerelease
number. Package versions omit `+` build metadata so NuGet keeps a distinct
identity for each of those commits.

## Guidance
- CI restores this local Cake tool with `dotnet tool restore`, then runs `dotnet cake build.cake --target=Validate`. Keep that invocation; resolving Cake as `latest` from the GitHub releases API is rate-limited on hosted runners.
- Prefer the `dotnet cake` `Build` target for a local compile that matches the CI build step.
- Run the `Analyzers` target to verify third-party analyzer diagnostics through `info` severity without applying fixes.
- Use explicit Cake targets such as `Build` and `Push` rather than calling `dotnet build` directly, except for one-off debugging.
- If package restore fails against internal feeds, add `https://api.nuget.org/v3/index.json` as a fallback source.
