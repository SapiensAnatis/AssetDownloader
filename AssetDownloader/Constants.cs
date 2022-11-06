namespace AssetDownloader;

public static class Constants
{
    public const bool Download_EN_US = true;

    public const bool Download_ZH_CN = false;

    public const bool Download_ZH_TW = false;

    public const string AriaConcurrentFiles = "16";

    // See: https://aria2.github.io/manual/en/html/aria2c.html#options
    public const string AriaOptions =
        $"-j{AriaConcurrentFiles} "
        + "--enable-mmap "
        + "--save-session-interval=120 "
        + "--conditional-get=true "
        + "--auto-file-renaming=false "
        + "--console-log-level=warn "
        + "--show-console-readout=false "
        + "--summary-interval=0";

    public const string DownloadOutputFolder = "DownloadResult";

    public const string ClonedRepoFolder = "dl-datamine";

    public const string Platform = "Android";

    public const string AriaFilePath = "aria_session.txt";

    public const string RepoUrl =
        "https://codeload.github.com/CerisWhite/dl-datamine/zip/refs/heads/master";

    // Need this to estimate cloning progress -- GitHub does not report the total size of the download
    public const int RepoSizeBytes = 635812219;

    public const string BaseUrl = "http://dragalialost.akamaized.net/dl/assetbundles/" + Platform;
}
