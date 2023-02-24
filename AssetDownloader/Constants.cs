namespace AssetDownloader;

public static class Constants
{
    public const string ClonedRepoFolder = "DragaliaManifests";

    public const string RepoUrl =
        "https://codeload.github.com/DragaliaLostRevival/DragaliaManifests/zip/refs/heads/master";

    // Need this to estimate cloning progress -- GitHub does not report the total size of the download
    public const int RepoSizeBytes = 637685460;

    public const string BaseUrl = "http://dragalialost.akamaized.net/dl/assetbundles/";
    public const string Android = "Android";
    public const string Ios = "iOS";

    public const string LatestAndroidManifestName = "20221002_y2XM6giU6zz56wCm";
    public const string LatestIosManifestName = "20221002_b1HyoeTFegeTexC0";

    public const string HelpText = """
        Usage: AssetDownloader.exe <arguments>
        Valid arguments:
            "-h" or "--help": Prints this help text. 
            "-s" or "--skip-old-assets": Only downloads the newest assets. (default: false)
            "-en" or "--download-en": Adds EN_US files to the download. (default: false)
            "-eu" or "--download-eu": Adds EN_EU and EN_US files to the download. (default: false)
            "-cn" or "--download-cn": Adds CN files to the download. (default: false)
            "-tw" or "--download-tw": Adds TW files to the download. (default: false)
            "-o <folder>" or "--output-folder <folder>": Sets the output folder. (default: DownloaderOutput)
            "-m <count>" or "--max-downloads <count>": Sets the maximum number of concurrent downloads. (default: 16)
            "-p <platform>" or "--platform <platform>": Sets the version of assets to download.
                Valid options: Android, iOS (default: Android)
        """;

    public const string JpManifest = "assetbundle.manifest.json";
    public const string EuManifest = "assetbundle.en_eu.manifest.json";
    public const string EnManifest = "assetbundle.en_us.manifest.json";
    public const string CnManifest = "assetbundle.zh_cn.manifest.json";
    public const string TwManifest = "assetbundle.zh_tw.manifest.json";
}
