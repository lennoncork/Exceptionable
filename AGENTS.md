# Exceptionable Project Guidelines

## Project documents

- [DESIGN.md](./DESIGN.md) is the source of truth for Exceptionable's goals,
  analyzer contracts, behavior, diagnostics, configuration, architecture,
  limitations, and implementation roadmap.
- [RESEARCH.md](./RESEARCH.md) is the source map for nullable-analysis
  precedents, Roslyn guidance, repository examples, and implementation
  references. It supports the design; it does not override it.
- The project is named **Exceptionable**. Keep the name consistent in new or
  updated documentation, package names, examples, and configuration guidance.

## Keeping the documents current

When a task changes the intended behavior, scope, public API, configuration,
diagnostics, architecture, or roadmap:

1. Update `DESIGN.md` in the same change. Record the decision where it belongs,
   including consequences for callers, implementations, tests, and known
   limitations.
2. Update `RESEARCH.md` when the decision relies on new external guidance or
   when existing references or recommendations become outdated. Include direct
   links to the relevant authoritative documentation or repository files and
   identify repository-specific examples as examples, not requirements.
3. Keep the documents consistent. Update cross-references, terminology,
   package/project names, diagnostic IDs, and configuration examples wherever
   they are affected.
4. Preserve unresolved questions as open decisions in `DESIGN.md`; do not
   silently turn assumptions or research observations into product decisions.
5. Verify external links and the claims they support when adding or materially
   revising research. Prefer official Microsoft/Roslyn sources for API
   semantics, and use pinned repository revisions for examples when practical.
6. Keep both documents concise and actionable. Do not copy entire reference
   material into them; summarize the relevant point and link to its source.

For research-only updates, update `RESEARCH.md` without changing the design
unless the findings imply a deliberate product decision. For implementation
changes that diverge from the design, update the design in the same change or
explain why the behavior remains an intentional deviation.

## Intended repository structure

The repository currently contains the project guidance and design documents;
the following is the target layout for implementation, not a claim that these
projects and directories already exist. Keep the structure aligned with
[DESIGN.md](./DESIGN.md), using the examples and cautions in
[RESEARCH.md](./RESEARCH.md) as guidance rather than binding requirements.

```text
/
├── AGENTS.md
├── DESIGN.md
├── RESEARCH.md
├── src/
│   ├── Exceptionable.slnx
│   ├── Directory.Build.props
│   ├── Exceptionable/
│   │   └── # Public attribute API; package independently of the analyzer
│   ├── Exceptionable.Analyzers/
│   │   ├── Configuration/
│   │   ├── Contracts/
│   │   ├── Analysis/
│   │   └── Diagnostics/
│   └── Exceptionable.Analyzers.Tests/
│       └── # Analyzer tests
├── build.cake
├── build/
│   └── # Shared Cake task modules and build support
├── samples/
│   └── Exceptionable.Consumer/
└── docs/
    └── diagnostics/
```

- Keep the public attribute API (`Exceptionable` package, in the
  `Exceptionable/` project) and analyzer in separate projects and NuGet
  packages. Do not copy the attributes into the analyzer package. Put analyzer
  deployment assets in the conventional NuGet `analyzers/dotnet/...` layout.
- Keep contract discovery, attribute/name resolution, and precedence in the
  analyzer's `Contracts/` area; keep exception-flow and catch/propagation
  analysis in `Analysis/`; centralize rule descriptors and diagnostic
  reporting in `Diagnostics/`. Resolve compilation-specific symbols per
  compilation and never retain them across compilations.
- Keep the attributes tests independent from analyzer tests. Analyzer tests
  should compile representative snippets and verify positive and negative
  behavior, diagnostic IDs, arguments, and source locations. Use package tests
  and the consumer sample to verify that each package works independently and
  that the analyzer loads in a consumer.
- Use `build.cake` as the entry point for build, test, and pack workflows.
  Invoke targets through Cake, for example
  `dotnet cake build.cake --target=Build` and
  `dotnet cake build.cake --target=Test`. Keep reusable Cake task modules
  under `build/`. Validate the required .NET 10 and net481 target
  configurations.
- Put user-facing setup, configuration, and diagnostic documentation under
  `docs/`; keep the authoritative product behavior and staged implementation
  decisions in `DESIGN.md`, and research links and precedent summaries in
  `RESEARCH.md`. Avoid duplicating those documents in project-level guidance.
- Keep configuration aligned with the design: `<Exceptionable>` and
  `exceptionable_analysis` control whether analysis runs, and standard
  `dotnet_diagnostic` settings control rule severity only after it is enabled.
  Nullable analysis is the primary precedent, not a substitute for Exceptionable
  semantics.
- Do not add a separate code-fix project or other components until the design
  calls for them. Confirm still-open framework, Roslyn, and diagnostic-ID
  choices in `DESIGN.md` before treating them as repository-wide conventions.
