using Microsoft.AspNetCore.SignalR;
using Nordform.Sim.Core;

namespace Nordform.Sim.Api;

/// <summary>Ett ögonblick av en körande simulering, skickat till klienter som gått med i sessionen.</summary>
public sealed record SimulationSnapshot(double Time, IReadOnlyList<TruckSnapshot> Trucks, SimulationMetrics Metrics);

/// <summary>Skickar simuleringstillstånd till klienter. Ett eget gränssnitt (inte bara
/// <see cref="IHubContext{SimulationHub}"/> direkt) gör det möjligt att testa
/// <see cref="SimulationSessionManager"/> utan en riktig SignalR-anslutning.</summary>
public interface ISimulationBroadcaster
{
    Task StateUpdatedAsync(string sessionId, SimulationSnapshot snapshot);
    Task SessionEndedAsync(string sessionId);
}

public sealed class SignalRSimulationBroadcaster(IHubContext<SimulationHub> hub) : ISimulationBroadcaster
{
    public Task StateUpdatedAsync(string sessionId, SimulationSnapshot snapshot) =>
        hub.Clients.Group(sessionId).SendAsync("state", snapshot);

    public Task SessionEndedAsync(string sessionId) =>
        hub.Clients.Group(sessionId).SendAsync("ended");
}
