using System.Collections.Concurrent;
using Nordform.Sim.Core;

namespace Nordform.Sim.Api;

/// <summary>Håller körande simuleringar och strömmar deras tillstånd via <see cref="ISimulationBroadcaster"/>
/// (NF-050). Varje session kör i sin egen bakgrundstask tills scenariots varaktighet är nådd eller
/// den stoppas, och skickar ett nytt ögonblick med ett fast mellanrum i realtid.</summary>
public sealed class SimulationSessionManager(ISimulationBroadcaster broadcaster)
{
    private sealed class Session
    {
        public required string Id;
        public required SimulationEngine Engine;
        public required Scenario Scenario;
        public readonly CancellationTokenSource Cts = new();
    }

    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    /// <summary>Startar en ny session. <paramref name="speed"/> är hur många simulerade sekunder
    /// som körs per real sekund, <paramref name="tickSeconds"/> hur ofta ett nytt ögonblick skickas.</summary>
    public string Start(
        Layout layout,
        IReadOnlyList<Product> products,
        Scenario scenario,
        IDispatchStrategy strategy,
        double speed = 10,
        double tickSeconds = 0.5)
    {
        var session = new Session
        {
            Id = Guid.NewGuid().ToString("n"),
            Engine = new SimulationEngine(layout, products, scenario, strategy),
            Scenario = scenario,
        };
        _sessions[session.Id] = session;

        _ = Task.Run(() => RunAsync(session, speed, tickSeconds));
        return session.Id;
    }

    public bool Stop(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return false;
        session.Cts.Cancel();
        return true;
    }

    public SimulationSnapshot? GetSnapshot(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session) ? Snapshot(session.Engine) : null;

    private async Task RunAsync(Session session, double speed, double tickSeconds)
    {
        try
        {
            while (!session.Cts.IsCancellationRequested && session.Engine.Time < session.Scenario.DurationSeconds)
            {
                session.Engine.RunFor(tickSeconds * speed);
                await broadcaster.StateUpdatedAsync(session.Id, Snapshot(session.Engine));
                await Task.Delay(TimeSpan.FromSeconds(tickSeconds), session.Cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Sessionen stoppades, inget att larma om.
        }
        finally
        {
            _sessions.TryRemove(session.Id, out _);
            await broadcaster.SessionEndedAsync(session.Id);
        }
    }

    private static SimulationSnapshot Snapshot(SimulationEngine engine) =>
        new(engine.Time, engine.Trucks, engine.GetMetrics());
}
