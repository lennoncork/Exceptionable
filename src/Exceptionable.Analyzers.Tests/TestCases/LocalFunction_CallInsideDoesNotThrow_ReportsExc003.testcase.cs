// Tests a throwing call inside a [DoesNotThrow] local function.
// Expected behavior: report EXC003 at that call. The outer method does not inherit it.
using Exceptionable;
class C
{
    void Throws()
    {
        throw new System.InvalidOperationException();
    }

    void M()
    {
        [DoesNotThrow]
        void Local()
        {
            Throws();
        }

        Local();
    }
}
