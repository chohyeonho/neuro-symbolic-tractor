using System.Globalization;
using Nst.Application.Ports;
using Nst.Domain;

namespace Nst.Application.Actions;

// nst lines <file> <start> [count]: numbered lines of one file from start, at most Lines.MaxLines.
// Which files may be read is the port's business (the project folder only, for the real file system).
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

    // A whole number of 1 or more, written in digits only. A number too large for an int counts as the largest
    // int, so a huge count still means "as many as allowed" and a huge start lies past the end of the file.
    static bool TryPositive(string s, out int value)
    {
        value = 0;
        if (s.Length == 0 || !s.All(char.IsAsciiDigit) || s.All(c => c == '0')) return false;
        if (!int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out value)) value = int.MaxValue;
        return true;
    }
}
