namespace Nst.Application;

// What one call of nst ends with. Out goes to standard output, Err to standard error.
// Exit codes: 0 done, 1 the outside failed (a file, a program), 2 the call itself was wrong.
public sealed record ActionResult(int ExitCode, IReadOnlyList<string> Out, IReadOnlyList<string> Err)
{
    public static ActionResult Done(IReadOnlyList<string> output) => new(0, output, []);

    public static ActionResult Failed(string message) => new(1, [], [message]);

    public static ActionResult Misused(string message) => new(2, [], [message]);
}
