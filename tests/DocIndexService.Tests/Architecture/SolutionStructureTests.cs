using System.Reflection;

namespace DocIndexService.Tests.Architecture;

public class SolutionStructureTests
{
    [Fact]
    public void CoreAssembly_ShouldBeLoadable()
    {
        var assembly = Assembly.Load("DocIndexService.Core");

        Assert.NotNull(assembly);
    }

    [Fact]
    public void ApplicationAssembly_ShouldReferenceCoreAssembly()
    {
        var assembly = Assembly.Load("DocIndexService.Application");
        var refs = assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.Contains("DocIndexService.Core", refs);
    }
}
