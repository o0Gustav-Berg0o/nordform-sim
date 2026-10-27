using Nordform.Sim.Core;

namespace Nordform.Sim.Core.Tests;

public class PathFinderTests
{
    [Fact]
    public void Shortest_path_takes_the_straight_corridor()
    {
        var pf = new PathFinder(TestData.SmallLayout());
        var result = pf.FindPath("A", "C");

        Assert.NotNull(result);
        Assert.Equal(new[] { "A", "B", "C" }, result!.Nodes);
        Assert.Equal(20, result.Distance, 3);
    }

    [Fact]
    public void Blocked_corridor_forces_detour()
    {
        var pf = new PathFinder(TestData.SmallLayout());
        pf.SetBlocked("B", "C", true);

        var result = pf.FindPath("A", "C");

        Assert.NotNull(result);
        Assert.Equal(new[] { "A", "D", "C" }, result!.Nodes);
        Assert.Equal(2 * Math.Sqrt(200), result.Distance, 3);
    }

    [Fact]
    public void Unreachable_node_gives_null()
    {
        var pf = new PathFinder(TestData.SmallLayout());
        pf.SetBlocked("B", "C", true);
        pf.SetBlocked("D", "C", true);

        Assert.Null(pf.Distance("A", "C"));
    }

    [Fact]
    public void Unknown_node_gives_null()
    {
        var pf = new PathFinder(TestData.SmallLayout());
        Assert.Null(pf.Distance("A", "Z"));
    }
}
