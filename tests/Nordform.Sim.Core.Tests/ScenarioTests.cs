namespace Nordform.Sim.Core.Tests;

/// <summary>Körer varje situation i data/scenario-tests/*.json och kontrollerar att motorns
/// resultat matchar den förväntade utgången. Nya situationer läggs till som en ny JSON-fil,
/// ingen ny kod behövs.</summary>
public class ScenarioTests
{
    public static IEnumerable<object[]> Cases() =>
        Directory.GetFiles(TestData.DataPath("scenario-tests"), "*.json")
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => new object[] { Path.GetFileName(f) });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Situation_matches_expected_behavior(string fileName)
    {
        var testCase = JsonData.Load<ScenarioTestCase>(TestData.DataPath("scenario-tests", fileName));

        var layout = JsonData.Load<Layout>(TestData.DataPath("layouts", testCase.Layout + ".json"));
        var products = JsonData.Load<List<Product>>(TestData.DataPath("products", testCase.Products + ".json"));
        var scenario = JsonData.Load<Scenario>(TestData.DataPath("scenarios", testCase.Scenario + ".json"));

        var engine = new SimulationEngine(layout, products, scenario, new NearestStrategy());

        foreach (var pair in testCase.BlockedCorridors ?? Array.Empty<IReadOnlyList<string>>())
            engine.SetCorridorBlocked(pair[0], pair[1], true);
        foreach (var id in testCase.DownStations ?? Array.Empty<string>())
            engine.SetStationDown(id, true);
        foreach (var id in testCase.BrokenTrucks ?? Array.Empty<int>())
            engine.SetTruckBroken(id, true);

        engine.RunFor(testCase.DurationSeconds);
        var m = engine.GetMetrics();

        if (testCase.Expect.MinShipped is { } minShipped)
            Assert.True(m.Shipped >= minShipped,
                $"{testCase.Name}: väntade minst {minShipped} levererade, fick {m.Shipped}");
        if (testCase.Expect.MaxShipped is { } maxShipped)
            Assert.True(m.Shipped <= maxShipped,
                $"{testCase.Name}: väntade högst {maxShipped} levererade, fick {m.Shipped}");
        if (testCase.Expect.MinCompletedTransports is { } minTransports)
            Assert.True(m.CompletedTransports >= minTransports,
                $"{testCase.Name}: väntade minst {minTransports} transporter, fick {m.CompletedTransports}");
    }
}
