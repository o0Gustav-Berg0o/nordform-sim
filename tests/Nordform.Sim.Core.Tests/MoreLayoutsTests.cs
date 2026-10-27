namespace Nordform.Sim.Core.Tests;

/// <summary>NF-070: fler kartor, utöver den ursprungliga `furniture-line`. Alla ska gå att läsa,
/// vara sammanhängande, och gå att köra med samma produkter och scenarier som `furniture-line`
/// eftersom de täcker samma uppsättning stationstyper.</summary>
public class MoreLayoutsTests
{
    public static IEnumerable<object[]> LayoutFiles() =>
        new[] { "cell-based", "job-shop", "hub-and-spokes", "bad-layout" }
            .Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(LayoutFiles))]
    public void Layout_loads_and_every_station_is_reachable_from_the_first_node(string name)
    {
        var layout = JsonData.Load<Layout>(TestData.DataPath("layouts", name + ".json"));
        var paths = new PathFinder(layout);

        Assert.True(layout.Stations.Count > 0);
        foreach (var station in layout.Stations)
            Assert.NotNull(paths.Distance(layout.Nodes[0].Id, station.Node));
    }

    [Theory]
    [MemberData(nameof(LayoutFiles))]
    public void Layout_covers_every_station_kind_the_furniture_products_need(string name)
    {
        var layout = JsonData.Load<Layout>(TestData.DataPath("layouts", name + ".json"));
        var products = TestData.FurnitureProducts();
        var kinds = layout.Stations.Select(s => s.Kind).ToHashSet();

        foreach (var product in products)
            foreach (var step in product.Routing)
                Assert.Contains(step.StationKind, kinds);
    }

    [Theory]
    [MemberData(nameof(LayoutFiles))]
    public void Layout_delivers_orders_with_the_normal_day_scenario(string name)
    {
        var layout = JsonData.Load<Layout>(TestData.DataPath("layouts", name + ".json"));
        var engine = new SimulationEngine(layout, TestData.FurnitureProducts(), TestData.LoadScenario(), new NearestStrategy());

        engine.RunFor(1800);

        Assert.True(engine.Shipped > 0, $"{name}: inga order levererades på 30 minuter");
    }
}
