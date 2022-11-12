using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using AssetDownloader.Models;

namespace AssetDownloader;

public class Downloader
{
    private readonly SemaphoreSlim _downloadSemaphore;

    private readonly string _downloadFolder;
    private readonly HttpClient _downloadClient;

    private long _downloadedBytes;
    private long _downloadedAssets;

    private readonly ISet<AssetInfo> _assets;

    public Downloader(ISet<AssetInfo> assets, string downloadFolder, string platform, int maxConcurrent = 16)
    {
        _assets = assets;

        _downloadFolder = Path.GetFullPath(downloadFolder) + Path.DirectorySeparatorChar;
        _downloadSemaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);

        _downloadClient = new HttpClient();
        _downloadClient.BaseAddress = new Uri(Constants.BaseUrl + platform + '/');
        _downloadClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            "UnityPlayer/2019.4.31f1 (UnityWebRequest/1.0, libcurl/7.75.0-DEV)");

        _downloadedBytes = 0;
        _downloadedAssets = 0;
    }

    public async Task DownloadFiles()
    {
        await Console.Out.WriteLineAsync("Filtering out unneeded assets.");
        var newAssets = _assets
            .Where(asset => !File.Exists(_downloadFolder + asset.DownloadPath) ||
                            !VerifyFileHash(File.ReadAllBytes(_downloadFolder + asset.DownloadPath), asset.HashBytes))
            .ToList();

        var totalBytesString = Utils.GetHumanReadableFilesize(newAssets.Sum(asset => asset.Size), 6);
        var totalAssets = newAssets.Count;

        await Console.Out.WriteLineAsync("Creating directories.");

        foreach (var hashPrefix in newAssets.Select(asset => asset.HashId).Distinct())
            Directory.CreateDirectory(_downloadFolder + hashPrefix);

        await Console.Out.WriteLineAsync("Starting asset download threads.");

        var tasks = newAssets.Select(asset =>
                DownloadFile(asset.DownloadPath, _downloadFolder + asset.DownloadPath, asset.HashBytes,
                    asset.Size))
            .ToList();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await Console.Out.WriteLineAsync("Asset download started.");

        while (_downloadedAssets != totalAssets)
        {
            await Console.Out.WriteAsync(
                    $" - Download progress: " +
                    $"{Utils.GetHumanReadableFilesize(_downloadedBytes, 6)}/{totalBytesString} MB " +
                    $"({_downloadedAssets}/{totalAssets}) " +
                    "              \r");

           await Task.Delay(10);
        }

        stopwatch.Stop();
        await Console.Out.WriteLineAsync($"\nAsset download completed. Time elapsed: {stopwatch.Elapsed}");
    }

    private static bool VerifyFileHash(byte[] fileData, byte[] expectedHash)
    {
        var actualHash = SHA256.HashData(fileData);
        return actualHash.SequenceEqual(expectedHash);
    }

    public async Task DownloadFile(string downloadPath, string filePath, byte[] expectedHash, long fileSize)
    {
        await _downloadSemaphore.WaitAsync();

        while (true)
        {
            try
            {
                var fileData = await _downloadClient.GetByteArrayAsync(downloadPath);
                if (VerifyFileHash(fileData, expectedHash))
                {
                    Interlocked.Add(ref _downloadedBytes, fileData.LongLength);
                    _downloadSemaphore.Release();

                    await File.WriteAllBytesAsync(filePath, fileData);
                    return;
                }

                await Console.Out.WriteLineAsync(
                    $"File {downloadPath} was downloaded but did not have the proper hash, retrying.");
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Failed to download file {downloadPath}. Exception: {ex}");
                _downloadSemaphore.Release();
            }
        }
    }
}