using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Handlers;
using AssetDownloader.Models;

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

    public static Arguments VerifyArguments(string[] args)
    {
        bool validArguments = true;
        string outputFolder = "DownloaderOutput";
        bool skipOldAssets = false;
        bool downloadEn = false;
        bool downloadEu = false;
        bool downloadCn = false;
        bool downloadTw = false;
        int maxConcurrent = 16;
        string platformName = Constants.Android;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h"
                or "--help":
                    validArguments = false;
                    break;
                case "-s"
                or "--skip-old-assets":
                    skipOldAssets = true;
                    break;
                case "-en"
                or "--download-en":
                    downloadEn = true;
                    break;
                case "-eu"
                or "--download-eu":
                    downloadEn = true;
                    downloadEu = true;
                    break;
                case "-cn"
                or "--download-cn":
                    downloadCn = true;
                    break;
                case "-tw"
                or "--download-tw":
                    downloadTw = true;
                    break;
                case "-o"
                or "--output-folder":
                    i++;
                    outputFolder = args[i];
                    break;
                case "-m"
                or "--max-downloads":
                    i++;
                    validArguments &= int.TryParse(args[i], out maxConcurrent);
                    break;
                case "-p"
                or "--platform":
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

        validArguments = validArguments && downloadEn | downloadEu | downloadCn | downloadTw;

        return new Arguments()
        {
            OutputFolder = outputFolder,
            SkipOldAssets = skipOldAssets,
            DownloadCn = downloadCn,
            DownloadEn = downloadEn,
            DownloadEu = downloadEu,
            DownloadTw = downloadTw,
            MaxConcurrent = maxConcurrent,
            PlatformName = platformName,
            IsValid = validArguments
        };
    }

    public static Arguments InteractiveArgs()
    {
        Arguments result = new();

        result.SkipOldAssets = InputPromptBool(
            "Do you want to only download the most recent version of each asset (cuts download from ~50GB to ~15GB)?"
                + "\nThis option is not recommended unless you have no space for the full download. It may make it difficult to access old event data."
        );

        string outputFolder = InputPromptString(
            $"Filepath for download result: (leave blank for ./DownloadOutput)"
        );

        if (string.IsNullOrWhiteSpace(outputFolder))
            outputFolder = "DownloadOutput";

        result.OutputFolder = outputFolder;

        result.MaxConcurrent = InputPromptInt(
            "Maximum concurrent downloads (leave blank for 16)",
            16
        );

        string platform = InputPromptString(
            "Target asset platform? (allowed values: 'iOS', 'Android', leave blank for Android)"
        );
        result.PlatformName = platform.ToLower() switch
        {
            "android" => Constants.Android,
            "ios" => Constants.Ios,
            _ => Constants.Android
        };

        result.DownloadEn = InputPromptBool("Download American English localisation assets?");
        result.DownloadEu = InputPromptBool(
            "Download international English localisation assets? (UK/Australia -- will also download American English)"
        );

        if (result.DownloadEu)
            result.DownloadEn = true;

        result.DownloadCn = InputPromptBool("Download Chinese localisation assets?");
        result.DownloadTw = InputPromptBool("Download Taiwanese localisation assets?");

        return result;
    }

    private static bool InputPromptBool(string prompt)
    {
        Console.Write($"{prompt} (y/n): ");
        return Console.ReadLine() == "y";
    }

    private static int InputPromptInt(string prompt, int @default)
    {
        Console.Write($"{prompt}: ");
        string? input = Console.ReadLine();
        return int.TryParse(input, out int result) ? result : @default;
    }

    private static string InputPromptString(string prompt)
    {
        Console.Write($"{prompt}: ");
        return Console.ReadLine() ?? throw new NullReferenceException();
    }

    public static void FriendlyExit()
    {
        Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}Press Enter to exit...");
        Console.ReadLine();

        Environment.Exit(0);
    }

    public static async Task CloneRepo()
    {
        HttpClientHandler handler = new() { AllowAutoRedirect = true };
        ProgressMessageHandler ph = new(handler);
        string total = $"{GetHumanReadableFilesize(Constants.RepoSizeBytes, 6)} MB";

        ph.HttpReceiveProgress += (_, args) =>
        {
            var percent = GetFormattedPercent(
                args.BytesTransferred,
                args.TotalBytes ?? Constants.RepoSizeBytes
            );

            var megabytes = $"{GetHumanReadableFilesize(args.BytesTransferred, 6)} MB";

            Console.Write($" - Download progress: {megabytes} of approx. {total} ({percent})\r");
        };

        HttpClient client = new(ph);

        using var response = await client.GetAsync(
            Constants.RepoUrl,
            HttpCompletionOption.ResponseHeadersRead
        );
        using var zipResponse = new ZipArchive(
            await response.Content.ReadAsStreamAsync(),
            ZipArchiveMode.Read
        );

        Console.WriteLine("\n - Unzipping download...");
        zipResponse.ExtractToDirectory(Constants.ClonedRepoFolder);
    }
}
