using Nordform.Sim.Core;

namespace Nordform.Sim.Core.Tests;

public class RngTests
{
    [Fact]
    public void Sequence_matches_the_web_prototype_for_seed_4217()
    {
        // Värdena kommer från samma mulberry32-implementation i prototype/nordform-map.html.
        var rng = new Rng(4217);
        Assert.Equal(0.62648735870607197, rng.NextDouble(), 12);
        Assert.Equal(0.049428804777562618, rng.NextDouble(), 12);
        Assert.Equal(0.86280186986550689, rng.NextDouble(), 12);
    }

    [Fact]
    public void Same_seed_gives_same_sequence()
    {
        var a = new Rng(7);
        var b = new Rng(7);
        for (var i = 0; i < 100; i++) Assert.Equal(a.NextDouble(), b.NextDouble());
    }

    [Fact]
    public void Values_are_in_unit_interval()
    {
        var rng = new Rng(1);
        for (var i = 0; i < 10_000; i++)
        {
            var v = rng.NextDouble();
            Assert.InRange(v, 0.0, 1.0);
            Assert.NotEqual(1.0, v);
        }
    }
}
