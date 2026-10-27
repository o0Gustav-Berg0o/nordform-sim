namespace Nordform.Sim.Core;

/// <summary>
/// Planeringsmetod. Givet en ledig truck och väntande uppdrag väljs vilket uppdrag
/// trucken ska ta, eller null om inget går att nå. Studenternas moduler implementerar detta.
/// </summary>
public interface IDispatchStrategy
{
    string Name { get; }
    TransportJob? Select(TruckView truck, IReadOnlyList<TransportJob> waiting, IPathCost paths, double now);
}

public sealed class FifoStrategy : IDispatchStrategy
{
    public string Name => "Äldsta först";

    public TransportJob? Select(TruckView truck, IReadOnlyList<TransportJob> waiting, IPathCost paths, double now)
    {
        TransportJob? best = null;
        foreach (var job in waiting)
        {
            if (paths.Distance(truck.Node, job.FromNode) is null) continue;
            if (best is null || job.CreatedAt < best.CreatedAt) best = job;
        }
        return best;
    }
}

public sealed class NearestStrategy : IDispatchStrategy
{
    public string Name => "Närmaste uppdrag";

    public TransportJob? Select(TruckView truck, IReadOnlyList<TransportJob> waiting, IPathCost paths, double now)
    {
        TransportJob? best = null;
        var bestDist = double.MaxValue;
        foreach (var job in waiting)
        {
            var d = paths.Distance(truck.Node, job.FromNode);
            if (d is null) continue;
            if (d.Value < bestDist) { best = job; bestDist = d.Value; }
        }
        return best;
    }
}

public sealed class UrgentThenNearestStrategy : IDispatchStrategy
{
    public string Name => "Brådskande först, sedan närmast";

    public TransportJob? Select(TruckView truck, IReadOnlyList<TransportJob> waiting, IPathCost paths, double now)
    {
        TransportJob? best = null;
        var bestRank = int.MaxValue;
        var bestDist = double.MaxValue;
        foreach (var job in waiting)
        {
            var d = paths.Distance(truck.Node, job.FromNode);
            if (d is null) continue;
            var rank = job.Urgent ? 0 : 1;
            if (rank < bestRank || (rank == bestRank && d.Value < bestDist))
            {
                best = job; bestRank = rank; bestDist = d.Value;
            }
        }
        return best;
    }
}
