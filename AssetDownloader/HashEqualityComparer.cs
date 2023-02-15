using AssetDownloader.Models;

namespace AssetDownloader;

public class HashEqualityComparer : IEqualityComparer<AssetInfo>
{
    public bool Equals(AssetInfo? x, AssetInfo? y) => x?.Hash == y?.Hash;

    public int GetHashCode(AssetInfo obj) => obj.Hash.GetHashCode();
}
