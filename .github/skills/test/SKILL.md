---
name: test
description: Canonical test instructions for the repository.
scope: repository
version: 1.0
---

# Test Skill

## Description

This skill contains the canonical test instructions for the repository. Agents should follow these steps when running tests.

## Prerequisites
- Install a compatible .NET SDK (see `dotnet --info`).
- Restore local dotnet tools with `dotnet tool restore`.

## Commands
From the repository root (the directory containing `build.cake`):

```bash
dotnet tool restore
dotnet restore src/Exceptionable.slnx --source https://api.nuget.org/v3/index.json
dotnet cake --target=Test
```

## Guidance
- Prefer the `dotnet cake` `Test` target for the unit test suite.
- Use `dotnet cake --target=Validate` to include formatting, analyzer checks,
  and the packaged consumer build.
- Run the `Test` target after making significant changes.
- Use `dotnet test` only for quick, isolated tests when debugging a single project.
