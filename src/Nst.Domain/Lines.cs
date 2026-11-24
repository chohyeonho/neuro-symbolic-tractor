namespace Nst.Domain;

// The constitution, rule 1: one call of `nst lines` prints at most MaxLines lines of a file.
// lean/NstProof.lean proves the rule for selectLines, a model of Select below; keep the two in the same shape.
public static class Lines
{
    public const int MaxLines = 120;

    // Same shape as selectLines: drop start0 lines, then take min(count, MaxLines).
    public static IEnumerable<string> Select(IEnumerable<string> fileLines, int start0, int count) =>
        fileLines.Skip(start0).Take(Math.Min(count, MaxLines));
}
