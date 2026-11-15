// Source members that override or implement a no-throw polyfill and let an exception escape.
// Expected behavior: EXC001 on each throw. The sealed struct call is EXC003.
// The unsealed class call is not. A catch around the throwing struct call is not EXC006.
using System;

struct BadToken
{
    public override string ToString() => throw new InvalidOperationException();
    public void Leak() { ToString(); }
    public void Caught() { try { ToString(); } catch (InvalidOperationException) { } }
}

class OpenWidget
{
    public override string ToString() => throw new InvalidOperationException();
    public void Leak() { ToString(); }
}

struct BadCompare : IComparable<int>
{
    public int CompareTo(int other) => throw new InvalidOperationException();
}
