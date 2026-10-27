namespace Nordform.Sim.Core.Tests;

/// <summary>En rad i data/regression-baseline.json: mätvärden sparade för ett scenario och en
/// strategi vid en känd, tidigare granskad körning.</summary>
public sealed record RegressionBaseline(
    string Scenario,
    string Strategy,
    int Shipped,
    int CompletedTransports,
    double EmptyPercent,
    double AverageWaitSeconds);

/// <summary>Larmar om en ändring i motorn gör planeringen sämre än den baslinje som sparades i
/// data/regression-baseline.json (NF-032). Baslinjen uppdateras medvetet (ny körning, nya tal i
/// filen) när en ändring avsiktligt förändrar beteendet, till exempel en ny eller justerad strategi.</summary>
public class RegressionBaselineTests
{
    private static readonly Dictionary<string, Func<IDispatchStrategy>> StrategyFactories = new()
    {
        ["Fifo"] = () => new FifoStrategy(),
        ["Nearest"] = () => new NearestStrategy(),
        ["UrgentThenNearest"] = () => new UrgentThenNearestStrategy(),
    };

    public static IEnumerable<object[]> Baselines() =>
        JsonData.Load<List<RegressionBaseline>>(TestData.DataPath("regression-baseline.json"))
            .Select(b => new object[] { b });

    [Theory]
    [MemberData(nameof(Baselines))]
    public void Metrics_have_not_regressed_since_the_baseline_was_recorded(RegressionBaseline baseline)
    {
        var scenario = TestData.LoadScenario(baseline.Scenario + ".json");
        var strategy = StrategyFactories[baseline.Strategy]();
        var engine = new SimulationEngine(TestData.FurnitureLayout(), TestData.FurnitureProducts(), scenario, strategy);

        engine.RunFor(scenario.DurationSeconds);
        var m = engine.GetMetrics();

        const double tolerance = 0.01; // 1 %, skydd mot flyttalsavrundning, inte en avsiktlig lättnad

        Assert.True(m.Shipped >= baseline.Shipped,
            $"{baseline.Scenario}/{baseline.Strategy}: färre levererade order, {m.Shipped} mot baslinjens {baseline.Shipped}");
        Assert.True(m.CompletedTransports >= baseline.CompletedTransports * (1 - tolerance),
            $"{baseline.Scenario}/{baseline.Strategy}: färre transporter, {m.CompletedTransports} mot baslinjens {baseline.CompletedTransports}");
        Assert.True(m.AverageWaitSeconds <= baseline.AverageWaitSeconds * (1 + tolerance) + 0.01,
            $"{baseline.Scenario}/{baseline.Strategy}: längre snittväntan, {m.AverageWaitSeconds:F2} s mot baslinjens {baseline.AverageWaitSeconds:F2} s");
        Assert.True(m.EmptyPercent <= baseline.EmptyPercent * (1 + tolerance) + 0.01,
            $"{baseline.Scenario}/{baseline.Strategy}: mer tomkörning, {m.EmptyPercent:F2}% mot baslinjens {baseline.EmptyPercent:F2}%");
    }
}
