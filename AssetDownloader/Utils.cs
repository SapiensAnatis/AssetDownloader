using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Handlers;

namespace AssetDownloader;

public static class Utils
{
    public static string GetFormattedPercent(long numerator, long denominator)
    {
        return ((float)numerator / denominator).ToString("P2", CultureInfo.InvariantCulture);
    }

    public static string GetHumanReadableFilesize(long filesize, int magnitude)
    {
        return Math.Round(filesize / Math.Pow(10, magnitude), 1).ToString("0.0");
    }

    public static bool VerifyArguments(string[] args, out string outputFolder, out string platformName,
        out bool skipOldAssets, out bool downloadEn, out bool downloadCn, out bool downloadTw, out int maxConcurrent)
    {
        var validArguments = true;
        outputFolder = "DownloaderOutput";
        skipOldAssets = false;
        downloadEn = false;
        downloadCn = false;
        downloadTw = false;
        maxConcurrent = 16;
        platformName = Constants.Android;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help":
                    validArguments = false;
                    break;
                case "-s" or "--skip-old-assets":
                    skipOldAssets = true;
                    break;
                case "-en" or "--download-en":
                    downloadEn = true;
                    break;
                case "-cn" or "--download-cn":
                    downloadCn = true;
                    break;
                case "-tw" or "--download-tw":
                    downloadTw = true;
                    break;
                case "-o" or "--output-folder":
                    i++;
                    outputFolder = args[i];
                    break;
                case "-m" or "--max-downloads":
                    i++;
                    validArguments &= int.TryParse(args[i], out maxConcurrent);
                    break;
                case "-p" or "--platform":
                    i++;
                    var input = args[i].ToLower();
                    if (input == Constants.Android.ToLower())
                        platformName = Constants.Android;
                    else if (input == Constants.Ios.ToLower())
                        platformName = Constants.Ios;
                    else
                        validArguments = false;
                    break;
            }
        }

        return validArguments && downloadEn | downloadCn | downloadTw;
    }

    public static async Task CloneRepo()
    {
        HttpClientHandler handler = new() { AllowAutoRedirect = true };
        ProgressMessageHandler ph = new(handler);

        ph.HttpReceiveProgress += (_, args) =>
        {
            var percent = GetFormattedPercent(
                args.BytesTransferred,
                args.TotalBytes ?? Constants.RepoSizeBytes
            );

            var megabytes = $"{GetHumanReadableFilesize(args.BytesTransferred, 6)} MB";

            Console.Write($" - Download progress: {megabytes} of approx. 636 MB ({percent})\r");
        };

        HttpClient client = new(ph);

        using var response = await client.GetAsync(Constants.RepoUrl, HttpCompletionOption.ResponseHeadersRead);
        using var zipResponse = new ZipArchive(await response.Content.ReadAsStreamAsync(), ZipArchiveMode.Read);

        Console.WriteLine("\n - Unzipping download...");
        zipResponse.ExtractToDirectory(Constants.ClonedRepoFolder);
    }
}
