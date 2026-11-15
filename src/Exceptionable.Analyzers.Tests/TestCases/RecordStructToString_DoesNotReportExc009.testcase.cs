// A compiler-generated record ToString is not required to repeat [DoesNotThrow].
// Expected behavior: no EXC009, and the call is not EXC006 until the type declares the contract.
using System;
readonly record struct Point(int X);
class Caller
{
    void Call(Point point) { try { point.ToString(); } catch (Exception) { } }
}
