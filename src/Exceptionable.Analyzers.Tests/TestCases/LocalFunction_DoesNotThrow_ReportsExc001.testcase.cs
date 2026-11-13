// Tests a [DoesNotThrow] local function that throws.
// Expected behavior: report EXC001 on the local function. The outer method is not EXC001.
using Exceptionable;
class C
{
    void M()
    {
        [DoesNotThrow]
        void Local()
        {
            throw new System.InvalidOperationException();
        }

        Local();
    }
}
