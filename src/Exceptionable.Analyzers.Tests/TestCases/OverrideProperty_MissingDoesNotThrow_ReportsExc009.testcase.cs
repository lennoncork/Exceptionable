// An override of a [DoesNotThrow] property must declare [DoesNotThrow] on the property.
// Expected behavior: EXC009 on Missing.Value. Declared.Value is not reported.
using Exceptionable;
class Base
{
    [DoesNotThrow]
    public virtual int Value => 1;
}
class Missing : Base
{
    public override int Value => 2;
}
class Declared : Base
{
    [DoesNotThrow]
    public override int Value => 3;
}
