using Nordform.Sim.Core;

namespace Nordform.Sim.Core.Tests;

public class EngineTests
{
    [Fact]
    public void Same_seed_and_strategy_gives_identical_metrics()
    {
        var a = TestData.Engine();
        var b = TestData.Engine();
        a.RunFor(900);
        b.RunFor(900);

        Assert.Equal(a.GetMetrics(), b.GetMetrics());
    }

    [Fact]
    public void Different_seed_changes_the_outcome()
    {
        var layout = TestData.FurnitureLayout();
        var products = TestData.FurnitureProducts();
        var scenario = TestData.LoadScenario();

        var a = new SimulationEngine(layout, products, scenario, new NearestStrategy());
        var b = new SimulationEngine(layout, products, scenario with { Seed = scenario.Seed + 1 }, new NearestStrategy());
        a.RunFor(900);
        b.RunFor(900);

        Assert.NotEqual(a.GetMetrics(), b.GetMetrics());
    }

    [Fact]
    public void Produces_sensible_metrics_over_half_an_hour()
    {
        var engine = TestData.Engine();
        engine.RunFor(1800);
        var m = engine.GetMetrics();

        Assert.True(m.Shipped > 0, "Minst en order ska hinna levereras");
        Assert.True(m.CompletedTransports > 0);
        Assert.InRange(m.EmptyPercent, 0.0, 100.0);
        Assert.InRange(m.UtilizationPercent, 0.0, 100.0);
        Assert.True(m.DistanceMeters > 0);
        Assert.True(m.AverageWaitSeconds >= 0);
    }

    [Fact]
    public void Every_started_order_is_either_active_or_shipped_and_wip_adds_up()
    {
        var engine = TestData.Engine(scenarioFile: "high-load.json");

        for (var i = 0; i < 6000; i++)
        {
            engine.Step();
            if (i % 10 != 0) continue;

            Assert.Equal(engine.OrdersStarted, engine.ActivePallets + engine.Shipped);
            Assert.All(engine.WipByStation.Values, w => Assert.True(w >= 0));

            var carried = engine.Trucks.Count(t => t.State is TruckState.ToDrop or TruckState.Unloading);
            Assert.Equal(engine.ActivePallets, engine.WipByStation.Values.Sum() + carried);
        }
    }

    [Fact]
    public void No_two_trucks_share_a_job()
    {
        var engine = TestData.Engine(scenarioFile: "high-load.json");

        for (var i = 0; i < 6000; i++)
        {
            engine.Step();
            var ids = engine.Trucks.Where(t => t.JobId is not null).Select(t => t.JobId!.Value).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }

    [Theory]
    [InlineData(typeof(FifoStrategy))]
    [InlineData(typeof(NearestStrategy))]
    [InlineData(typeof(UrgentThenNearestStrategy))]
    public void Every_strategy_delivers_orders(Type strategyType)
    {
        var strategy = (IDispatchStrategy)Activator.CreateInstance(strategyType)!;
        var engine = TestData.Engine(strategy, "high-load.json");
        engine.RunFor(1800);

        Assert.True(engine.Shipped > 0, $"{strategy.Name} levererade inga order");
    }

    [Fact]
    public void Blocked_corridor_still_delivers_via_detour_and_changes_the_distance()
    {
        var open = TestData.Engine();
        var blocked = TestData.Engine();
        blocked.SetCorridorBlocked("J2", "CNC", true);

        open.RunFor(1800);
        blocked.RunFor(1800);

        Assert.True(blocked.Shipped > 0, "Omvägen ska göra att order ändå levereras");
        Assert.NotEqual(open.GetMetrics().DistanceMeters, blocked.GetMetrics().DistanceMeters);
    }

    [Fact]
    public void Missing_station_kind_is_reported_clearly()
    {
        var layout = TestData.FurnitureLayout();
        var withoutPaint = layout with { Stations = layout.Stations.Where(s => s.Kind != "paint").ToList() };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SimulationEngine(withoutPaint, TestData.FurnitureProducts(), TestData.LoadScenario(), new FifoStrategy()));

        Assert.Contains("paint", ex.Message);
    }

    [Fact]
    public void Events_are_logged_for_pickups_and_drop_offs()
    {
        var engine = TestData.Engine();
        var events = new List<string>();
        engine.EventLogged += (_, text) => events.Add(text);

        engine.RunFor(600);

        Assert.Contains(events, e => e.Contains("hämtar"));
        Assert.Contains(events, e => e.Contains("lämnar"));
    }
}
