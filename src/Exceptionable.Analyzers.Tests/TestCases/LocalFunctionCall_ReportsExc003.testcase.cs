// Tests a throw inside a local function that the outer method calls.
// Expected behavior: report EXC003 at the call. The throw is not EXC001 on the outer method.
using Exceptionable;
class C
{
    [DoesNotThrow]
    void M()
    {
        void Local()
        {
            throw new System.InvalidOperationException();
        }

        Local();
    }
}
