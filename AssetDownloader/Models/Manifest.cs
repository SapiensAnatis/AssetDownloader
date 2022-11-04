using System.Text.Json.Serialization;

namespace AssetDownloader.Models;

public class Manifest
{
    [JsonPropertyName("categories")]
    public IEnumerable<AssetCategory> Categories { get; init; }

    [JsonPropertyName("rawAssets")]
    public IEnumerable<Asset> RawAssets { get; init; }

    public IEnumerable<Asset> AllAssets => Categories.SelectMany(c => c.Assets).Concat(RawAssets);

    [JsonConstructor]
    public Manifest(IEnumerable<AssetCategory> categories, IEnumerable<Asset> rawAssets)
    {
        Categories = categories;
        RawAssets = rawAssets;
    }
}

public record AssetCategory([property: JsonPropertyName("assets")] List<Asset> Assets);

public record Asset([property: JsonPropertyName("hash")] string Hash);
