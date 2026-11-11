// Source code declares a type with the same namespace and name as a polyfilled API.
// Expected behavior: polyfills apply only to external members, so the source member is not matched.
namespace System.IO {
    public static class File {
        public static string ReadAllText(string path) => path;
    }
}
class C {
    string M(string path) => System.IO.File.ReadAllText(path);
}
