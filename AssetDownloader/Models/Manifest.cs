using Newtonsoft.Json;

namespace AssetDownloader.Models;

public class Manifest
{
    [JsonProperty("categories")]
    public IEnumerable<AssetCategory> Categories { get; init; }

    [JsonProperty("rawAssets")]
    public IEnumerable<Asset> RawAssets { get; init; }

    public IEnumerable<Asset> AllAssets => Categories.SelectMany(c => c.Assets).Concat(RawAssets);

    [JsonConstructor]
    public Manifest(IEnumerable<AssetCategory> categories, IEnumerable<Asset> rawAssets)
    {
        Categories = categories;
        RawAssets = rawAssets;
    }
}

public record AssetCategory([property: JsonProperty("assets")] List<Asset> Assets);

public record Asset([property: JsonProperty("hash")] string Hash);
