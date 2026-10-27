using System.Text.Json;

namespace Nordform.Sim.Core;

/// <summary>Läser layout-, produkt- och scenariofiler.</summary>
public static class JsonData
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static T Load<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
        ?? throw new InvalidDataException($"Kunde inte läsa {path}");
}
