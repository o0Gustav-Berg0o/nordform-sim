namespace Nordform.Sim.Core.Tests;

/// <summary>En situation beskriven i JSON (data/scenario-tests/*.json): vilken layout, produkter
/// och scenario som ska köras, vilka störningar som ska läggas på, och vad som förväntas hända.</summary>
public sealed record ScenarioTestCase(
    string Name,
    string Layout,
    string Products,
    string Scenario,
    double DurationSeconds,
    IReadOnlyList<IReadOnlyList<string>>? BlockedCorridors,
    IReadOnlyList<string>? DownStations,
    IReadOnlyList<int>? BrokenTrucks,
    Expectation Expect);

public sealed record Expectation(
    int? MinShipped = null,
    int? MaxShipped = null,
    int? MinCompletedTransports = null);
