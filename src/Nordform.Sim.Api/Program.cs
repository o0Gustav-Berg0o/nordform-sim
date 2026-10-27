using Nordform.Sim.Api;
using Nordform.Sim.Core;

var builder = WebApplication.CreateBuilder(args);
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
app.Run();

public partial class Program;
