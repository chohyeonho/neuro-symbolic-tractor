using System.Reflection;
using Nst.Application;
using Nst.Domain;
using Nst.Infrastructure;

namespace Nst.Tests;

// The layers only point inward: nst (composition) -> Infrastructure -> Application -> Domain.
// Read from the built assemblies, so a reference that is added and used anywhere is caught.
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
}
