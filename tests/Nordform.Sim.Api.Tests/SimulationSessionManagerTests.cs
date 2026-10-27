using System.Collections.Concurrent;
using Nordform.Sim.Core;

namespace Nordform.Sim.Api.Tests;

/// <summary>Fångar upp vad en session skickar, utan en riktig SignalR-anslutning.</summary>
internal sealed class FakeBroadcaster : ISimulationBroadcaster
{
    public readonly ConcurrentBag<(string SessionId, SimulationSnapshot Snapshot)> Updates = new();
    public readonly ConcurrentBag<string> Ended = new();

    public Task StateUpdatedAsync(string sessionId, SimulationSnapshot snapshot)
    {
        Updates.Add((sessionId, snapshot));
        return Task.CompletedTask;
    }

    public Task SessionEndedAsync(string sessionId)
    {
        Ended.Add(sessionId);
        return Task.CompletedTask;
    }
}

public class SimulationSessionManagerTests
{
    private static Layout TinyLayout() => new(
        "test",
        new[] { new Node("A", 0, 0), new Node("B", 10, 0) },
        new[] { new Edge("A", "B") },
        new[]
        {
            new Station("in", "In", "in", "A", 1),
            new Station("out", "Out", "out", "B", 1),
        },
        ParkingNodes: new[] { "A" });

    private static List<Product> TinyProducts() =>
        new() { new Product("p", "p", new[] { new RoutingStep("in"), new RoutingStep("out") }) };

    private static Scenario TinyScenario(double durationSeconds) => new(
        "test", 1, 1, OrderIntervalSeconds: 2, DurationSeconds: durationSeconds,
        ProductMix: new Dictionary<string, double> { ["p"] = 1 });

    [Fact]
    public async Task Session_streams_state_until_the_scenario_duration_is_reached()
    {
        var broadcaster = new FakeBroadcaster();
        var manager = new SimulationSessionManager(broadcaster);

        var id = manager.Start(TinyLayout(), TinyProducts(), TinyScenario(2), new NearestStrategy(),
            speed: 100, tickSeconds: 0.05);

        await WaitUntil(() => broadcaster.Ended.Contains(id), TimeSpan.FromSeconds(5));

        Assert.True(broadcaster.Updates.Count(u => u.SessionId == id) > 0, "Inga tillståndsuppdateringar skickades");
        Assert.Contains(id, broadcaster.Ended);
        Assert.Null(manager.GetSnapshot(id));
    }

    [Fact]
    public async Task Stopping_a_session_ends_it_early()
    {
        var broadcaster = new FakeBroadcaster();
        var manager = new SimulationSessionManager(broadcaster);

        var id = manager.Start(TinyLayout(), TinyProducts(), TinyScenario(600), new NearestStrategy(),
            speed: 1, tickSeconds: 0.05);

        Assert.NotNull(manager.GetSnapshot(id));
        Assert.True(manager.Stop(id));

        await WaitUntil(() => broadcaster.Ended.Contains(id), TimeSpan.FromSeconds(5));
        Assert.Null(manager.GetSnapshot(id));
    }

    [Fact]
    public void Stopping_an_unknown_session_returns_false()
    {
        var manager = new SimulationSessionManager(new FakeBroadcaster());
        Assert.False(manager.Stop("does-not-exist"));
    }

    private static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(20);
        Assert.True(condition(), "Villkoret uppfylldes inte inom tidsgränsen");
    }
}
