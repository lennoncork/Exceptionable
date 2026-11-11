// Calls polyfilled File APIs from members that catch or declare the exceptions.
// Expected behavior: no diagnostics. A catch handles the exceptions, and
// [Throws<IOException>] permits IOException and its subclasses.
using Exceptionable;
class C {
    string Caught(string path) {
        try { return System.IO.File.ReadAllText(path); }
        catch (System.Exception) { return string.Empty; }
    }

    [Throws<System.IO.IOException>]
    string Declared(string path) {
        try { return System.IO.File.ReadAllText(path); }
        catch (System.UnauthorizedAccessException) { return string.Empty; }
    }
}
