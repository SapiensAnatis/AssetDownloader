using AssetDownloader.Models;

namespace AssetDownloader;

public class FilenameEqualityComparer : IEqualityComparer<AssetInfo>
{
    public bool Equals(AssetInfo? x, AssetInfo? y) => x?.Name == y?.Name;

    public int GetHashCode(AssetInfo obj) => obj.Name.GetHashCode();
}
