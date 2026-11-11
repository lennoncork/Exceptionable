// Verifies the generator's unchanged-input caching: the edited version of the second file, with an unrelated member added.
// Expected behavior: the generated output from the first file is not recomputed.
namespace Library;
public class Other
{
    public int Value => 1;

    public int Other2 => 2;
}
