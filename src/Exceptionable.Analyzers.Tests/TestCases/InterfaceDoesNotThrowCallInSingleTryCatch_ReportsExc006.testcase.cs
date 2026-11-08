// Tests a single try/catch around an interface call marked DoesNotThrow.
// Expected behavior: report EXC006 using the interface member contract.
using Exceptionable;
interface IApi {
    [DoesNotThrow] void Execute();
}
class Caller {
    void Call(IApi api) { try { api.Execute(); } catch (System.Exception) { } }
}
