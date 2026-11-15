// A derived type [Throws] replaces a parent type [DoesNotThrow].
// The override of a [DoesNotThrow] method must still declare [DoesNotThrow].
// Expected behavior: EXC009 on Safe, and EXC001 for the throw.
using Exceptionable;
[DoesNotThrow]
class Base
{
    [DoesNotThrow]
    public virtual void Safe() { }
}
[Throws("System.InvalidOperationException")]
class Derived : Base
{
    public override void Safe() { throw new System.InvalidOperationException(); }
}
