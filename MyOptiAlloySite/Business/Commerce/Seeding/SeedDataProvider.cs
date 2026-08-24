using System.Text.Json;
using MyOptiAlloySite.Business.Commerce.Seeding.Data;

namespace MyOptiAlloySite.Business.Commerce.Seeding;

/// <summary>
/// Loads and caches the seed dataset from disk.
/// </summary>
public sealed class SeedDataProvider(IWebHostEnvironment environment)
{
    public const string RelativePath = "SeedData/commerce-catalog.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private SeedDocument _cached;

    public SeedDocument Load()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var path = Path.Combine(environment.ContentRootPath, RelativePath);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Seed dataset not found at '{path}'. Commerce seeding reads its data from " +
                $"'{RelativePath}' relative to the content root.");
        }

        using var stream = File.OpenRead(path);
        _cached = JsonSerializer.Deserialize<SeedDocument>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"Seed dataset at '{path}' deserialized to null.");

        return _cached;
    }
}
