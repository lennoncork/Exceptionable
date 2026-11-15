// A base method [DoesNotThrow] still constrains the override body.
// Expected behavior: EXC001 for each throw, and EXC009 because neither override declares [DoesNotThrow].
using Exceptionable;
class Base
{
    [DoesNotThrow]
    public virtual void Safe() { }
}
class Unmarked : Base
{
    public override void Safe() { throw new System.InvalidOperationException(); }
}
class Declared : Base
{
    [Throws("System.InvalidOperationException")]
    public override void Safe() { throw new System.InvalidOperationException(); }
}
