using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace Nordform.Sim.Api.Tests;

/// <summary>Testar HTTP-sidan av NF-050 (starta/läsa/stoppa en session) och att SignalR-hubben
/// faktiskt strömmar minst ett tillstånd till en ansluten klient.</summary>
public class SimulationEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SimulationEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> StartSessionAsync(string strategy = "nearest", double speed = 60)
    {
        var response = await _client.PostAsJsonAsync("/simulations", new
        {
            layoutId = "furniture-line",
            productsId = "furniture-products",
            scenarioId = "normal-day",
            strategy,
            speed,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartResponse>();
        return body!.SessionId;
    }

    private sealed record StartResponse(string SessionId);

    [Fact]
    public async Task Starting_a_session_lets_you_read_its_state_and_then_stop_it()
    {
        var sessionId = await StartSessionAsync();

        var snapshot = await _client.GetStringAsync($"/simulations/{sessionId}");
        Assert.Contains("\"trucks\"", snapshot, StringComparison.OrdinalIgnoreCase);

        var stopResponse = await _client.PostAsync($"/simulations/{sessionId}/stop", null);
        Assert.Equal(HttpStatusCode.OK, stopResponse.StatusCode);

        // Sessionen städas bort asynkront, så vi väntar in det innan vi kontrollerar 404.
        HttpResponseMessage? afterStop = null;
        for (var i = 0; i < 50; i++)
        {
            afterStop = await _client.GetAsync($"/simulations/{sessionId}");
            if (afterStop.StatusCode == HttpStatusCode.NotFound) break;
            await Task.Delay(20);
        }
        Assert.Equal(HttpStatusCode.NotFound, afterStop!.StatusCode);
    }

    [Fact]
    public async Task Starting_a_session_with_an_unknown_layout_gives_404()
    {
        var response = await _client.PostAsJsonAsync("/simulations", new
        {
            layoutId = "does-not-exist",
            productsId = "furniture-products",
            scenarioId = "normal-day",
            strategy = "nearest",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Starting_a_session_with_an_unknown_strategy_gives_400()
    {
        var response = await _client.PostAsJsonAsync("/simulations", new
        {
            layoutId = "furniture-line",
            productsId = "furniture-products",
            scenarioId = "normal-day",
            strategy = "does-not-exist",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Hub_streams_at_least_one_state_update_to_a_joined_client()
    {
        var sessionId = await StartSessionAsync(speed: 200);

        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/hub/simulation"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var received = new TaskCompletionSource<object?>();
        connection.On<object>("state", _ => received.TrySetResult(null));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinSession", sessionId);

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(received.Task, completed);

        await connection.DisposeAsync();
    }
}
