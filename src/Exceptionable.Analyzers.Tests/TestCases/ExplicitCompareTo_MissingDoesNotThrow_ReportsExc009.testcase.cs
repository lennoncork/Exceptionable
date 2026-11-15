// An explicit implementation of a no-throw polyfill must declare [DoesNotThrow].
// Expected behavior: EXC009 on CompareTo, and EXC001 for the escaping throw.
using System;
class Token : IComparable<int>
{
    int IComparable<int>.CompareTo(int other) => throw new InvalidOperationException();
}
