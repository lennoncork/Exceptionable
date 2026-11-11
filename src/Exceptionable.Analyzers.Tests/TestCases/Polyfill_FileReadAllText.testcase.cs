// Calls File.ReadAllText, an external API with no Exceptionable contract.
// Expected behavior: with the System.IO polyfill enabled, EXC003 reports IOException and
// UnauthorizedAccessException; without it, the call stays unspecified.
class C {
    string M(string path) => System.IO.File.ReadAllText(path);
}
