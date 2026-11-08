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

Messages name exception types with their namespace-qualified display names.
