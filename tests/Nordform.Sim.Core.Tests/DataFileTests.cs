using Nordform.Sim.Core;

namespace Nordform.Sim.Core.Tests;

/// <summary>Kontrollerar att dataformaten går att läsa och hänger ihop.</summary>
public class DataFileTests
{
    private static string Data(params string[] parts) =>
        Path.Combine(new[] { TestData.RepoRoot(), "data" }.Concat(parts).ToArray());

    [Fact]
    public void Furniture_layout_loads_and_is_connected()
    {
        var layout = JsonData.Load<Layout>(Data("layouts", "furniture-line.json"));
        var paths = new PathFinder(layout);

        Assert.Equal(13, layout.Stations.Count);
        foreach (var station in layout.Stations)
            Assert.NotNull(paths.Distance("GODS", station.Node));
    }

    [Fact]
    public void Every_routing_step_has_a_matching_station_kind()
    {
        var layout = JsonData.Load<Layout>(Data("layouts", "furniture-line.json"));
        var products = JsonData.Load<List<Product>>(Data("products", "furniture-products.json"));
        var kinds = layout.Stations.Select(s => s.Kind).ToHashSet();

        foreach (var product in products)
            foreach (var step in product.Routing)
                Assert.Contains(step.StationKind, kinds);
    }

    [Fact]
    public void Scenario_product_mix_refers_to_existing_products()
    {
        var products = JsonData.Load<List<Product>>(Data("products", "furniture-products.json"));
        var ids = products.Select(p => p.Id).ToHashSet();

        foreach (var file in Directory.GetFiles(Data("scenarios"), "*.json"))
        {
            var scenario = JsonData.Load<Scenario>(file);
            foreach (var id in scenario.ProductMix.Keys)
                Assert.Contains(id, ids);
            Assert.Equal(1.0, scenario.ProductMix.Values.Sum(), 3);
        }
    }
}
