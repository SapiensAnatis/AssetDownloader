using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Handlers;
using static AssetDownloader.Constants;

namespace AssetDownloader;

public static class Functions
{
    public static string GetFormattedPercent(long numerator, long denominator)
    {
        return ((float)numerator / denominator).ToString("P2", CultureInfo.InvariantCulture);
    }

    public static string GetHumanReadableFilesize(long filesize, int magnitude)
    {
        return Math.Round(filesize / Math.Pow(10, magnitude), 1).ToString("0.0");
    }

    public static async Task CloneRepo()
    {
        HttpClientHandler handler = new() { AllowAutoRedirect = true };
        ProgressMessageHandler ph = new(handler);

        ph.HttpReceiveProgress += (_, args) =>
        {
            string percent = GetFormattedPercent(
                args.BytesTransferred,
                args.TotalBytes ?? RepoSizeBytes
            );

            string megabytes = $"{GetHumanReadableFilesize(args.BytesTransferred, 6)} MB";

            Console.Write($" - Download progress: {megabytes} of approx. 636 MB ({percent})\r");
        };

        HttpClient client = new(ph);
        string zipFilepath = Path.GetTempFileName();

        {
            using HttpResponseMessage response = await client.GetAsync(RepoUrl);
            using FileStream fs = File.Open(zipFilepath, FileMode.Open);

            await response.Content.CopyToAsync(fs);
        }

        Console.WriteLine("\n - Unzipping download...");
        ZipFile.ExtractToDirectory(zipFilepath, ClonedRepoFolder);
    }

    public static ProcessStartInfo CreateAriaProcess(string inputFile)
    {
        string ariaBinary = "aria2c";

        if (OperatingSystem.IsWindows())
            ariaBinary = "aria2c.exe";

        ProcessStartInfo startInfo =
            new()
            {
                FileName = ariaBinary,
                Arguments =
                    $"-i {inputFile} "
                    + $"--save-session {inputFile} "
                    + $"--stop-with-process {Environment.ProcessId} "
                    + AriaOptions,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

        return startInfo;
    }
}
