using Nordform.Sim.Api;
using Nordform.Sim.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISimulationBroadcaster, SignalRSimulationBroadcaster>();
builder.Services.AddSingleton<SimulationSessionManager>();
var app = builder.Build();

var dataDir = app.Configuration["DataDir"] ?? DataCatalog.FindDataDir(app.Environment.ContentRootPath);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/strategies", () => new[]
{
    new FifoStrategy().Name,
    new NearestStrategy().Name,
    new UrgentThenNearestStrategy().Name,
});

app.MapGet("/layouts", () => DataCatalog.ListIds(dataDir, "layouts"));
app.MapGet("/layouts/{id}", (string id) =>
{
    var path = DataCatalog.ResolvePath(dataDir, "layouts", id);
    return path is null ? Results.NotFound() : Results.Json(JsonData.Load<Layout>(path));
});

app.MapGet("/products", () => DataCatalog.ListIds(dataDir, "products"));
app.MapGet("/products/{id}", (string id) =>
{
    var path = DataCatalog.ResolvePath(dataDir, "products", id);
    return path is null ? Results.NotFound() : Results.Json(JsonData.Load<List<Product>>(path));
});

app.MapGet("/scenarios", () => DataCatalog.ListIds(dataDir, "scenarios"));
app.MapGet("/scenarios/{id}", (string id) =>
{
    var path = DataCatalog.ResolvePath(dataDir, "scenarios", id);
    return path is null ? Results.NotFound() : Results.Json(JsonData.Load<Scenario>(path));
});

// NF-050: SignalR-ström av simuleringstillstånd till kartan.
app.MapHub<SimulationHub>("/hub/simulation");

var strategyFactories = new Dictionary<string, Func<IDispatchStrategy>>(StringComparer.OrdinalIgnoreCase)
{
    ["fifo"] = () => new FifoStrategy(),
    ["nearest"] = () => new NearestStrategy(),
    ["urgent"] = () => new UrgentThenNearestStrategy(),
};

app.MapPost("/simulations", (StartSimulationRequest req, SimulationSessionManager sessions) =>
{
    var layoutPath = DataCatalog.ResolvePath(dataDir, "layouts", req.LayoutId);
    var productsPath = DataCatalog.ResolvePath(dataDir, "products", req.ProductsId);
    var scenarioPath = DataCatalog.ResolvePath(dataDir, "scenarios", req.ScenarioId);
    if (layoutPath is null || productsPath is null || scenarioPath is null)
        return Results.NotFound("Layout, produkter eller scenario hittades inte.");
    if (!strategyFactories.TryGetValue(req.Strategy, out var makeStrategy))
        return Results.BadRequest($"Okänd strategi '{req.Strategy}'. Giltiga: {string.Join(", ", strategyFactories.Keys)}.");

    var layout = JsonData.Load<Layout>(layoutPath);
    var products = JsonData.Load<List<Product>>(productsPath);
    var scenario = JsonData.Load<Scenario>(scenarioPath);

    var sessionId = sessions.Start(layout, products, scenario, makeStrategy(), req.Speed ?? 10);
    return Results.Ok(new { sessionId });
});

app.MapGet("/simulations/{id}", (string id, SimulationSessionManager sessions) =>
    sessions.GetSnapshot(id) is { } snapshot ? Results.Json(snapshot) : Results.NotFound());

app.MapPost("/simulations/{id}/stop", (string id, SimulationSessionManager sessions) =>
    sessions.Stop(id) ? Results.Ok() : Results.NotFound());

app.Run();

/// <summary>Begäran om att starta en ny simuleringssession. <see cref="Strategy"/> är "fifo",
/// "nearest" eller "urgent". <see cref="Speed"/> är simulerade sekunder per real sekund.</summary>
public sealed record StartSimulationRequest(string LayoutId, string ProductsId, string ScenarioId, string Strategy, double? Speed);

public partial class Program;
