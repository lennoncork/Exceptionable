// Tests that a yield inside a local function does not skip the outer method.
// Expected behavior: report EXC001 for the throw in the DoesNotThrow method.
using Exceptionable;
using System.Collections.Generic;
class C
{
    [DoesNotThrow]
    void M()
    {
        IEnumerable<int> Local()
        {
            yield return 1;
        }

        throw new System.InvalidOperationException();
    }
}
