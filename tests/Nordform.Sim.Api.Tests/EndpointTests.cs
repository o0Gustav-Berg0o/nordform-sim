using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Nordform.Sim.Api.Tests;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Theory]
    [InlineData("/layouts", "furniture-line")]
    [InlineData("/products", "furniture-products")]
    [InlineData("/scenarios", "normal-day")]
    public async Task Lista_InnehallerKandId(string url, string expectedId)
    {
        var response = await _client.GetStringAsync(url);
        Assert.Contains(expectedId, response);
    }

    [Theory]
    [InlineData("/layouts/furniture-line", "\"name\"")]
    [InlineData("/products/furniture-products", "\"routing\"")]
    [InlineData("/scenarios/normal-day", "\"seed\":4217")]
    public async Task Lasa_GerInnehallet(string url, string expectedFragment)
    {
        var response = await _client.GetStringAsync(url);
        Assert.Contains(expectedFragment, response);
    }

    [Theory]
    [InlineData("/layouts/does-not-exist")]
    [InlineData("/products/does-not-exist")]
    [InlineData("/scenarios/does-not-exist")]
    public async Task Lasa_SaknatIdGer404(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/scenarios/..%2f..%2fappsettings")]
    [InlineData("/layouts/..%5c..%5cappsettings")]
    public async Task Lasa_PathTraversalGer404(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_ArOk()
    {
        var response = await _client.GetStringAsync("/health");
        Assert.Contains("ok", response);
    }
}
