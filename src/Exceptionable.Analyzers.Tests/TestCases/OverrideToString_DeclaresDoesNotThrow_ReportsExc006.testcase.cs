// A source ToString that declares [DoesNotThrow] is a no-throw call when dispatch is certain.
// Expected behavior: report EXC006 and do not report EXC009.
using System;
using Exceptionable;
struct Token
{
    [DoesNotThrow]
    public override string ToString() => "token";
}
class Caller
{
    void Call(Token token) { try { token.ToString(); } catch (Exception) { } }
}
