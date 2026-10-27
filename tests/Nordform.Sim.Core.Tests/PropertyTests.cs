using FsCheck.Xunit;

namespace Nordform.Sim.Core.Tests;

/// <summary>Egenskapstester (NF-031): kontrollerar att motorns invarianter håller för
/// slumpmässigt genererade seeds och strategier, inte bara för de fasta exempel som
/// EngineTests använder.</summary>
public class PropertyTests
{
    private static readonly IDispatchStrategy[] Strategies =
        { new FifoStrategy(), new NearestStrategy(), new UrgentThenNearestStrategy() };

    private static SimulationEngine MakeEngine(int seed, int strategyPick)
    {
        var strategy = Strategies[((strategyPick % Strategies.Length) + Strategies.Length) % Strategies.Length];
        return new SimulationEngine(
            TestData.FurnitureLayout(),
            TestData.FurnitureProducts(),
            TestData.LoadScenario() with { Seed = seed },
            strategy);
    }

    [Property(MaxTest = 15)]
    public void No_truck_ever_holds_two_jobs_at_once(int seed, int strategyPick)
    {
        var engine = MakeEngine(seed, strategyPick);
        for (var i = 0; i < 2000; i++)
        {
            engine.Step();
            var jobIds = engine.Trucks.Where(t => t.JobId is not null).Select(t => t.JobId!.Value).ToList();
            Assert.Equal(jobIds.Count, jobIds.Distinct().Count());
        }
    }

    [Property(MaxTest = 15)]
    public void Started_orders_always_equal_active_plus_shipped(int seed, int strategyPick)
    {
        var engine = MakeEngine(seed, strategyPick);
        for (var i = 0; i < 2000; i++)
        {
            engine.Step();
            Assert.Equal(engine.OrdersStarted, engine.ActivePallets + engine.Shipped);
        }
    }

    [Property(MaxTest = 15)]
    public void Wip_is_never_negative(int seed, int strategyPick)
    {
        var engine = MakeEngine(seed, strategyPick);
        for (var i = 0; i < 2000; i++)
        {
            engine.Step();
            Assert.All(engine.WipByStation.Values, w => Assert.True(w >= 0));
        }
    }

    [Property(MaxTest = 20)]
    public void Metrics_stay_within_sensible_bounds(int seed, int strategyPick)
    {
        var engine = MakeEngine(seed, strategyPick);
        engine.RunFor(1800);
        var m = engine.GetMetrics();

        Assert.InRange(m.EmptyPercent, 0.0, 100.0);
        Assert.InRange(m.UtilizationPercent, 0.0, 100.0);
        Assert.True(m.DistanceMeters >= 0);
        Assert.True(m.AverageWaitSeconds >= 0);
        Assert.True(m.Shipped >= 0);
    }
}
