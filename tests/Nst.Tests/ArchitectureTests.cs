using System.Reflection;
using System.Xml.Linq;
using Nst.Application;
using Nst.Domain;
using Nst.Infrastructure;

namespace Nst.Tests;

// The layers only point inward: nst (composition) -> Infrastructure -> Application -> Domain.
// Checked twice: in the built assemblies (a reference that is used), and in the project files
// (a reference that is declared, even while nothing uses it yet).
public class ArchitectureTests
{
    static readonly string[] Layers = ["Nst.Domain", "Nst.Application", "Nst.Infrastructure", "nst"];

    static Assembly Of(string name) => name switch
    {
        "Nst.Domain" => typeof(DomainLayer).Assembly,
        "Nst.Application" => typeof(ActionTable).Assembly,
        "Nst.Infrastructure" => typeof(InfrastructureLayer).Assembly,
        _ => Assembly.Load(name),
    };

    [Theory]
    [InlineData("Nst.Domain")]
    [InlineData("Nst.Application")]
    [InlineData("Nst.Infrastructure")]
    [InlineData("nst")]
    public void Each_layer_uses_only_layers_inside_it(string layer)
    {
        var inner = Layers.TakeWhile(l => l != layer).ToHashSet();
        var used = Of(layer).GetReferencedAssemblies().Select(a => a.Name!).Where(Layers.Contains);
        Assert.All(used, name => Assert.Contains(name, inner));
    }

    [Theory]
    [InlineData("Nst.Domain")]
    [InlineData("Nst.Application")]
    [InlineData("Nst.Infrastructure")]
    [InlineData("nst")]
    public void Each_project_declares_only_layers_inside_it(string layer)
    {
        var inner = Layers.TakeWhile(l => l != layer).ToHashSet();
        var project = Path.Combine(RepositoryRoot(), "src", layer, layer + ".csproj");
        var declared = XDocument.Load(project).Descendants("ProjectReference")
            .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/')))
            .ToList();
        Assert.All(declared, name => Assert.Contains(name, inner));
    }

    static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "nst.slnx"))) return dir.FullName;
        throw new DirectoryNotFoundException("nst.slnx was not found above the test folder.");
    }
}
