using Nst.Application.Ports;
using Nst.Domain;

namespace Nst.Application.Actions;

// nst lines <file> <start> [count]: numbered lines of one file from start, at most Lines.MaxLines.
public sealed class LinesAction(ITextFiles files) : IAction
{
    public string Name => "lines";

    public string Usage => $"nst lines <file> <start> [count]   numbered lines of one file from start, at most {Lines.MaxLines}";

    public ActionResult Run(IReadOnlyList<string> args)
    {
        if (args.Count is < 2 or > 3 || !TryPositive(args[1], out var start))
            return ActionResult.Misused("usage: " + Usage);
        var count = Lines.MaxLines;
        if (args.Count == 3 && !TryPositive(args[2], out count))
            return ActionResult.Misused("usage: " + Usage);
        try
        {
            var n = start;
            return ActionResult.Done(Lines.Select(files.ReadLines(args[0]), start - 1, count)
                .Select(line => $"{n++}: {line}")
                .ToList());
        }
        catch (IOException ex)
        {
            return ActionResult.Failed($"nst lines: {ex.Message}");
        }
    }

    static bool TryPositive(string s, out int value) => int.TryParse(s, out value) && value >= 1;
}
