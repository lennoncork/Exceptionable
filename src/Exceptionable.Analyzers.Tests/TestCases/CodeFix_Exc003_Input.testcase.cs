// Calls File.ReadAllText from an undeclared method.
// Expected behavior: the EXC003 fix adds [Throws] declarations to the enclosing method.
class C
{
    string M(string path) => System.IO.File.ReadAllText(path);
}
