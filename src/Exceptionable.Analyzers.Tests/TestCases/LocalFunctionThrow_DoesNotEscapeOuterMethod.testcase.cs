// Tests a throw inside a local function. Local functions are outside the outer method's flow.
// Expected behavior: do not report the throw as escaping the outer method.
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
