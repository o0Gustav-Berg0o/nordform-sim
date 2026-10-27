namespace Nordform.Sim.Core;

public enum TruckState { Idle, Parking, ToPickup, Loading, ToDrop, Unloading }

/// <summary>Inställningar för motorn. Enheter: layoutens enhet och sekunder.</summary>
public sealed record EngineOptions(
    double TruckSpeed = 40,
    double DwellSeconds = 2,
    double ParkAfterIdleSeconds = 45,
    double TimeStep = 0.1,
    double UrgentShare = 0.12);

public sealed record SimulationMetrics(
    double SimulatedSeconds,
    double EmptyPercent,
    double AverageWaitSeconds,
    double DistanceMeters,
    int Shipped,
    int CompletedTransports,
    int QueuedJobs,
    double UtilizationPercent);

public sealed record TruckSnapshot(int Id, double X, double Y, string Node, TruckState State, int? JobId);

/// <summary>
/// Deterministisk simulering av internlogistik. Pallar följer en produkts arbetsplan genom
/// fabrikens stationer, och truckar flyttar dem. Vilken truck som tar vilket uppdrag avgörs av
/// den <see cref="IDispatchStrategy"/> som skickas in. Allt slumpmässigt styrs av scenariots seed.
/// </summary>
public sealed class SimulationEngine
{
    private enum PalletState { Processing, Waiting, Carried, Gone, Blocked }

    private sealed class Pallet
    {
        public int Id;
        public bool Urgent;
        public Product Product = null!;
        public int Step;
        public string At = "";
        public PalletState State;
        public double ReadyAt;
    }

    private sealed class Job
    {
        public int Id;
        public Pallet Pallet = null!;
        public string FromStation = "";
        public string ToStation = "";
        public string FromNode = "";
        public string ToNode = "";
        public double CreatedAt;
        public bool Urgent;
        public bool Assigned;

        public TransportJob ToRecord() => new(Id, FromNode, ToNode, CreatedAt, Urgent);
    }

    private sealed class Truck
    {
        public int Id;
        public double X;
        public double Y;
        public string Node = "";
        public string Home = "";
        public List<string> Path = new();
        public string Dest = "";
        public TruckState State = TruckState.Idle;
        public Job? Job;
        public double Timer;
        public double IdleTime;
        public double BusyTime;
    }

    private readonly Layout _layout;
    private readonly Scenario _scenario;
    private readonly IDispatchStrategy _strategy;
    private readonly EngineOptions _opt;
    private readonly PathFinder _paths;
    private readonly Rng _rng;
    private readonly Dictionary<string, Node> _nodes;
    private readonly Dictionary<string, Station> _stations;
    private readonly Dictionary<string, Product> _products;
    private readonly List<(string Id, double Weight)> _mix;
    private readonly Dictionary<string, int> _wip = new();
    private readonly Dictionary<string, string?> _lastProduct = new();
    private readonly HashSet<string> _downStations = new();
    private readonly HashSet<int> _brokenTrucks = new();
    private readonly List<Product> _pendingOrders = new();
    private readonly List<Pallet> _pallets = new();
    private readonly List<Job> _jobs = new();
    private readonly List<Truck> _trucks = new();

    private double _time;
    private double _nextSpawn = 3;
    private int _palletSeq;
    private int _jobSeq;
    private int _ordersStarted;
    private int _shipped;
    private int _done;
    private double _emptyDistance;
    private double _loadedDistance;
    private double _waitSum;
    private int _waitCount;

    /// <summary>Anropas vid hämtning och lämning, med simulerad tid och en text.</summary>
    public event Action<double, string>? EventLogged;

    public SimulationEngine(
        Layout layout,
        IReadOnlyList<Product> products,
        Scenario scenario,
        IDispatchStrategy strategy,
        EngineOptions? options = null)
    {
        _layout = layout;
        _scenario = scenario;
        _strategy = strategy;
        _opt = options ?? new EngineOptions();
        _paths = new PathFinder(layout);
        _rng = new Rng(scenario.Seed);
        _nodes = layout.Nodes.ToDictionary(n => n.Id);
        _stations = layout.Stations.ToDictionary(s => s.Id);
        _products = products.ToDictionary(p => p.Id);

        foreach (var s in layout.Stations) { _wip[s.Id] = 0; _lastProduct[s.Id] = null; }

        _mix = scenario.ProductMix
            .Where(kv => _products.ContainsKey(kv.Key) && kv.Value > 0)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => (Id: kv.Key, Weight: kv.Value))
            .ToList();
        if (_mix.Count == 0)
            throw new InvalidOperationException("Scenariot har ingen produkt i produktmixen som finns i produktlistan.");

        foreach (var product in _mix.Select(m => _products[m.Id]))
            foreach (var step in product.Routing)
                if (!layout.Stations.Any(s => s.Kind == step.StationKind))
                    throw new InvalidOperationException(
                        $"Produkten {product.Id} kräver stationstypen '{step.StationKind}' som saknas i layouten.");

        IReadOnlyList<string> parking = layout.ParkingNodes is { Count: > 0 } parkingNodes
            ? parkingNodes
            : new[] { layout.Nodes[0].Id };
        for (var i = 0; i < scenario.Trucks; i++)
        {
            var home = parking[i % parking.Count];
            var n = _nodes[home];
            _trucks.Add(new Truck { Id = i, X = n.X, Y = n.Y, Node = home, Home = home, Dest = home });
        }
    }

    public double Time => _time;
    public int OrdersStarted => _ordersStarted;
    public int Shipped => _shipped;
    public int ActivePallets => _pallets.Count(p => p.State != PalletState.Gone);
    public int QueuedJobs => _jobs.Count;
    public IReadOnlyDictionary<string, int> WipByStation => _wip;

    public IReadOnlyList<TruckSnapshot> Trucks =>
        _trucks.Select(t => new TruckSnapshot(t.Id, t.X, t.Y, t.Node, t.State, t.Job?.Id)).ToList();

    /// <summary>Kör ett tidssteg med standardlängden.</summary>
    public void Step() => Step(_opt.TimeStep);

    /// <summary>Kör simuleringen så många hela tidssteg som ryms i den angivna tiden.</summary>
    public void RunFor(double seconds)
    {
        var steps = (int)Math.Round(seconds / _opt.TimeStep);
        for (var i = 0; i < steps; i++) Step();
    }

    public void Step(double dt)
    {
        _time += dt;

        while (_time >= _nextSpawn)
        {
            _pendingOrders.Add(PickProduct());
            _nextSpawn += _scenario.OrderIntervalSeconds * (0.7 + 0.6 * _rng.NextDouble());
        }

        while (_pendingOrders.Count > 0 && TryStartOrder(_pendingOrders[0]))
            _pendingOrders.RemoveAt(0);

        for (var i = 0; i < _pallets.Count; i++)
        {
            var p = _pallets[i];
            if (p.State == PalletState.Processing && _time >= p.ReadyAt && !_downStations.Contains(p.At))
                PalletReady(p);
        }

        for (var i = 0; i < _pallets.Count; i++)
        {
            var p = _pallets[i];
            if (p.State == PalletState.Blocked) TryRoute(p);
        }

        foreach (var truck in _trucks) UpdateTruck(truck, dt);

        _pallets.RemoveAll(p => p.State == PalletState.Gone);
    }

    /// <summary>Blockerar eller öppnar ett korridorsegment. Truckar på väg får ny väg om det går.</summary>
    public void SetCorridorBlocked(string a, string b, bool blocked)
    {
        _paths.SetBlocked(a, b, blocked);
        foreach (var tr in _trucks)
        {
            var moving = tr.State is TruckState.ToPickup or TruckState.ToDrop or TruckState.Parking;
            if (moving && tr.Path.Count > 0) SetPath(tr, tr.Dest);
        }
    }

    /// <summary>Stänger av eller öppnar en station. En avstängd station tar inte emot nya pallar
    /// och pallar som redan bearbetas där blir kvar tills den öppnas igen.</summary>
    public void SetStationDown(string stationId, bool down)
    {
        if (down) _downStations.Add(stationId);
        else _downStations.Remove(stationId);
    }

    /// <summary>Truckhaveri: en trasig truck fryser på plats och släpper sitt uppdrag.
    /// Uppdraget blir tillgängligt för andra truckar igen.</summary>
    public void SetTruckBroken(int truckId, bool broken)
    {
        var tr = _trucks.FirstOrDefault(t => t.Id == truckId);
        if (tr is null) return;

        if (broken)
        {
            _brokenTrucks.Add(truckId);
            if (tr.Job is { } job)
            {
                job.Assigned = false;
                tr.Job = null;
            }
        }
        else
        {
            _brokenTrucks.Remove(truckId);
        }
    }

    public SimulationMetrics GetMetrics()
    {
        var total = _emptyDistance + _loadedDistance;
        var busy = _trucks.Sum(t => t.BusyTime);
        return new SimulationMetrics(
            _time,
            total > 0 ? _emptyDistance / total * 100 : 0,
            _waitCount > 0 ? _waitSum / _waitCount : 0,
            total * _layout.MetersPerUnit,
            _shipped,
            _done,
            _jobs.Count,
            _time > 0 && _trucks.Count > 0 ? busy / (_time * _trucks.Count) * 100 : 0);
    }

    // ---------- order och pallar ----------

    /// <summary>Försöker starta en order. Misslyckas om alla kandidatstationer är fulla eller avstängda,
    /// och ordern blir då kvar i kön tills det finns plats.</summary>
    private bool TryStartOrder(Product product)
    {
        var station = ChooseStation(product.Routing[0].StationKind);
        if (station is null) return false;

        var pallet = new Pallet
        {
            Id = ++_palletSeq,
            Urgent = _rng.NextDouble() < _opt.UrgentShare,
            Product = product,
            Step = 0,
            At = station.Id,
            State = PalletState.Processing,
        };
        pallet.ReadyAt = _time + ProcessTime(product, 0, station);
        _wip[station.Id]++;
        _ordersStarted++;
        _pallets.Add(pallet);
        return true;
    }

    private Product PickProduct()
    {
        var total = _mix.Sum(m => m.Weight);
        var r = _rng.NextDouble() * total;
        var acc = 0.0;
        foreach (var (id, weight) in _mix)
        {
            acc += weight;
            if (r < acc) return _products[id];
        }
        return _products[_mix[^1].Id];
    }

    /// <summary>Väljer den station av rätt typ som har minst last, är öppen och har plats kvar.
    /// Vid lika last avgör slumpen. Null om ingen kandidat har plats.</summary>
    private Station? ChooseStation(string kind, string? exemptFromCapacity = null)
    {
        int Load(Station s) => _wip[s.Id] + _jobs.Count(j => j.ToStation == s.Id);
        bool HasRoom(Station s) =>
            !_downStations.Contains(s.Id) &&
            (s.Id == exemptFromCapacity || s.Capacity is null || Load(s) < s.Capacity.Value);

        var candidates = _layout.Stations.Where(s => s.Kind == kind).Where(HasRoom).ToList();
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        var min = candidates.Min(Load);
        var tied = candidates.Where(s => Load(s) == min).ToList();
        return tied.Count == 1 ? tied[0] : tied[(int)(_rng.NextDouble() * tied.Count)];
    }

    private double ProcessTime(Product product, int step, Station station)
    {
        var baseSeconds = product.Routing[step].ProcessSeconds ?? station.ProcessSeconds;
        var changeover = _lastProduct[station.Id] is { } last && last != product.Id
            ? station.ChangeoverSeconds
            : 0;
        _lastProduct[station.Id] = product.Id;
        return baseSeconds * (0.8 + 0.4 * _rng.NextDouble()) + changeover;
    }

    private void PalletReady(Pallet p)
    {
        if (!TryRoute(p)) p.State = PalletState.Blocked;
    }

    /// <summary>Försöker ta nästa steg i pallens arbetsplan. Returnerar false om nästa station
    /// är full eller avstängd, så att anroparen kan låta pallen vänta kvar.</summary>
    private bool TryRoute(Pallet p)
    {
        var last = p.Product.Routing.Count - 1;
        if (p.Step == last)
        {
            p.State = PalletState.Gone;
            _wip[p.At]--;
            _shipped++;
            return true;
        }

        var next = ChooseStation(p.Product.Routing[p.Step + 1].StationKind, exemptFromCapacity: p.At);
        if (next is null) return false;

        if (next.Id == p.At)
        {
            // Nästa steg sker på samma station, ingen transport behövs.
            p.Step++;
            p.ReadyAt = _time + ProcessTime(p.Product, p.Step, next);
            p.State = PalletState.Processing;
            return true;
        }

        _jobs.Add(new Job
        {
            Id = ++_jobSeq,
            Pallet = p,
            FromStation = p.At,
            ToStation = next.Id,
            FromNode = _stations[p.At].Node,
            ToNode = next.Node,
            CreatedAt = _time,
            Urgent = p.Urgent,
        });
        p.State = PalletState.Waiting;
        return true;
    }

    // ---------- truckar ----------

    private void UpdateTruck(Truck tr, double dt)
    {
        if (_brokenTrucks.Contains(tr.Id)) return;

        if (tr.State is TruckState.Idle or TruckState.Parking) TryAssign(tr);

        if (tr.State == TruckState.Idle)
        {
            tr.IdleTime += dt;
            if (tr.IdleTime > _opt.ParkAfterIdleSeconds && tr.Node != tr.Home && SetPath(tr, tr.Home))
                tr.State = TruckState.Parking;
        }

        if (tr.State is TruckState.ToPickup or TruckState.ToDrop or TruckState.Parking)
        {
            Move(tr, dt);
            if (tr.Path.Count == 0)
            {
                if (tr.Node == tr.Dest) Arrive(tr);
                else SetPath(tr, tr.Dest);
            }
        }
        else if (tr.State == TruckState.Loading)
        {
            tr.Timer -= dt;
            if (tr.Timer <= 0) FinishLoading(tr);
        }
        else if (tr.State == TruckState.Unloading)
        {
            tr.Timer -= dt;
            if (tr.Timer <= 0) FinishUnloading(tr);
        }

        if (tr.State is not (TruckState.Idle or TruckState.Parking)) tr.BusyTime += dt;
    }

    private void TryAssign(Truck tr)
    {
        var waiting = _jobs.Where(j => !j.Assigned).ToList();
        if (waiting.Count == 0) return;

        // En truck som är på väg till parkering räknas från nästa nod på vägen.
        var anchor = tr.Path.Count > 0 ? tr.Path[0] : tr.Node;
        var choice = _strategy.Select(
            new TruckView(tr.Id, anchor),
            waiting.Select(j => j.ToRecord()).ToList(),
            _paths,
            _time);
        if (choice is null) return;

        var job = waiting.FirstOrDefault(j => j.Id == choice.Id);
        if (job is null) return;

        job.Assigned = true;
        tr.Job = job;
        if (!SetPath(tr, job.FromNode))
        {
            job.Assigned = false;
            tr.Job = null;
            return;
        }
        tr.State = TruckState.ToPickup;
        tr.IdleTime = 0;
    }

    private bool SetPath(Truck tr, string dest)
    {
        if (tr.Path.Count > 0)
        {
            var anchor = tr.Path[0];
            var r = _paths.FindPath(anchor, dest);
            if (r is null) return false;
            tr.Path = new List<string>(r.Nodes);
        }
        else
        {
            var r = _paths.FindPath(tr.Node, dest);
            if (r is null) return false;
            tr.Path = r.Nodes.Skip(1).ToList();
        }
        tr.Dest = dest;
        return true;
    }

    private void Move(Truck tr, double dt)
    {
        var remaining = _opt.TruckSpeed * dt;
        var loaded = tr.State == TruckState.ToDrop;

        while (remaining > 1e-9 && tr.Path.Count > 0)
        {
            var n = _nodes[tr.Path[0]];
            var dx = n.X - tr.X;
            var dy = n.Y - tr.Y;
            var d = Math.Sqrt(dx * dx + dy * dy);
            var travelled = Math.Min(d, remaining);

            if (d <= remaining)
            {
                tr.X = n.X;
                tr.Y = n.Y;
                tr.Node = tr.Path[0];
                tr.Path.RemoveAt(0);
                remaining -= d;
            }
            else
            {
                tr.X += dx / d * remaining;
                tr.Y += dy / d * remaining;
                remaining = 0;
            }

            if (loaded) _loadedDistance += travelled;
            else _emptyDistance += travelled;
        }
    }

    private void Arrive(Truck tr)
    {
        switch (tr.State)
        {
            case TruckState.ToPickup:
                tr.State = TruckState.Loading;
                tr.Timer = _opt.DwellSeconds;
                _waitSum += _time - tr.Job!.CreatedAt;
                _waitCount++;
                break;
            case TruckState.ToDrop:
                tr.State = TruckState.Unloading;
                tr.Timer = _opt.DwellSeconds;
                break;
            case TruckState.Parking:
                tr.State = TruckState.Idle;
                break;
        }
    }

    private void FinishLoading(Truck tr)
    {
        var job = tr.Job!;
        job.Pallet.State = PalletState.Carried;
        _wip[job.FromStation]--;
        _jobs.Remove(job);
        EventLogged?.Invoke(_time,
            $"T{tr.Id + 1} hämtar P{job.Pallet.Id}{(job.Urgent ? " (brådskande)" : "")} vid {_stations[job.FromStation].Name}");

        tr.State = TruckState.ToDrop;
        if (!SetPath(tr, job.ToNode)) tr.Dest = job.ToNode;
    }

    private void FinishUnloading(Truck tr)
    {
        var job = tr.Job!;
        var p = job.Pallet;
        p.At = job.ToStation;
        p.Step++;
        p.State = PalletState.Processing;
        p.ReadyAt = _time + ProcessTime(p.Product, p.Step, _stations[p.At]);
        _wip[p.At]++;
        _done++;
        EventLogged?.Invoke(_time, $"T{tr.Id + 1} lämnar P{p.Id} vid {_stations[p.At].Name}");

        tr.Job = null;
        tr.State = TruckState.Idle;
        tr.IdleTime = 0;
    }
}
