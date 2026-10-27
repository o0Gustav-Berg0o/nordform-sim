using Nordform.Sim.Core;

namespace Nordform.Sim.Core.Tests;

internal static class TestData
{
    /// <summary>Liten graf: A-B-C på rad, plus omväg A-D-C. Rak väg A till C är 20 enheter.</summary>
    public static Layout SmallLayout() => new(
        "test",
        new[] { new Node("A", 0, 0), new Node("B", 10, 0), new Node("C", 20, 0), new Node("D", 10, 10) },
        new[] { new Edge("A", "B"), new Edge("B", "C"), new Edge("A", "D"), new Edge("D", "C") },
        Array.Empty<Station>());

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Nordform.Sim.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Hittar inte repots rot");
    }
}
