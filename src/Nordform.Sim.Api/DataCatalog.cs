namespace Nordform.Sim.Api;

/// <summary>
/// Listar och slår upp data-filer (layouter, produkter, scenarier) under en datamapp.
/// Varje resurstyp är en undermapp med .json-filer, och filnamnet (utan .json) är id:t.
/// </summary>
public static class DataCatalog
{
    /// <summary>
    /// Hittar repots <c>data</c>-mapp genom att leta uppåt från contentRoot efter lösningsfilen.
    /// Behövs eftersom ASP.NET Core sätter contentRoot till projektmappen, inte repots rot.
    /// </summary>
    public static string FindDataDir(string contentRoot)
    {
        var dir = new DirectoryInfo(contentRoot);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Nordform.Sim.slnx")))
            dir = dir.Parent;
        return dir is null ? "data" : Path.Combine(dir.FullName, "data");
    }

    public static IReadOnlyList<string> ListIds(string dataDir, string resource)
    {
        var dir = Path.Combine(dataDir, resource);
        if (!Directory.Exists(dir)) return Array.Empty<string>();
        return Directory.GetFiles(dir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(id => id is not null)
            .Select(id => id!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Slår upp sökvägen till en resursfil. Returnerar null om id:t innehåller en sökväg
    /// (skydd mot path traversal) eller om filen inte finns.
    /// </summary>
    public static string? ResolvePath(string dataDir, string resource, string id)
    {
        if (string.IsNullOrEmpty(id) || id.Contains('/') || id.Contains('\\') || id.Contains(".."))
            return null;

        var dir = Path.GetFullPath(Path.Combine(dataDir, resource));
        var path = Path.GetFullPath(Path.Combine(dir, id + ".json"));
        if (!path.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return null;

        return File.Exists(path) ? path : null;
    }
}
