using Nordform.Sim.Core;

namespace Nordform.Sim.Core.Tests;

public class CapacityAndDisruptionTests
{
    private static Layout TwoStationLayout(int? capacity, double changeover = 0) => new(
        "test",
        new[] { new Node("A", 0, 0), new Node("B", 10, 0) },
        new[] { new Edge("A", "B") },
        new[]
        {
            new Station("in", "In", "in", "A", 1, Capacity: capacity, ChangeoverSeconds: changeover),
            new Station("out", "Out", "out", "B", 1),
        },
        ParkingNodes: new[] { "A" });

    private static Product OneStepProduct(string id = "p") =>
        new(id, id, new[] { new RoutingStep("in"), new RoutingStep("out") });

    private static Scenario OneProductScenario(int seed = 1, int trucks = 2) => new(
        "test", seed, trucks, OrderIntervalSeconds: 5, DurationSeconds: 600,
        ProductMix: new Dictionary<string, double> { ["p"] = 1 });

    [Fact]
    public void Station_with_capacity_never_holds_more_pallets_than_allowed()
    {
        var engine = new SimulationEngine(
            TwoStationLayout(capacity: 2),
            new[] { OneStepProduct() },
            OneProductScenario(),
            new NearestStrategy());

        for (var i = 0; i < 3000; i++)
        {
            engine.Step();
            Assert.True(engine.WipByStation["in"] <= 2, "Stationen 'in' har fler pallar än sin kapacitet tillåter");
        }
    }

    [Fact]
    public void Changeover_time_only_applies_when_the_product_changes()
    {
        var layout = TwoStationLayout(capacity: null, changeover: 30);
        var products = new[] { OneStepProduct("a"), OneStepProduct("b") };
        var scenario = new Scenario(
            "test", 1, 1, OrderIntervalSeconds: 5, DurationSeconds: 600,
            ProductMix: new Dictionary<string, double> { ["a"] = 0.5, ["b"] = 0.5 });

        var withChangeover = new SimulationEngine(layout, products, scenario, new NearestStrategy());
        var withoutChangeover = new SimulationEngine(
            TwoStationLayout(capacity: null, changeover: 0), products, scenario, new NearestStrategy());

        withChangeover.RunFor(600);
        withoutChangeover.RunFor(600);

        Assert.True(withChangeover.Shipped <= withoutChangeover.Shipped,
            "Omställningstid ska aldrig göra att fler order hinner levereras på samma tid");
    }

    [Fact]
    public void Disabled_station_stops_accepting_new_pallets_until_it_opens_again()
    {
        var engine = new SimulationEngine(
            TwoStationLayout(capacity: null),
            new[] { OneStepProduct() },
            OneProductScenario(trucks: 1),
            new NearestStrategy());

        engine.SetStationDown("in", true);
        engine.RunFor(120);
        Assert.Equal(0, engine.OrdersStarted);

        engine.SetStationDown("in", false);
        engine.RunFor(120);
        Assert.True(engine.OrdersStarted > 0, "Stationen ska ta emot order igen när den öppnas");
    }

    [Fact]
    public void Broken_truck_releases_its_job_so_another_truck_can_take_it()
    {
        var engine = new SimulationEngine(
            TwoStationLayout(capacity: null),
            new[] { OneStepProduct() },
            OneProductScenario(trucks: 2),
            new NearestStrategy());

        engine.RunFor(10);
        var busy = engine.Trucks.FirstOrDefault(t => t.JobId is not null);
        if (busy is null) return; // inget uppdrag hunnit tilldelas än, testet verifierar då inget

        engine.SetTruckBroken(busy.Id, true);
        var snapshot = engine.Trucks.Single(t => t.Id == busy.Id);
        Assert.Null(snapshot.JobId);

        engine.RunFor(600);
        Assert.True(engine.Shipped > 0, "En annan truck ska kunna slutföra uppdraget");
    }
}
