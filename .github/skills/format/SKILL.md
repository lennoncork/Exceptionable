---
name: dotnet-format
description: Run the repository-wide formatting check with dotnet format.
scope: repository
version: 1.0
---

# Dotnet Format Skill

## Description

This skill explains how to check formatting for the entire solution.

## Prerequisites
- Install a compatible .NET SDK (see `dotnet --info`).
- Restore local dotnet tools with `dotnet tool restore`.

## Commands
From the repository root (the directory containing `build.cake`):

```bash
dotnet restore src/Exceptionable.slnx --source https://api.nuget.org/v3/index.json
dotnet cake --target=Formatting
```

The `Formatting` target runs `dotnet format --verify-no-changes` against the entire solution. `dotnet cake --target=Test` also runs this check.

If formatting changes are reported, format the affected files and run the check again.
