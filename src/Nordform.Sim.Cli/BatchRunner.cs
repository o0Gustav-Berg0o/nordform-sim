using System.Globalization;
using System.Text;
using System.Text.Json;
using Nordform.Sim.Core;

namespace Nordform.Sim.Cli;

public sealed record BatchRow(
    string Scenario,
    int Seed,
    string Strategy,
    double EmptyPercent,
    double AverageWaitSeconds,
    int Shipped,
    int CompletedTransports,
    double DistanceMeters,
    double UtilizationPercent);

/// <summary>
/// Kör alla strategier mot alla scenariofiler i en mapp, för en liten uppsättning seeds, och
/// skriver resultatet till CSV och JSON. Varje scenario körs med sitt eget seed-fält ersatt, så
/// att samma seeds kan jämföras mellan scenarier.
/// </summary>
public static class BatchRunner
{
    private static readonly int[] Seeds = { 4217, 1000, 2000 };

    public static IReadOnlyList<BatchRow> Run(string dataDir, Layout layout, IReadOnlyList<Product> products)
    {
        IDispatchStrategy[] strategies =
        {
            new FifoStrategy(),
            new NearestStrategy(),
            new UrgentThenNearestStrategy(),
        };

        var scenarioFiles = Directory.GetFiles(Path.Combine(dataDir, "scenarios"), "*.json").OrderBy(f => f);
        var rows = new List<BatchRow>();

        foreach (var file in scenarioFiles)
        {
            var baseScenario = JsonData.Load<Scenario>(file);
            foreach (var seed in Seeds)
            {
                var scenario = baseScenario with { Seed = seed };
                foreach (var strategy in strategies)
                {
                    var engine = new SimulationEngine(layout, products, scenario, strategy);
                    engine.RunFor(scenario.DurationSeconds);
                    var m = engine.GetMetrics();
                    rows.Add(new BatchRow(
                        baseScenario.Name,
                        seed,
                        strategy.Name,
                        m.EmptyPercent,
                        m.AverageWaitSeconds,
                        m.Shipped,
                        m.CompletedTransports,
                        m.DistanceMeters,
                        m.UtilizationPercent));
                }
            }
        }

        return rows;
    }

    public static void WriteCsv(string path, IReadOnlyList<BatchRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("scenario,seed,strategy,empty_percent,average_wait_seconds,shipped,completed_transports,distance_meters,utilization_percent");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',',
                Csv(r.Scenario),
                r.Seed.ToString(CultureInfo.InvariantCulture),
                Csv(r.Strategy),
                r.EmptyPercent.ToString("F2", CultureInfo.InvariantCulture),
                r.AverageWaitSeconds.ToString("F2", CultureInfo.InvariantCulture),
                r.Shipped.ToString(CultureInfo.InvariantCulture),
                r.CompletedTransports.ToString(CultureInfo.InvariantCulture),
                r.DistanceMeters.ToString("F1", CultureInfo.InvariantCulture),
                r.UtilizationPercent.ToString("F2", CultureInfo.InvariantCulture)));
        }
        File.WriteAllText(path, sb.ToString());
    }

    public static void WriteJson(string path, IReadOnlyList<BatchRow> rows)
    {
        var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
