using Nordform.Sim.Core;

// Kör alla planeringsstrategier på samma layout, produkter och scenario och jämför mätvärdena.
// Användning: dotnet run --project src/Nordform.Sim.Cli [datamapp] [scenariofil]
var dataDir = args.Length > 0 ? args[0] : "data";
var layout = JsonData.Load<Layout>(Path.Combine(dataDir, "layouts", "furniture-line.json"));
var products = JsonData.Load<List<Product>>(Path.Combine(dataDir, "products", "furniture-products.json"));
var scenarioFile = args.Length > 1 ? args[1] : Path.Combine(dataDir, "scenarios", "normal-day.json");
var scenario = JsonData.Load<Scenario>(scenarioFile);

Console.WriteLine($"{layout.Name}: {layout.Stations.Count} stationer, {scenario.Trucks} truckar");
Console.WriteLine($"Scenario: {scenario.Name}, seed {scenario.Seed}, {scenario.DurationSeconds / 60:F0} min simulerad tid");
Console.WriteLine();
Console.WriteLine($"{"Metod",-34}{"Tomkörning",12}{"Snittväntan",13}{"Order",8}{"Transp.",9}{"Sträcka",11}{"Utnyttj.",10}");

IDispatchStrategy[] strategies =
{
    new FifoStrategy(),
    new NearestStrategy(),
    new UrgentThenNearestStrategy(),
};

foreach (var strategy in strategies)
{
    var engine = new SimulationEngine(layout, products, scenario, strategy);
    engine.RunFor(scenario.DurationSeconds);
    var m = engine.GetMetrics();
    Console.WriteLine(
        $"{strategy.Name,-34}{m.EmptyPercent,11:F0} %{m.AverageWaitSeconds,11:F0} s{m.Shipped,8}{m.CompletedTransports,9}{m.DistanceMeters / 1000,8:F2} km{m.UtilizationPercent,8:F0} %");
}
