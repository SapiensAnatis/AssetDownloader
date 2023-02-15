namespace AssetDownloader.Models;

public class Manifest
{
    public List<AssetCategory> Categories { get; }
    public List<AssetInfo> RawAssets { get; }
    public IEnumerable<AssetInfo> AllAssets =>
        Categories.SelectMany(c => c.Assets).Concat(RawAssets);

    public Manifest(List<AssetCategory> categories, List<AssetInfo> rawAssets)
    {
        Categories = categories;
        RawAssets = rawAssets;
    }
}

public class AssetCategory
{
    public List<AssetInfo> Assets { get; }

    public AssetCategory(List<AssetInfo> assets)
    {
        Assets = assets;
    }
}

public class AssetInfo
{
    public string Name { get; }
    public string Hash { get; }
    public long Size { get; }

    internal string HashId { get; }
    internal byte[] HashBytes { get; }
    internal string DownloadPath { get; }

    public AssetInfo(string name, string hash, long size)
    {
        Name = name;
        Hash = hash;
        Size = size;

        HashId = hash[..2];
        HashBytes = Base32.ToBytes(hash);
        DownloadPath = $"{HashId}/{hash}";
    }
}
