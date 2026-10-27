using Microsoft.AspNetCore.SignalR;

namespace Nordform.Sim.Api;

/// <summary>Klienter ansluter hit och går med i en grupp per simuleringssession för att få
/// tillståndet strömmat (NF-050). Själva simuleringen körs av <see cref="SimulationSessionManager"/>.</summary>
public sealed class SimulationHub : Hub
{
    public Task JoinSession(string sessionId) => Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

    public Task LeaveSession(string sessionId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
}
