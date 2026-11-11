// Calls File.ReadAllText from a method marked [DoesNotThrow].
// Expected behavior: EXC003 is reported but no code fix is offered, because declaring would contradict the contract.
using Exceptionable;
class C
{
    [DoesNotThrow]
    string M(string path) => System.IO.File.ReadAllText(path);
}
