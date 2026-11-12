// Verifies the generator's unchanged-input caching: this file throws an escaping exception and is never edited.
// Expected behavior: after another file changes, the generated output is reported as cached or unchanged.
namespace Library;
public class Resource
{
    public Resource(string name)
    {
        throw new System.InvalidOperationException();
    }
}
