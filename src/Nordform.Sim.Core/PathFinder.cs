namespace Nordform.Sim.Core;

public interface IPathCost
{
    /// <summary>Kortaste avstånd i layoutens enhet, eller null om noderna inte går att nå.</summary>
    double? Distance(string from, string to);
}

/// <summary>Dijkstra över korridornätet. Blockerade segment hoppas över.</summary>
public sealed class PathFinder : IPathCost
{
    private readonly Dictionary<string, Node> _nodes;
    private readonly Dictionary<string, List<(string To, double Length, string Key)>> _adj = new();
    private readonly HashSet<string> _blocked = new();

    public PathFinder(Layout layout)
    {
        _nodes = layout.Nodes.ToDictionary(n => n.Id);
        foreach (var n in layout.Nodes) _adj[n.Id] = new();
        foreach (var e in layout.Edges)
        {
            var a = _nodes[e.From];
            var b = _nodes[e.To];
            var len = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            var key = EdgeKey(e.From, e.To);
            _adj[e.From].Add((e.To, len, key));
            _adj[e.To].Add((e.From, len, key));
            if (e.Blocked) _blocked.Add(key);
        }
    }

    public static string EdgeKey(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? $"{a}~{b}" : $"{b}~{a}";

    public void SetBlocked(string a, string b, bool blocked)
    {
        var key = EdgeKey(a, b);
        if (blocked) _blocked.Add(key);
        else _blocked.Remove(key);
    }

    public double? Distance(string from, string to) => FindPath(from, to)?.Distance;

    public PathResult? FindPath(string from, string to)
    {
        if (!_nodes.ContainsKey(from) || !_nodes.ContainsKey(to)) return null;

        var dist = new Dictionary<string, double> { [from] = 0 };
        var prev = new Dictionary<string, string>();
        var done = new HashSet<string>();
        var queue = new PriorityQueue<string, double>();
        queue.Enqueue(from, 0);

        while (queue.TryDequeue(out var u, out var du))
        {
            if (!done.Add(u)) continue;
            if (u == to) break;
            foreach (var (v, len, key) in _adj[u])
            {
                if (_blocked.Contains(key)) continue;
                var nd = du + len;
                if (!dist.TryGetValue(v, out var dv) || nd < dv)
                {
                    dist[v] = nd;
                    prev[v] = u;
                    queue.Enqueue(v, nd);
                }
            }
        }

        if (!done.Contains(to)) return null;

        var path = new List<string> { to };
        var cur = to;
        while (cur != from)
        {
            cur = prev[cur];
            path.Add(cur);
        }
        path.Reverse();
        return new PathResult(path, dist[to]);
    }
}
