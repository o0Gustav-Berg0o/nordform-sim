using Nordform.Sim.Core;

namespace Nordform.Sim.Core.Tests;

public class StrategyTests
{
    private static (IReadOnlyList<TransportJob> Jobs, PathFinder Paths, TruckView Truck) Setup()
    {
        var jobs = new List<TransportJob>
        {
            new(1, "B", "C", CreatedAt: 5, Urgent: false),
            new(2, "C", "A", CreatedAt: 1, Urgent: false),
            new(3, "D", "A", CreatedAt: 3, Urgent: true),
        };
        return (jobs, new PathFinder(TestData.SmallLayout()), new TruckView(1, "A"));
    }

    [Fact]
    public void Fifo_picks_oldest_job()
    {
        var (jobs, paths, truck) = Setup();
        Assert.Equal(2, new FifoStrategy().Select(truck, jobs, paths, 10)!.Id);
    }

    [Fact]
    public void Nearest_picks_closest_pickup()
    {
        var (jobs, paths, truck) = Setup();
        Assert.Equal(1, new NearestStrategy().Select(truck, jobs, paths, 10)!.Id);
    }

    [Fact]
    public void Urgent_job_goes_first()
    {
        var (jobs, paths, truck) = Setup();
        Assert.Equal(3, new UrgentThenNearestStrategy().Select(truck, jobs, paths, 10)!.Id);
    }

    [Fact]
    public void No_jobs_gives_null()
    {
        var (_, paths, truck) = Setup();
        Assert.Null(new NearestStrategy().Select(truck, Array.Empty<TransportJob>(), paths, 0));
    }
}
