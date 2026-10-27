using Nordform.Sim.Core;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/strategies", () => new[]
{
    new FifoStrategy().Name,
    new NearestStrategy().Name,
    new UrgentThenNearestStrategy().Name,
});

// NF-050: SignalR-ström av simuleringstillstånd till kartan.
app.Run();
