namespace Nordform.Sim.Core;

/// <summary>Punkt i korridornätet. Koordinater i layoutens enhet (se MetersPerUnit).</summary>
public sealed record Node(string Id, double X, double Y);

/// <summary>Korridorsegment mellan två noder. Kan vara blockerat.</summary>
public sealed record Edge(string From, string To, bool Blocked = false);

/// <summary>Station med en dockningsnod där pallar hämtas och lämnas.</summary>
public sealed record Station(string Id, string Name, string Kind, string Node, double ProcessSeconds);

public sealed record Layout(
    string Name,
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Edge> Edges,
    IReadOnlyList<Station> Stations,
    double MetersPerUnit = 0.1);

/// <summary>Ett steg i en arbetsplan. Anges per stationstyp, inte per enskild station.</summary>
public sealed record RoutingStep(string StationKind, double? ProcessSeconds = null);

public sealed record Product(string Id, string Name, IReadOnlyList<RoutingStep> Routing);

public sealed record Scenario(
    string Name,
    int Seed,
    int Trucks,
    double OrderIntervalSeconds,
    double DurationSeconds,
    IReadOnlyDictionary<string, double> ProductMix);

public sealed record TransportJob(int Id, string FromNode, string ToNode, double CreatedAt, bool Urgent);

public sealed record TruckView(int Id, string Node);

public sealed record PathResult(IReadOnlyList<string> Nodes, double Distance);
