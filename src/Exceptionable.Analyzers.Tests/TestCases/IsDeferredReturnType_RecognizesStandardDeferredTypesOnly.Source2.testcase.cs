// Verifies Is Deferred Return Type Recognizes Standard Deferred Types Only.

using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    Task Work() => Task.CompletedTask;
    ValueTask WorkValue() => default;
    IEnumerable<int> Items() => null;
    IAsyncEnumerable<int> Stream() => null;
    IAsyncEnumerator<int> Move() => null;
    int Value() => 0;
}

