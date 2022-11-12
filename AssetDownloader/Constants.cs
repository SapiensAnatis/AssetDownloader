namespace AssetDownloader;

public static class Constants
{
    public const string ClonedRepoFolder = "dl-datamine";

    public const string RepoUrl =
        "https://codeload.github.com/CerisWhite/dl-datamine/zip/refs/heads/master";

    // Need this to estimate cloning progress -- GitHub does not report the total size of the download
    public const int RepoSizeBytes = 635812219;

    public const string BaseUrl = "http://dragalialost.akamaized.net/dl/assetbundles/";

    public const string LatestManifestName = "20221002_y2XM6giU6zz56wCm";

    public const string HelpText = """
        Usage: AssetDownloader.exe <arguments>
        Valid arguments:
            "-h" or "--help": Prints this help text. 
            "-s" or "--skip-old-assets": Only downloads the newest assets. (default: false)
            "-en" or "--download-en": Adds EN files to the download. (default: false)
            "-cn" or "--download-cn": Adds CN files to the download. (default: false)
            "-tw" or "--download-tw": Adds TW files to the download. (default: false)
            "-o <folder>" or "--output-folder <folder>": Sets the output folder. (default: DownloadOutput)
            "-m <count>" or "--max-downloads <count>": Sets the maximum number of concurrent downloads. (default: 16)
            "-p <platform>" or "--platform <platform>": Sets the version of assets to download.
                Valid options: Android, iOS (default: Android)
        """;
}
