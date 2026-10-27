using Nordform.Sim.Core;

// Första versionen: läser en layout och visar grunddata. Batchkörning av scenarier är NF-040 på tavlan.
var layoutPath = args.Length > 0 ? args[0] : "data/layouts/furniture-line.json";
var layout = JsonData.Load<Layout>(layoutPath);
var paths = new PathFinder(layout);

Console.WriteLine($"{layout.Name}: {layout.Stations.Count} stationer, {layout.Nodes.Count} noder, {layout.Edges.Count} segment");

var d = paths.Distance("GODS", "FARD");
Console.WriteLine(d is null
    ? "Ingen väg mellan GODS och FARD"
    : $"GODS till FARD: {d.Value:F0} enheter ({d.Value * layout.MetersPerUnit:F1} m)");
