using System.Diagnostics;
using System.Text;

namespace Nst.Tests;

// Runs the built nst (nst.dll next to this test assembly) as its own process, the way a session calls it.
static class NstProcess
{
    public sealed record Result(int ExitCode, string[] OutLines, string Err);

    // Longer than any call should take; a call still running then is killed and the test fails.
    static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    public static Result Run(params string[] args) => RunIn(Environment.CurrentDirectory, args);

    // Runs nst from folder, as a session launched in that project folder would.
    public static Result RunIn(string folder, params string[] args) => RunWith(folder, folder, args);

    // Runs nst from folder with NST_ROOT set to root, or not set at all when root is null.
    public static Result RunWith(string? root, string folder, params string[] args)
    {
        var nst = Path.Combine(AppContext.BaseDirectory, "nst.dll");
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = folder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        psi.Environment.Remove("NST_ROOT");
        if (root is not null) psi.Environment["NST_ROOT"] = root;
        psi.ArgumentList.Add(nst);
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEndAsync();
        var outText = p.StandardOutput.ReadToEndAsync();
        if (!p.WaitForExit(Limit))
        {
            p.Kill(entireProcessTree: true);
            Assert.Fail($"nst {string.Join(' ', args)} was still running after {Limit.TotalSeconds} seconds and was killed.");
        }
        p.WaitForExit();
        var lines = outText.Result.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        if (lines.Length > 0 && lines[^1].Length == 0) lines = lines[..^1];
        return new Result(p.ExitCode, lines, err.Result);
    }
}
