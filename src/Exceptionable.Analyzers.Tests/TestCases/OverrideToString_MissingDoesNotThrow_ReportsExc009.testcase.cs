// An override of ToString must declare [DoesNotThrow].
// Expected behavior: report EXC009 on ToString. The code fix adds the attribute.
using Exceptionable;
class Widget
{
    public override string ToString() => "widget";
}
