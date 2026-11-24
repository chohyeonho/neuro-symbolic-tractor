// nst: the one program an AI session may run.
// Composition root: the only place that knows which adapter fills which port.
using System.Text;
using Nst.Application;

namespace Nst;

static class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        var result = Compose().Run(args);
        foreach (var line in result.Out) Console.WriteLine(line);
        foreach (var line in result.Err) Console.Error.WriteLine(line);
        return result.ExitCode;
    }

    // Every action nst has, each given its adapters. The base has none yet.
    static ActionTable Compose() => new([]);
}
