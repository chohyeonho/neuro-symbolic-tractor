using System.Diagnostics;
using System.Text;

namespace Nst.Tests;

// Runs the built nst (nst.dll next to this test assembly) as its own process, the way a session calls it.
static class NstProcess
{
    public sealed record Result(int ExitCode, string[] OutLines, string Err);

    public static Result Run(params string[] args) => RunIn(Environment.CurrentDirectory, args);

    // Runs nst from folder, as a session launched in that project folder would.
    public static Result RunIn(string folder, params string[] args)
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
        psi.ArgumentList.Add(nst);
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEndAsync();
        var outText = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        var lines = outText.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        if (lines.Length > 0 && lines[^1].Length == 0) lines = lines[..^1];
        return new Result(p.ExitCode, lines, err.Result);
    }
}
