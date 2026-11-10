# Exceptionable — Exception Contract Analyzer Design

## 1. Vision

Exceptionable is a Roslyn analyzer that lets libraries declare which
exceptions their APIs may throw and lets callers verify that those exceptions
are handled or propagated. The design borrows the declarative,
compiler-visible approach of .NET nullable analysis: annotations describe
contracts, and analysis checks whether code honors them. Nullable analysis is
the primary reference for project-level and editor configuration.

The analyzer is static analysis, not runtime exception interception. It does
not change program behavior or guarantee that every possible exception is
known. Its first goal is a useful, predictable contract for explicitly
declared exceptions and common synchronous control flow.

The project name is **Exceptionable**. The initial distribution consists of
two separate NuGet packages:

- **Exceptionable** provides the public attributes as a normal
  compile-time library for projects that annotate APIs.
- **Exceptionable.Analyzers** provides the Roslyn analyzer and its analyzer
  build assets. It is installed as an analyzer, not as a runtime dependency.

The package boundary must keep the attributes usable without the analyzer and
the analyzer deployable independently. The analyzer recognizes the public
attributes from Exceptionable as well as equivalent
source-defined attributes, so adoption can be incremental.

## 2. Mandatory requirements

The requirements in this section are binding for all implementation,
packaging, and documentation work. They are not suggestions or open design
options. If another section conflicts with a requirement here, this section
takes precedence.

1. **Project identity:** The project is named **Exceptionable**.
2. **Separate NuGet packages:** Produce two distinct packages:
   - **Exceptionable** contains the public attributes used in
     consumer source code.
   - **Exceptionable.Analyzers** contains the analyzer and its analyzer
     deployment assets.
   The attributes package must be independently usable without the analyzer;
   the analyzer package must be separately installable as an analyzer.
3. **Analysis controls:** Provide a project-level `<Exceptionable>` XML/MSBuild
   property and `.editorconfig` settings to turn analysis on or off. Support
   `enable` and `disable` values. A scoped `.editorconfig` setting overrides
   the project-level setting. If neither setting is present, analysis is
   disabled. Per-diagnostic severity settings configure or suppress
   diagnostics only after analysis is enabled; they must not enable analysis
   by themselves.
4. **Nullable as the primary reference:** Follow .NET nullable analysis as
   the primary precedent for analysis enablement, configuration behavior,
   and IDE/build integration, while honoring the explicit Exceptionable
   configuration names and precedence above.
5. **Target support:** Support C# consumer projects targeting .NET 10 and
    .NET Framework 4.8.1 (`net481`). The attributes library targets both
    frameworks; the analyzer targets .NET Standard 2.0 so it can load in
    compatible Roslyn hosts.
6. **Build runner:** Use Cake as the build runner. Build, test, and package
    workflows must be orchestrated through Cake.
7. **Release versioning:** Derive package versions from Git history using
  GitVersion. Publish packages for published GitHub Releases, including
  prereleases, using GitVersion's stable or prerelease package version.

## 3. Goals

- Provide source-level attributes for declaring exception behavior on methods
  and types.
- Publish the attributes and analyzer in separate NuGet packages.
- Report when a declared exception is neither caught nor allowed to escape a
  method that declares it.
- Report when an implementation throws an exception outside its declared
  contract.
- Check calls to annotated APIs in the caller's context.
- Work as a normal Roslyn analyzer in the IDE and during builds, with
  configurable diagnostic severity.
- Let projects and editor configuration enable or disable Exceptionable
  analysis, following the configuration model of .NET nullable analysis.
- Define conservative behavior for code the analyzer cannot prove.

## 4. Non-goals for the initial release

- Runtime checks, exception wrapping, or generated try/catch blocks.
- Inferring exceptions from code that has no explicit throw in a supported
  member. Framework and third-party methods stay unspecified unless their
  assembly recorded an inferred contract or they carry `[Throws]`.
  `ThrowIfNull` and other helper calls stay ordinary calls. Section 6
  describes the escaping-throw inference that is in scope.
- Proving behavior across reflection, native calls, dynamic dispatch, or
  arbitrary control flow.
- Modeling asynchronous, iterator, or interprocedural behavior beyond the
  explicitly listed later phases.
- Replacing C#'s existing exception semantics or requiring every method to
  declare every exception it might indirectly encounter.

## 5. Attribute contract

The attributes are metadata consumed by the analyzer; they have no runtime
effect. They should be public, sealed, and documented in the
**Exceptionable** package, which is referenced by consumer code
that applies them. The **Exceptionable.Analyzers** package contains the
analyzer deployment assets, not a second copy of the public attribute API.
The analyzer must also recognize source-defined attributes so projects can
adopt the analyzer without requiring the attributes package.

### `[DoesNotThrow]`

Apply to a method or class to state that the annotated member does not allow an
exception to escape. On a class, the contract applies to its eligible instance
and static methods unless a member has a more specific contract.

This is a strong promise: an implementation must not let an exception escape.
It does not mean that no exception can occur inside the method; a method may
throw and handle an exception internally.

### `[Throws("Exception")]` and `[Throws<TException>]`

Apply either form to a method or class to declare an exception type that may
escape. The string form's argument is a type name, not an exception message.
`[Throws<TException>]` takes the exception type directly. `TException` must
derive from `System.Exception`. The package attribute enforces that with a
type constraint, and the analyzer applies the same inheritance check to every
recognized generic `[Throws]`, including a source-defined attribute that omits
the constraint. A type argument that does not derive from `System.Exception`
is EXC004.

To keep the string form usable while avoiding silent guessing, resolve names
as follows:

1. A fully qualified metadata name (for example,
   `"System.IO.IOException"`) resolves exactly. Nested types accept either
   the metadata form (`"N.Outer+NestedException"`) or the dotted C# form
   (`"N.Outer.NestedException"`). A dotted name that matches both a
   namespace-qualified exception and a different nested exception is
   ambiguous. A namespace-qualified type that does not derive from
   `System.Exception` does not hide a nested exception with the same dotted
   spelling.
2. A short name (for example, `"IOException"`) resolves only when it
   identifies one exception type in the compilation. Types that do not derive
   from `System.Exception` do not make the name ambiguous. The built-in
   `"Exception"` shorthand resolves to `System.Exception`. Short names are
   indexed once per compilation.
3. An unresolved or ambiguous name is an attribute-usage diagnostic; it must
   not silently disable analysis.
4. A name that identifies a type which does not derive from `System.Exception`
   is the same attribute-usage diagnostic. The message says that the type does
   not derive from `System.Exception`. A name that also matches an exception
   type still resolves to that exception type.

The declared type covers derived exception types, including an indirect
subclass and a closed constructed generic exception type such as
`MyException<int>`. Multiple `[Throws]` attributes may be applied to declare
multiple exception families. String and generic attributes on the same target
contribute to one declared set. An attribute type argument that is an open
type parameter is rejected by the compiler and is not a contract.

### Attribute usage and precedence

- The attributes are valid on methods, constructors, classes, structs,
  properties (including indexers), and events, and are inherited where C#
  attribute inheritance applies. They are not valid on an interface type.
  An interface contract is declared on each interface member. A derived
  interface member that redeclares a base member and does not declare its
  own contract keeps that base contract. When the member replaces several
  base members, the effective contract is their intersection; if the
  intersection allows nothing, the member is `[DoesNotThrow]`.
- A source implementation of an interface member that has a `[Throws]` or
  `[DoesNotThrow]` contract must declare an equivalent contract. Equivalence
  is the same `[DoesNotThrow]` or the same set of exception types. The
  implementation may declare that contract on the member, its property or
  event, an inherited override, or the containing class or struct. Inferred
  throws do not count. A mismatch is EXC009. This is the Exceptionable analog
  of nullable's interface-implementation mismatch warnings: the published
  attribution on the implementation must match the interface member. Throw
  and call checks still use the intersection described below; EXC009 does
  not replace those diagnostics. An unannotated interface member does not
  require the implementation to be unannotated. Class virtual and override
  replacement remains allowed and is not EXC009. When one method implements
  several interface members, each mismatched member is reported. Explicit
  implementations can match each interface separately.
- Accessor declarations may carry method-targeted attributes. A property or
  event contract applies to its accessors unless an accessor declares its own
  contract. Analyzer call checks include property get/set and event add/remove
  operations. Class-level contracts apply to accessors, including
  compiler-generated accessors such as auto-property getters and setters.
- The analyzer recognizes the canonical `Exceptionable.DoesNotThrowAttribute`,
  `Exceptionable.ThrowsAttribute`, and `Exceptionable.ThrowsAttribute<T>`
  identities, whether supplied by the package or defined in consumer source.
  A same-named attribute in another namespace is unrelated. The generic
  identity is the open attribute type with one type argument.
- A method-level `[Throws]` list overrides the class-level exception list for
  that method.
- An accessor contract overrides a contract on its property or event, and a
  property or event contract overrides a class or struct contract for that
  member's accessors. Those are different declaration targets, so they are not
  EXC005 conflicts.
- A method-level `[DoesNotThrow]` overrides a class-level `[Throws]` list.
- A method-level `[Throws]` overrides class-level `[DoesNotThrow]`.
- A contract declared on a derived type replaces the base type contract. A
  contract declared on an override replaces the base method contract. Neither
  replacement is a conflict with attributes that remain declared only on the
  base.
- Applying both attributes at the same declaration is invalid and reported.
  That includes a property or event that carries both attributes.
- Constructors, property and event accessors, user-defined operators, and
  user-defined conversions are included. Local functions and lambdas remain
  out of scope. A `yield` in a nested local function does not make the
  enclosing method an iterator, and a throw or call inside a local function
  or lambda is not treated as part of the enclosing method.

Precedence must be implemented centrally and covered by tests, rather than
being independently reinterpreted by each diagnostic. For a source
implementation, the effective contract is its replaced method or type contract
intersected with every implemented interface contract. `[DoesNotThrow]` in any
of those contracts allows nothing to escape. Otherwise an exception may escape
only when every declared set permits it. Throw and call diagnostics both use
that decision.

## 6. Meaning of the contract

Treat a method's contract as one of:

- **No-throw:** no exception may escape this method.
- **Declared set:** one or more exception types may escape; subtypes are
  included.
- **Unspecified:** legacy/default behavior; no new contract diagnostic is
  produced for that method.

A reachable, uncaught explicit `throw` on a supported synchronous member
becomes that member's inferred `[Throws]` contract. Supported members are
methods, constructors, accessors, operators, and conversions. The defining
compilation discovers the contract from source. The analyzer package's source
generator writes that contract into the compiled assembly, so a consuming
project can apply it from the reference alone. The record is assembly
metadata, normally using `Exceptionable.Generation.InferredThrowsAttribute`
and a numbered fallback when that name collides, keyed by the member's
documentation comment id. A source generator can add those files; it
cannot rewrite the existing member to carry `[Throws]`. The consumer's
analyzer reads the assembly attribute and treats it as the member's
`[Throws]` contract. The public attributes remain `[Throws]` and
`[DoesNotThrow]`. The generated attribute is an analyzer implementation
detail and is not part of the Exceptionable package. Constructed generic
exception types use a versioned, length-prefixed metadata representation that
includes assembly identity, containing types, and closed type arguments;
legacy non-generic display-name records remain readable. The helper attribute
uses its canonical name when available and a numbered, explicitly marked name
when user code already declares it; readers continue to accept the original
canonical form.
Private members and members in private nested types are not recorded because
referencing compilations cannot call them. Protected members remain available
to external derived types. Internal and private-protected members are recorded
only when the assembly declares `InternalsVisibleTo`; protected-internal
members remain available to external derived types.

An explicit `[Throws]` or `[DoesNotThrow]` contract replaces inference. The
explicit contract may be on the member, its property or event, its type, or
an override it inherits. Inferred types are not added to that set. Authors
use the explicit contract to control what callers must handle. A throw
outside an explicit `[Throws]` set is EXC002, and callers see only that set.
`[DoesNotThrow]` is not replaced; the throw remains EXC001.

When that member also produces a known escaping exception, a declared type
that no reachable throw or known call produces is EXC008. A derived throw or
call satisfies a declared base type. A `[Throws]` attribute on a member with
no known escaping exception remains the published contract and is not
EXC008. Caught and unreachable throws do not keep a declaration.

A call does not copy the callee's contract onto the caller. The caller must
catch the exception or declare it, or the call is EXC003. A member that only
calls a throwing member is not given the callee's contract. A constructor
that only chains with `: this()` or `: base()` is checked at that chain.

Caught throws, unreachable throws, and throws inside a local function,
lambda, or anonymous function do not count. Async methods, iterators, and
generated members are not inferred. `ArgumentNullException.ThrowIfNull` is
an ordinary call and is not an implicit source. Field and property
initializers that a constructor runs, including a primary-constructor
initializer, count as throws of that constructor. Struct constructors follow
the same rules.

An interface contract does not replace inference on a source implementation.
A call through the interface uses the interface contract. The implementation
is still checked against every interface contract it implements. Its escaping
throws are still inferred for a call on the concrete member, unless that
implementation has an explicit contract of its own. EXC009 still requires the
implementation to declare the same `[Throws]` or `[DoesNotThrow]` contract as
the interface member; inference is not that declaration.

When nullable annotations are enabled on the constructor and the escaping
throw guards a non-nullable reference parameter, EXC007 reports that the guard
may be redundant with the declared nullable contract. The same conclusion
applies when the throw names no parameter and every reference-type parameter
is a non-nullable reference. Nullable annotations are compile-time contracts,
not runtime validation: keep a guard that is part of a public API's runtime
behavior, including for nullable-oblivious callers, reflection, or other
languages. EXC007 remains an enabled warning but its wording is advisory.
Regardless, a reported guard does not add an inferred `[Throws]` contract and
is not also EXC002; a nullable-oblivious caller therefore gets no EXC003 for
that guard. An explicit `[Throws]` or `[DoesNotThrow]` still overrides
inference, and EXC007 still reports. The throw is EXC001 when the constructor's
contract is `[DoesNotThrow]`. A nullable parameter such as `string?`, or a
parameter with `[AllowNull]`, keeps the inferred contract, because the caller
is allowed to pass null. The nullable context is the annotation flag from
`SemanticModel.GetNullableContext` at the constructor declaration.

For a call with a known contract, an exception is handled if a reachable
`catch` clause catches its type or a base type. A declared exception can also
be propagated if the enclosing method's effective contract permits that
exception type, including when the permission comes from an implemented
interface contract. An exception is unhandled when neither condition holds.

For no-throw methods, all known escaping exceptions are violations; catches
that consume them are permitted. An explicit `throw` that cannot be shown to
be caught is EXC001. An annotated call that cannot is EXC003. Returning normally is not required: non-exceptional
termination such as an infinite loop is outside the contract.

A `try` with one or more `catch` clauses is unnecessary when every reachable
operation in the `try` body is a non-throwing operation or a call, object
creation, accessor, or user-defined operator whose effective contract is
`[DoesNotThrow]`, and at least one such `[DoesNotThrow]` operation is
reachable. Non-throwing operations are literals, locals, parameters, `this`,
the receiver of a null-conditional access, discards, `nameof`, `typeof`,
local assignments, implicit or `as` conversions, `if` and `?:`, `?.`, `??`,
`&&`, `||`, and `!` on built-in Boolean values. EXC006 reports that case. A
`finally` block does not keep the catch clauses necessary. Unannotated calls,
`[Throws]` calls, explicit throws, nested `try` statements, uncertain virtual
or abstract dispatch, built-in arithmetic, loops, `foreach`, `using`, and any
other operation suppress EXC006, because the catch may still handle an
exception the analyzer recognizes or cannot rule out. `nameof` does not
invoke the named member. Lambdas and local functions are not treated as
invoked by the try body.

These meanings apply to synchronous, non-iterator methods. `[Throws]` on an
`async` method does not currently describe exceptions observed when awaiting
its returned task, and a contract on an iterator does not describe exceptions
observed during enumeration. C# captures exceptions from an `async` method,
including an exception textually before its first `await`, in the returned
faulted task. Exceptions after an `await` are likewise observed when the task
is awaited. Iterator-body exceptions occur as enumeration advances; async
iterator exceptions are observed during asynchronous enumeration. The
analyzer skips async and iterator bodies and their known call sites, including
calls inside an async method, rather than treating deferred failures as
synchronous call-site throws. It also skips calls returning `Task`,
`ValueTask`, `IEnumerable`, `IEnumerator`, `IAsyncEnumerable`, or
`IAsyncEnumerator`, since it cannot distinguish eager from deferred
implementations of those APIs. Thus a `[DoesNotThrow]` async caller is not
evidence that an awaited call cannot fail. A non-async method returning
`Task` or `ValueTask` is different: its body is analyzed for immediate
throws, but callers of that deferred return type are still skipped, even when
the method throws before returning a task. These cases are intentionally out
of scope until invocation, `await`, and enumeration contracts can be modeled
separately.

The analyzer must not claim soundness for exceptions without a known contract.
A call to a member with no explicit or inferred contract is not assumed to be
non-throwing or `System.Exception`-throwing. Framework and third-party
methods stay in that category unless they declare `[Throws]` or were compiled
with this generator. Diagnostics stay on contracts the analyzer can see.

## 7. Initial analysis scope

Analyze synchronous method bodies, constructors, and accessors with semantic
information from Roslyn. The initial implementation should recognize:

- Explicit `throw` and `throw;` statements, including a null operand as
  `System.NullReferenceException`.
- An escaping explicit `throw` on a supported member. It is that member's
  effective `[Throws]` contract unless an explicit attribute replaces it, or
  EXC007 reports that a constructor `ArgumentNullException` may be redundant
  with its nullable parameter contract.
- Calls, object creation, property getter/setter accesses, event add/remove
  accessors, user-defined operators, and user-defined conversions whose
  target has an effective `[Throws]` contract.
- Field and property initializers, synchronous `foreach` and `using` pattern
  methods, and deconstruction assignments.
- A `try`/`catch` whose reachable body only reaches `[DoesNotThrow]` calls
  and non-throwing operations (EXC006).
- Catches with exact, base-type, or catch-all (`catch` without a type)
  coverage.
- Propagation permitted by the enclosing method's effective declared set.
- `finally` blocks conservatively: an exception thrown from a `finally` block
  can replace a pending exception and is not caught by the same `try`.
- Contracts on interface methods at calls statically bound through an
  interface-typed value. Source implementation methods are checked against
  every implemented interface contract, including explicit implementations,
  inherited interface members, and multiple interfaces mapped to one method.
  A source implementation whose declared contract is not equivalent to an
  implemented interface contract is EXC009.

Use Roslyn's operation, control-flow, and symbol APIs rather than text
matching. Generated code is not analyzed. Analyze operations only in
reachable control-flow blocks. Reachability is cached per control-flow root,
so a field initializer and a constructor body do not share a graph. A catch
proves handling only when the operation is in the corresponding `try` body and
the catch type covers the exception. A non-constant exception filter does not
prove handling. When type or flow information is unknown, prefer not to report
a definite violation; report an unsupported/unknown case only if a later
diagnostic is introduced specifically for it.

An interface call uses the contract declared by the statically selected
interface member. A redeclaration without its own contract uses the base
member contract described above. Calls through different interfaces use
their respective contracts. An implementation must satisfy all known interface contracts it
implements, so conflicting declarations constrain the implementation to the
intersection of their permitted exception sets. Such declarations are not
themselves EXC005 conflicts when placed on separate interfaces; they are
EXC009 when the implementation's declared contract does not match each
interface member. Explicit and implicit
implementations are mapped with Roslyn symbols, and inherited interface
members retain the contract on the member's declaring interface. An interface
contract is trusted for external implementations; a source method inherited
from a base class that does not itself implement the interface is not
validated against a derived class's interface contract. EXC009 follows the
same mapping: it is reported on a source implementation declared by the type
that implements the interface. Open virtual,
abstract, and unsealed override calls stay unsupported because the runtime
target can differ from the statically selected method. A sealed method, and a
method declared on a sealed type, is analyzed as that method. A `base` access
is analyzed as the selected member.

For `throw expression`, report the expression's static exception type before
the implicit conversion to `System.Exception` required by C#. A thrown
expression that is a compile-time null, including `throw null` and
`throw default(ExceptionType)`, escapes as `System.NullReferenceException`.
For `throw;`, use the enclosing catch type; a bare `catch` is treated as
`System.Exception`. Catch-all clauses handle known exceptions when they
enclose the operation in the `try` body, but do not handle a rethrow from
their own handler.

`base.` calls and member accesses are statically bound to the selected
member, including when that member is virtual. Field and property
initializers are checked against every constructor that executes them.
Constructors that chain with `: this()`, and compiler-generated record copy
constructors, do not execute those initializers. A constructor the user
writes with that signature still runs them. A positional parameter of the
record type conflicts with the synthesized copy constructor and does not
compile. A `this()` or `base()` constructor call is
still checked as a call. Synchronous `foreach` checks `GetEnumerator`,
`MoveNext`, `Current`, `Dispose`, and a deconstruction of the element.
Synchronous `using` checks `Dispose`, including an explicit interface
implementation. Deconstruction assignment checks setters on target
properties and the selected `Deconstruct` method. These pattern methods are
immediate calls even when their return type is otherwise treated as
deferred. `nameof` is not a call.

### Deferred analysis cases

Keep the first release focused and document its limits. Add explicit support
in later phases for:

- `async` methods, including exceptions observed when awaiting a task, and
  task-returning calls. An eventual model must distinguish invocation from
  `await`, handle catches around `await`, and propagate only exceptions
  observed by the caller.
- Iterators, where exceptions can arise during `MoveNext` or asynchronous
  enumeration rather than when the iterator method is called. A model must
  account for enumeration catches and deferred caller contracts.
- Lambdas and delegates. Open virtual, abstract, and unsealed override
  dispatch remains unsupported because the runtime target may differ from
  the statically selected method.
- Non-constant exception filters and complex rethrow/data-flow cases.
- Open exception type parameters as thrown types or `[Throws<T>]` arguments.
  A closed constructed type that derives from `System.Exception` is a valid
  generic attribute argument.
- External APIs with contracts distributed through reference assemblies.

Until a case is supported, it must not produce misleading certainty. Async
and iterator bodies and their recognized deferred call sites are skipped;
custom awaitables, custom enumerable abstractions, and external iterator
implementations that cannot be identified from source remain outside the
current guarantees. Document these limitations in help text and tests.

## 8. Diagnostics

Diagnostics use the `EXC` prefix. Descriptors include IDs, titles, messages,
categories, default severities, and help links. The catalog is
[docs/README.md](./docs/README.md).

| ID | Condition | Default |
|---|---|---|
| EXC001 | An explicit `throw` escapes a `[DoesNotThrow]` method or type. | Warning |
| EXC002 | An explicit `throw` escapes a `[Throws]` method but is not in its declared set. | Warning |
| EXC003 | A call, creation, accessor, operator, conversion, or supported pattern method may throw an exception that is neither caught nor permitted by the caller's contract. This includes a call inside a `[DoesNotThrow]` method. | Warning |
| EXC004 | `[Throws]` names no exception type, names an ambiguous type, or names a type that does not derive from `System.Exception`. | Error |
| EXC005 | `[DoesNotThrow]` and `[Throws]` conflict on the same class, struct, member, property, or event. | Error |
| EXC006 | A `try`/`catch` is unnecessary because every reachable operation in its body is non-throwing or a `[DoesNotThrow]` call, and at least one such call is reachable. | Warning |
| EXC007 | A constructor throws `ArgumentNullException` for a non-nullable reference parameter while nullable annotations are enabled; the guard may be redundant with the nullable contract, which is not runtime validation. | Warning |
| EXC008 | A `[Throws]` type does not escape, and the member produces some other known escaping exception. Remove the stale declaration or restore a throw or call that produces it. | Warning |
| EXC009 | A source implementation's declared `[Throws]` or `[DoesNotThrow]` contract does not match an implemented interface member. | Warning |

EXC001 and EXC002 report explicit throws. EXC008 reports a `[Throws]`
attribute whose type no longer escapes. A call whose contract is explicit
or inferred is EXC003 even when the caller is `[DoesNotThrow]`. A throw
covered by the inferred set is not EXC002. A nullable-redundant constructor
`ArgumentNullException` is EXC007 and is not also EXC002. Messages name
exception types with their namespace-qualified display names, so two exceptions that share a
short name stay distinct. Diagnostics identify the relevant throw or call
site. Avoid diagnostics for exception sources with no known contract.
EXC009 reports the implementing member when its declared contract is not
equivalent to the interface member's declared contract.

## 9. Analysis configuration

Analysis is controlled independently of the attributes: a project may retain
exception annotations while temporarily disabling diagnostics. Follow
nullable analysis as the primary model, using a project-level MSBuild property
and per-directory/file `.editorconfig` configuration.

The project-level switch is the `<Exceptionable>` property:

```xml
<PropertyGroup>
  <Exceptionable>enable</Exceptionable>
</PropertyGroup>
```

`enable` turns Exceptionable analysis on and `disable` turns it off. If the
property is omitted, analysis is disabled, following the opt-in default of
nullable analysis. The analyzer package must expose this property to the
analyzer through compiler-visible build properties so the same setting applies
in IDE and command-line builds.

An `.editorconfig` can override the project switch for files under its scope:

```editorconfig
[*]
exceptionable_analysis = enable

[Legacy/**/*.cs]
exceptionable_analysis = disable
```

The supported values are `enable` and `disable`. Any other value is ignored,
so a typo does not override `<Exceptionable>`. A recognized scoped
`exceptionable_analysis` value takes precedence over `<Exceptionable>`; when
neither is set, analysis is disabled. When analysis is enabled,
`dotnet_diagnostic.EXCxxx.severity` remains the standard per-diagnostic
severity and suppression mechanism:

```editorconfig
dotnet_diagnostic.EXC001.severity = warning
dotnet_diagnostic.EXC003.severity = none
```

Disabling Exceptionable analysis suppresses all Exceptionable diagnostics for
the affected compilation or file, regardless of per-rule severity settings.
Per-rule settings do not turn analysis on when the master switch is disabled.
Disabling a file does not remove `[Throws]` or `[DoesNotThrow]` metadata, and
it does not stop the generator from recording an inferred contract in the
compiled assembly. Callers in an enabled file are still checked against a
contract declared in a disabled file. A consuming project that enables
analysis sees both explicit attributes and inferred contracts stored in a
referenced assembly.

There is no `#exceptionable` preprocessor directive. `#nullable` changes the
nullable context from one line to the next because the C# compiler parses that
directive and records the context per source position. The directive grammar
is closed, and an analyzer cannot register a new directive or a similar
per-position context. `#exceptionable` is compiler error CS1024. `#pragma
exceptionable` is warning CS1633; Roslyn does not give analyzers a custom
pragma. `#:exceptionable` is the file-based-app form: it is rejected in
project compilations (CS9298) and cannot appear after the first token, so it
cannot select a region of a file. Per-file enablement remains
`exceptionable_analysis`. `#pragma warning disable` with specific `EXC` ids
can suppress diagnostics inside a span after analysis is enabled; it cannot
turn analysis on. The parser behavior and links are in
[RESEARCH.md](./RESEARCH.md).

## 10. Suggested architecture

Keep the public attributes independent of analyzer implementation details.
Organize the analyzer package into these logical parts:

1. **Attribute declarations** — `DoesNotThrowAttribute` and repeatable
   `ThrowsAttribute` in Exceptionable, with the documented targets
   and inheritance behavior.
2. **Contract reader** — finds source or metadata attributes, resolves
   exception names to `INamedTypeSymbol`, applies inheritance and precedence,
   and returns a single normalized contract representation.
3. **Exception-flow analysis** — identifies known exception sources,
   propagates them through supported control flow, and determines whether
   catches or the enclosing contract handle them.
4. **Diagnostics** — declares rules and reports locations and arguments;
   analysis enablement and per-rule severity follow standard Roslyn
   configuration.
5. **Tests** — analyzer tests for valid and invalid code, attribute lookup,
   inheritance, and documented limitations.

The implementation keeps `ExceptionableAnalyzer` as the Roslyn registration
entry point. A compilation-start action creates an `ExceptionableCompilationContext`
that owns the compilation, `AnalysisOptions`, `ExceptionTypeResolver`,
`ContractResolver`, and `ReachabilityAnalysis`. These objects and any symbols
they reference are scoped to that compilation; none are stored in static
state.

The `Contracts/` area contains the normalized `ExceptionContract`,
`ContractResolver`, `ExceptionTypeResolver`, and attribute/symbol helpers.
`ContractResolver` is the only place that applies method, type, override, and
interface precedence. Resolved names, declared contracts, and effective
contracts are cached for the compilation.
`Analysis/` contains separate declaration, throw, call, unnecessary-catch,
and interface-contract-match handlers, plus exception-flow, operation-target,
method-policy, reachability, and inferred-throw components. `InferredThrowAnalysis` scans
source for the defining compilation and reads generated metadata for a method
that has no declaring syntax.
`Generation/` contains the incremental generator that writes that metadata
and the marker used to recognize it. The generator lives in the analyzer
assembly so the NuGet `analyzers/dotnet/cs` asset loads it with the analyzer.
It does not add a public attribute to the Exceptionable package.
`Diagnostics/` owns the descriptors. Keep rule handlers focused on one
diagnostic path and share semantic helpers rather than duplicating contract
resolution or operation traversal.

Test contract resolution, exception-name resolution, configuration, exception
flow, target-method selection, method policy, and reachability in focused unit
test classes. Retain analyzer-level tests for combined Roslyn behavior,
diagnostic IDs, arguments, and source locations. Use compilation-start actions
to resolve framework symbols once per compilation, and register only the
operation/symbol actions needed for supported constructs. Package the analyzer
in the conventional NuGet analyzer assets location and verify both packages
in a consumer project.

## 11. Implementation status

The initial analyzer includes the phase 0 through phase 3 behavior and the
phase 4 documentation:

- Separate Exceptionable and Exceptionable.Analyzers packages, Cake build,
  test, and pack, and a consumer sample for `net10.0` and `net481`.
- `<Exceptionable>` and `exceptionable_analysis`, including file-scoped
  overrides and per-rule severity.
- Both attribute forms, name resolution, precedence, and EXC004 and EXC005.
- Explicit throws (EXC001 and EXC002), annotated calls (EXC003), and
  unnecessary `try`/`catch` (EXC006), including constructors, accessors,
  initializers, pattern methods, and interface contracts.
- A reachable uncaught throw on a supported synchronous member is an inferred
  `[Throws]` contract. An explicit `[Throws]` or `[DoesNotThrow]` contract
  replaces that inference. The generator persists the inferred contract in
  the compiled assembly for referencing compilations. A constructor
  `ArgumentNullException` reported by EXC007 is not inferred.
- EXC008 warns when a `[Throws]` type is stale relative to the known escaping
  exceptions. A `[Throws]` member with no known escaping exception is left
  unchanged.
- EXC009 warns when a source implementation does not declare the same
  `[Throws]` or `[DoesNotThrow]` contract as an implemented interface member.
- Supported hosts are recorded in section 13.

Still open:

- Code fixes that insert `[Throws]` into user source. Add one only where a
  safe fix is clear. Never generate a catch that silently suppresses an
  exception.
- Reading throws from assemblies that were not compiled with this generator.
  Framework and third-party methods stay unspecified unless they declare
  `[Throws]`.
- Generator profiling used synthetic single-file compilations with 50, 250,
  and 1,000 methods (two throw sites each), one warmup, and medians of three
  runs. `RunGeneratorsAndUpdateCompilation` took 79/121/372 ms initially and
  18/68/186 ms for a one-method throw-type edit. A per-member output prototype
  did not improve edit times (17/105/310 ms), so it was not retained. The
  compilation-wide callback remains; these synthetic results do not replace
  profiling in a representative IDE project before a deeper pipeline redesign.
- Async/await and iterator contracts, before enabling rules for those
  constructs. Override compatibility beyond the sealed and `base` cases also
  remains future work. EXC009 covers interface implementations only; class
  virtual and override contract replacement stays allowed.

## 12. Testing strategy and acceptance criteria

Use Roslyn analyzer test helpers with compilable source snippets and verify
both diagnostics and their source locations. Include positive and negative
tests for each rule.

Keep every C# source snippet compiled or analyzed by a test in a named
`src/Exceptionable.Analyzers.Tests/TestCases/*.testcase.cs` fixture, including
snippets used by lower-level Roslyn unit tests and multi-assembly generator
tests. Load fixtures through `ExceptionableAnalyzerTests.GetTestCase`; keep
assertions and test logic in the unit-test methods. Start each fixture with a
brief comment describing the scenario and expected behavior. When a test needs
multiple compilations, use a separate fixture for each source input.

Minimum acceptance criteria for the initial usable release:

- An annotated throw that is caught does not produce an escaping-exception
  diagnostic.
- An annotated exception that is neither caught nor declared is diagnosed at
  the call or throw site.
- A declared exception, including its derived types, may propagate through a
  caller with a compatible contract.
- A `[DoesNotThrow]` implementation that lets a known exception escape is
  diagnosed, while one that catches it is accepted.
- A single `try`/`catch` around a call whose effective contract is
  `[DoesNotThrow]` reports EXC006. The same shape around a `[Throws]` call,
  an unannotated call, an open virtual call, or a `[DoesNotThrow]` call beside
  a reachable throw does not.
- Conflicting or unresolved annotations produce clear attribute diagnostics.
- The two NuGet packages can be built and consumed independently for their
  documented purposes.
- `<Exceptionable>enable</Exceptionable>` enables analysis and
  `<Exceptionable>disable</Exceptionable>` disables it.
- `.editorconfig` can enable or disable analysis for its scope and overrides
  the project-level switch.
- Per-rule severity settings can suppress or configure diagnostics when
  analysis is enabled, but do not enable analysis by themselves.
- A call to a member with no explicit contract and no escaping throw stays
  unspecified.
- A supported member that lets an exception escape is treated as `[Throws]`
  for each escaping type, unless an explicit contract replaces inference or
  EXC007 applies.
- An explicit `[Throws]` or `[DoesNotThrow]` is the whole contract callers
  see. Inferred types are not added to it.
- A `[Throws]` type that no reachable throw or known call produces is EXC008
  when the member produces some other known escaping exception. A member
  whose `[Throws]` contract has no known escaping exception is not EXC008.
- A source implementation that omits or changes an implemented interface
  `[Throws]` or `[DoesNotThrow]` contract is EXC009. A matching declaration
  on the member, its property or event, an inherited override, or the
  containing type is accepted. An inferred throw is not a match.
- A referencing compilation reports EXC003 for an inferred contract recorded
  in a referenced assembly, without that member's source.
- A nullable-aware constructor that throws `ArgumentNullException` for a
  non-nullable reference parameter reports advisory EXC007 and does not
  publish that exception. Nullable-oblivious callers can still trigger the
  runtime guard without receiving EXC003. A nullable or `[AllowNull]`
  parameter keeps the inferred contract.
- The analyzer builds, loads in a consumer project, and runs without
  diagnostics caused by the analyzer itself.
- Tests explicitly capture unsupported cases so future phases can expand
  coverage without changing undocumented assumptions.

## 13. Implementation decisions and open questions

The initial implementation confirms these decisions:

- The attributes target .NET 10 and .NET Framework 4.8.1; the analyzer targets
  .NET Standard 2.0 and uses Roslyn 4.14. That compiler version loads in
  Visual Studio 2022 17.14 or later and in .NET SDK builds that include
  Roslyn 4.14 or newer. Tests parse with `LanguageVersion.Latest` from that
  compiler reference. Generated code is excluded.
- `[Throws<TException>]` requires a consumer target that supports generic
  attributes, which is .NET 7 and later. A `net481` consumer uses the string
  form. The attribute assembly itself still multi-targets `net481` so the
  string form is available there.
- Per-file analysis uses `exceptionable_analysis`. There is no `#exceptionable`
  directive. The compiler rejects that syntax, and an analyzer cannot add one.
  See section 9.
- Diagnostics use the `EXC` prefix. EXC001-EXC003 and EXC006-EXC009 are
  enabled warnings and EXC004-EXC005 are enabled errors.
- A reachable uncaught throw on a supported synchronous member adds an
  inferred `[Throws]` contract for that exception. The generator records it
  with the member's documentation comment id. An explicit `[Throws]` or
  `[DoesNotThrow]`, including a type or override contract, replaces
  inference. A call does not copy a contract onto its caller. EXC007 uses
  the constructor declaration's nullable annotation context and drops only
  that constructor `ArgumentNullException` from inference; it is advisory,
  not a claim that the runtime guard is unnecessary. `[AllowNull]` and a
  nullable reference parameter keep the inferred contract. `ThrowIfNull` is
  not an implicit source. EXC008 compares a direct `[Throws]` set with the
  known escaping throws and calls of the members that set governs. Closed
  constructed generic exception types are serialized with their metadata
  identity and resolved in consuming compilations.
- EXC006 reports a `try`/`catch` whose reachable `try` body cannot throw a
  tracked exception because every call there has a `[DoesNotThrow]` contract
  and every other reachable operation is non-throwing. It is not reported for
  a `try`/`finally` without a catch, or when the body contains a recognized
  exception source or an operation the analyzer cannot account for.
- Class-level contracts apply to methods, constructors, and accessors,
  including auto-property accessors. Property and event contracts apply to
  their accessors unless an accessor declares its own.
- Interface contracts apply at interface-typed call sites and, together with
  the implementation's own contract, at throws and calls inside a source
  implementation. A source implementation must also declare an equivalent
  `[Throws]` or `[DoesNotThrow]` contract; a mismatch is EXC009. External
  implementations and methods inherited from a
  non-implementing base class are trusted rather than proven. The attributes
  cannot be applied to an interface type. A derived interface member that
  redeclares a base member and has no contract of its own keeps the base
  contract; several base contracts intersect, and an empty intersection is
  `[DoesNotThrow]`.
- A derived type contract replaces a base type contract, and an override's
  contract replaces the base method contract. Open virtual, abstract, and
  unsealed override call sites remain unsupported. Sealed methods and methods
  on sealed types are checked at the call.
- A short exception name is an error when more than one exception type uses
  it. EXC004 says that the name is ambiguous. An unresolved name uses the
  invalid-type wording. A name or generic type argument that identifies a
  type which does not derive from `System.Exception` says so. Non-exception
  types are ignored when deciding ambiguity if an exception type matches.
  Attribute strings do not use source-file `using` directives. A leading
  `global::` is ignored. Nested exception names accept dotted and metadata
  forms. A dotted name that matches two different exception types is
  ambiguous. A non-exception namespace type does not hide a nested exception
  with the same dotted spelling. Diagnostic messages use the
  namespace-qualified exception display name. EXC001 and EXC002 are explicit
  throws; annotated calls, including calls inside `[DoesNotThrow]`, are
  EXC003.
- `[Throws<TException>]` declares the same contract as the string form.
  `TException` must derive from `System.Exception`. String and generic
  attributes on one target form one declared set.
- Property and event contracts apply to their accessors unless an accessor
  declares its own contract. User-defined operators and conversions are
  checked at their bodies and call sites. `base` access is statically bound.
  Field and property initializers are checked against the constructors that
  run them. Compiler-generated record copy constructors do not run
  initializers; a user-written constructor with that signature does. A
  positional parameter of the record type is illegal because it conflicts
  with the synthesized copy constructor.
  Synchronous `foreach` and `using` pattern methods, and
  deconstruction, are calls. `throw null` is `NullReferenceException`.
  `nameof`, local functions, and lambdas are not enclosing-method throws.
- GitVersion derives package versions from Git history. Feature-branch and
  other non-release commits use ContinuousDelivery so the prerelease number
  changes with each commit. Package versions use that SemVer value and omit
  `+` build metadata, which NuGet does not keep in the package identity.
  Stable and prerelease GitHub Releases both publish their
  GitVersion-generated package versions. `main` remains labeled `beta` until
  a release tag selects the stable or prerelease version.

Async/await and iterator contracts remain open. `[Throws]` currently describes
only synchronous exceptions; it does not describe faults observed when awaiting
tasks or enumerating iterators. Task-like and standard enumerable-returning
calls are skipped to avoid reporting deferred exceptions as immediate ones.
The same skip includes synchronous throws inside async and iterator methods.
`foreach` still checks `GetEnumerator` because that call is immediate.
Open virtual and unsealed override call sites also remain unsupported.
`base` access is checked. Local functions and lambdas are not part of the
enclosing method.
