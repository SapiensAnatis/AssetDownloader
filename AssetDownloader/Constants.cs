namespace AssetDownloader;

public static class Constants
{
    public const bool Download_EN_US = true;

    public const bool Download_ZH_CN = false;

    public const bool Download_ZH_TW = false;

    // See: https://aria2.github.io/manual/en/html/aria2c.html#options
    public const string AriaOptions =
        "-j16 --enable-mmap --save-session-interval=120 --conditional-get=true --auto-file-renaming=false --summary-interval=0 --show-console-readout=false  --download-result=full";

    public const string RepoUrl =
        "https://codeload.github.com/CerisWhite/dl-datamine/zip/refs/heads/master";

    public const string ClonedRepoFolder = "dl-datamine";

    public const string Platform = "Android";

    public const string BaseUrl = "http://dragalialost.akamaized.net/dl/assetbundles/" + Platform;

    public const string DownloadOutputFolder = "DownloadResult";
}
