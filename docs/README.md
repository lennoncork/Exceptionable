# Exceptionable diagnostics

Analysis is off until a project sets `<Exceptionable>enable</Exceptionable>`
or an `.editorconfig` sets `exceptionable_analysis = enable`. Severity
settings do not turn analysis on. Behavior and limits are defined in
[DESIGN.md](../DESIGN.md).

| ID | Default | Meaning |
|---|---|---|
| [EXC001](diagnostics/EXC001.md) | Warning | An explicit throw escapes `[DoesNotThrow]`. |
| [EXC002](diagnostics/EXC002.md) | Warning | An explicit throw is outside the declared `[Throws]` set. |
| [EXC003](diagnostics/EXC003.md) | Warning | An annotated call may throw an exception the caller does not handle or declare. |
| [EXC004](diagnostics/EXC004.md) | Error | `[Throws]` does not name one exception type. |
| [EXC005](diagnostics/EXC005.md) | Error | `[DoesNotThrow]` and `[Throws]` conflict on the same target. |
| [EXC006](diagnostics/EXC006.md) | Warning | A `try`/`catch` cannot observe a tracked exception. |
| [EXC007](diagnostics/EXC007.md) | Warning | An `ArgumentNullException` guard may be redundant with nullable annotations. |
| [EXC008](diagnostics/EXC008.md) | Warning | A `[Throws]` type does not escape. |
| [EXC009](diagnostics/EXC009.md) | Warning | An implementation does not declare the same exception contract as an interface member. |
| [EXC010](diagnostics/EXC010.md) | Warning | A callback may throw an exception its parameter contract does not allow. |
| [EXC011](diagnostics/EXC011.md) | Error | An exception contract on a parameter requires a delegate. |

Messages name exception types with their namespace-qualified display names.

## Code fixes

The `Exceptionable.Analyzers` package includes code fixes for:

- EXC002 and EXC003: add `[Throws("<type>")]` to the enclosing method,
  constructor, accessor, or local function. Not offered for `[DoesNotThrow]`
  members or inside lambdas.
- EXC006: remove the unnecessary `catch` clauses. When the `try` has no
  `finally`, remove the `try` as well.
- EXC008: remove the stale `[Throws]` attribute.
- EXC009: copy the interface `[Throws]` or `[DoesNotThrow]` contract onto an
  implementation that does not already declare one. Not offered for a generic
  exception type.

## Framework polyfills

Framework methods carry no `[Throws]` metadata. Exceptionable ships a curated
catalog of exceptions for specific APIs, organized by namespace. It is on by
default and applies only when analysis is enabled. You can turn it off for
the project, a directory, or a single namespace.

```xml
<PropertyGroup>
  <ExceptionablePolyfills>disable</ExceptionablePolyfills>
</PropertyGroup>
```

```editorconfig
exceptionable_polyfill.System.Linq = disable
exceptionable_polyfill.System.IO = enable
```

The most specific namespace setting wins, then `exceptionable_polyfill`, then
`<ExceptionablePolyfills>`. A polyfilled call is reported as EXC003. An
explicit or inferred contract always takes precedence.

| Namespace | APIs |
|---|---|
| `System` | `Int32.Parse`, `Int64.Parse`, `Guid.Parse`, `DateTime.Parse` |
| `System.Collections.Generic` | `Dictionary` and `List` indexers, `Queue.Dequeue`/`Peek`, `Stack.Pop`/`Peek` |
| `System.IO` | `File` read, write, copy, and delete members; `Directory.CreateDirectory` and `GetFiles` |
| `System.Linq` | `First`, `Last`, `Single`, `ElementAt`, unseeded `Aggregate` |
| `System.Threading.Tasks` | `Task.Wait`, `Task.WaitAll`, `Task<T>.Result` |

Missing an API? Open an issue naming the namespace, type, member, and the
exception it documents. To add one, edit the module for its namespace in
`src/Exceptionable.Analyzers/Polyfills/` and add a test.
