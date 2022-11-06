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

public class AssetCategory
{
    [JsonProperty("assets")]
    public List<Asset> Assets { get; init; }

    public AssetCategory(List<Asset> assets)
    {
        this.Assets = assets;
    }
}

public class Asset
{
    [JsonProperty("name")]
    public string Name { get; init; }

    [JsonProperty("hash")]
    public string Hash { get; init; }

    public Asset(string name, string hash)
    {
        this.Name = name;
        this.Hash = hash;
    }

    public override bool Equals(object? obj)
    {
        return obj is Asset asset && this.Name == asset.Name;
    }

    public override int GetHashCode()
    {
        return this.Name.GetHashCode();
    }
}
