// Deferred bodies and calls are not analyzed as synchronous exception contracts.
using Exceptionable;
using System.Collections.Generic;
using System.Threading.Tasks;
class C {
    [Throws("System.Exception")] async Task AsyncMethod() {
        if (System.Environment.TickCount == 0) throw new System.Exception();
        await Task.Yield();
        throw new System.Exception();
    }
    [Throws("System.Exception")] async ValueTask AsyncValueTask() {
        await Task.Yield();
        throw new System.Exception();
    }
    [Throws("System.Exception")] IEnumerable<int> Iterator() {
        yield return 1;
        throw new System.Exception();
    }
    [Throws("System.Exception")] async IAsyncEnumerable<int> AsyncIterator() {
        await Task.Yield();
        yield return 1;
        throw new System.Exception();
    }
    [DoesNotThrow] void CallAsync() {
        _ = AsyncMethod();
        _ = AsyncValueTask();
    }
    [DoesNotThrow] void Enumerate() {
        foreach (var value in Iterator()) {
        }
    }
    [DoesNotThrow] async Task AwaitAsync() {
        await AsyncMethod();
        await AsyncValueTask();
    }
    [DoesNotThrow] async Task EnumerateAsync() {
        await foreach (var value in AsyncIterator()) {
        }
    }
}
