// Verifies the generator's unchanged-input caching: the original version of the file that is later edited.
// Expected behavior: it contains no throw, so editing it must not change the generated output.
namespace Library;
public class Other
{
    public int Value => 1;
}
