using System.Reflection;

namespace Tactics.Core.Tests;

/// <summary>
/// Guards architecture rule 2: Tactics.Core is pure and depends on the BCL only.
/// If someone adds a NuGet package or a project reference to Core, this test fails.
/// </summary>
public sealed class CoreAssemblyTests
{
    [Fact]
    public void Core_references_only_base_class_library_assemblies()
    {
        var core = Assembly.Load("Tactics.Core");

        var nonBcl = core.GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .Where(name => !IsBcl(name))
            .ToList();

        Assert.Empty(nonBcl);
    }

    // BCL assembly names: System, System.*, plus the legacy facades.
    private static bool IsBcl(string name) =>
        name is "System" or "netstandard" or "mscorlib"
        || name.StartsWith("System.", StringComparison.Ordinal);
}
