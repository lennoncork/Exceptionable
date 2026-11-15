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
| [EXC009](diagnostics/EXC009.md) | Warning | A member does not declare the contract required by an interface or a `[DoesNotThrow]` base. |
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
- EXC009: copy the required `[Throws]` or `[DoesNotThrow]` contract onto an
  implementation or override that does not already declare one. Not offered
  for a generic exception type, or when the member already declares `[Throws]`.

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
`<ExceptionablePolyfills>`. A throwing entry is reported as EXC003. A no-throw
entry makes a certain call of the catalogued member, or of a same-assembly
member that is not declared in source, `[DoesNotThrow]`: a `try`/`catch`
around only that call is EXC006. A source override or interface implementation
must declare `[DoesNotThrow]` (EXC009). Until it does, a call of that source
member is not EXC006. A known exception that escapes it is EXC001, including
when the member declares `[Throws]`. A `[DoesNotThrow]` on a parent class or
struct covers the derived member. An open virtual call stays unchecked. An
inferred contract takes precedence at the call, so a member that is known to
throw is not treated as no-throw there.

| Namespace | APIs |
|---|---|
| `System` | `Int32.Parse`, `Int64.Parse`, `Guid.Parse`, `DateTime.Parse`. No-throw: parameterless `Object.ToString`, `Object.Equals(object)`, `Object.GetHashCode`, `Object.ReferenceEquals`, `IEquatable<T>.Equals`, `IComparable<T>.CompareTo`, and same-assembly overrides such as `String` and `Int32` |
| `System.Collections.Generic` | `Dictionary` and `List` indexers, `Queue.Dequeue`/`Peek`, `Stack.Pop`/`Peek`. No-throw: `IEqualityComparer<T>.Equals` |
| `System.IO` | `File` read, write, copy, and delete members; `Directory.CreateDirectory` and `GetFiles` |
| `System.Linq` | `First`, `Last`, `Single`, `ElementAt`, unseeded `Aggregate` |
| `System.Threading.Tasks` | `Task.Wait`, `Task.WaitAll`, `Task<T>.Result` |

Missing an API? Open an issue naming the namespace, type, member, and the
exception it documents. To add one, edit the module for its namespace in
`src/Exceptionable.Analyzers/Polyfills/` and add a test.
