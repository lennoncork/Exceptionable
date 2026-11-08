// Tests a base property read. The accessor is statically bound.
// Expected behavior: report EXC003 for the base getter.
using Exceptionable;
class Base
{
    [Throws("System.IO.IOException")]
    public virtual int Value => throw new System.IO.IOException();
}
class Derived : Base
{
    [DoesNotThrow]
    public override int Value => base.Value;
}
