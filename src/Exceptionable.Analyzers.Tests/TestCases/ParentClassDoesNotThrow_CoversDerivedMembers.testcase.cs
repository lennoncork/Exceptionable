// A [DoesNotThrow] on a parent class covers derived members, including an override of ToString.
// Expected behavior: no EXC009. The throw is EXC001. The sealed call is EXC006.
using System;
using Exceptionable;
[DoesNotThrow]
class TokenBase
{
    public virtual string Describe() => "base";
}
sealed class Token : TokenBase
{
    public override string Describe() => "token";
    public override string ToString() => throw new InvalidOperationException();
}
class Caller
{
    void Call(Token token) { try { token.Describe(); } catch (Exception) { } }
}
