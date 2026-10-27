using Nordform.Sim.Core;

namespace Nordform.Sim.Cli.Tests;

public class BatchRunnerTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Nordform.Sim.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Hittar inte repots rot");
    }

    private static string DataDir() => Path.Combine(RepoRoot(), "data");

    [Fact]
    public void Run_GerEnRadPerScenarioSeedOchStrategi()
    {
        var dataDir = DataDir();
        var layout = JsonData.Load<Layout>(Path.Combine(dataDir, "layouts", "furniture-line.json"));
        var products = JsonData.Load<List<Product>>(Path.Combine(dataDir, "products", "furniture-products.json"));
        var scenarioCount = Directory.GetFiles(Path.Combine(dataDir, "scenarios"), "*.json").Length;

        var rows = BatchRunner.Run(dataDir, layout, products);

        Assert.Equal(scenarioCount * 3 * 3, rows.Count);
        Assert.All(rows, r => Assert.True(r.Shipped >= 0));
    }

    [Fact]
    public void Run_SammaSeedGerSammaResultat()
    {
        var dataDir = DataDir();
        var layout = JsonData.Load<Layout>(Path.Combine(dataDir, "layouts", "furniture-line.json"));
        var products = JsonData.Load<List<Product>>(Path.Combine(dataDir, "products", "furniture-products.json"));

        var first = BatchRunner.Run(dataDir, layout, products);
        var second = BatchRunner.Run(dataDir, layout, products);

        Assert.Equal(first, second);
    }

    [Fact]
    public void WriteCsvOchWriteJson_SkriverLasbaraFiler()
    {
        var dataDir = DataDir();
        var layout = JsonData.Load<Layout>(Path.Combine(dataDir, "layouts", "furniture-line.json"));
        var products = JsonData.Load<List<Product>>(Path.Combine(dataDir, "products", "furniture-products.json"));
        var rows = BatchRunner.Run(dataDir, layout, products);

        var tmpDir = Path.Combine(Path.GetTempPath(), "nordform-batch-test-" + Guid.NewGuid());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var csvPath = Path.Combine(tmpDir, "batch.csv");
            var jsonPath = Path.Combine(tmpDir, "batch.json");

            BatchRunner.WriteCsv(csvPath, rows);
            BatchRunner.WriteJson(jsonPath, rows);

            var csvLines = File.ReadAllLines(csvPath);
            Assert.Equal(rows.Count + 1, csvLines.Length);
            Assert.StartsWith("scenario,seed,strategy,", csvLines[0]);

            var json = File.ReadAllText(jsonPath);
            Assert.Contains("\"Scenario\"", json);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }
}
