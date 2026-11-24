// nst: the one program an AI session may run.
// Composition root: the only place that knows which adapter fills which port.
using System.Text;
using Nst.Application;
using Nst.Application.Actions;
using Nst.Infrastructure;

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

    // Every action nst has, each given its adapters.
    static ActionTable Compose() => new([
        new LinesAction(new FileSystemTextFiles()),
    ]);
}
