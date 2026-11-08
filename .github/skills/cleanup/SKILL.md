---
name: code-cleanup
description: Canonical code cleanup instructions for the repository.
scope: repository
version: 1.0
---

# Code Cleanup Skill

## Description

This skill applies the repository's ReSharper cleanup profile to changed C# files.

## Prerequisites

- Install a compatible .NET SDK (see `dotnet --info`).
- Install the JetBrains ReSharper command-line tools if `dotnet jb` is unavailable:

```bash
dotnet tool install --global JetBrains.ReSharper.GlobalTools
```

## Commands

From the repository root (the directory containing `build.cake`), run cleanup for the changed C# files. Paths passed to `--include` are relative to the `src` solution directory and must be separated with semicolons.

```bash
dotnet jb cleanupcode src/Exceptionable.slnx --include="Exceptionable/ChangedFile.cs;Exceptionable.Analyzers.Tests/ChangedTests.cs" --settings="src/Exceptionable.sln.DotSettings" --profile="Exceptionable: Full Cleanup"
dotnet cake --target=Build
```

## Guidance

- Run cleanup after adding or modifying C# files.
- Limit `--include` to files changed for the current task; do not clean the whole solution unless explicitly requested.
- Review cleanup changes and preserve unrelated user edits.
- Run the Cake `Build` target after cleanup to mirror repository validation.
- Run the `Test` skill as well when cleanup affects behavior or test code.
