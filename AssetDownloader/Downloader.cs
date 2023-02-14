using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using AssetDownloader.Models;

namespace AssetDownloader;

public class Downloader
{
    private SemaphoreSlim _downloadSemaphore;

    private readonly string _downloadFolder;
    private readonly HttpClient _downloadClient;

    private long _downloadedBytes;
    private long _downloadedAssets;

    private readonly IEnumerable<AssetInfo> _assets;
    private readonly ConcurrentBag<AssetInfo> _failedAssets;

    public Downloader(
        IEnumerable<AssetInfo> assets,
        string downloadFolder,
        string platform,
        int maxConcurrent = 16
    )
    {
        _assets = assets;
        _failedAssets = new ConcurrentBag<AssetInfo>();

        _downloadFolder = Path.GetFullPath(downloadFolder) + Path.DirectorySeparatorChar;
        _downloadSemaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);

        _downloadClient = new HttpClient();
        _downloadClient.BaseAddress = new Uri(Constants.BaseUrl + platform + '/');
        _downloadClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "UnityPlayer/2019.4.31f1 (UnityWebRequest/1.0, libcurl/7.75.0-DEV)"
        );

        _downloadedBytes = 0;
        _downloadedAssets = 0;
    }

    public async Task DownloadFiles()
    {
        await Console.Out.WriteLineAsync(
            "Filtering out unneeded assets. If the script has already run, this will take a long time."
        );

        var currentDownloadedAssets = _assets
            .AsParallel()
            .WithDegreeOfParallelism(_downloadSemaphore.CurrentCount)
            .Where(
                asset =>
                    !File.Exists(_downloadFolder + asset.DownloadPath)
                    || !VerifyFileHash(
                        File.ReadAllBytes(_downloadFolder + asset.DownloadPath),
                        asset.HashBytes
                    )
            )
            .ToList();

        await Console.Out.WriteLineAsync("Creating directories.");

        foreach (var hashPrefix in currentDownloadedAssets.Select(asset => asset.HashId).Distinct())
            Directory.CreateDirectory(_downloadFolder + hashPrefix);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            _downloadedBytes = 0;
            _downloadedAssets = 0;
            _failedAssets.Clear();

            var totalBytesString = Utils.GetHumanReadableFilesize(
                currentDownloadedAssets.Sum(asset => asset.Size),
                6
            );

            var totalAssets = currentDownloadedAssets.Count;

            await Console.Out.WriteLineAsync("Starting asset download threads.");

            var tasks = currentDownloadedAssets
                .AsParallel()
                .WithDegreeOfParallelism(_downloadSemaphore.CurrentCount)
                .Select(asset => DownloadFile(asset, _downloadFolder + asset.DownloadPath))
                .ToList();

            await Console.Out.WriteLineAsync("Asset download started.");

            while (_downloadedAssets + _failedAssets.Count != totalAssets)
            {
                await Console.Out.WriteAsync(
                    " - Download progress: "
                        + $"{Utils.GetHumanReadableFilesize(_downloadedBytes, 6)}/{totalBytesString} MB, "
                        + $"({_downloadedAssets}/{totalAssets}) Assets, "
                        + $"{Utils.GetFormattedPercent(_downloadedAssets, totalAssets)} "
                        + $"({stopwatch.Elapsed:hh\\:mm\\:ss})"
                        + "              \r"
                );

                await Task.Delay(10);
            }

            if (!_failedAssets.IsEmpty)
            {
                await Console.Out.WriteLineAsync(
                    "\nSome assets failed to download. Retrying them."
                );
                currentDownloadedAssets = _failedAssets.ToList();

                // Known issue: if only a few large files remain, then the downloader may fail in a loop as it
                // is unable to download them sequentially within the timeout window.
                _downloadSemaphore = new SemaphoreSlim(1, 1);
                _downloadClient.Timeout *= 2;
            }
        } while (!_failedAssets.IsEmpty);

        stopwatch.Stop();
        await Console.Out.WriteLineAsync(
            $"\nAsset download completed. Time elapsed: {stopwatch.Elapsed:hh\\:mm\\:ss}"
        );
    }

    private static bool VerifyFileHash(byte[] fileData, byte[] expectedHash)
    {
        var actualHash = SHA256.HashData(fileData);
        return actualHash.SequenceEqual(expectedHash);
    }

    private async Task DownloadFile(AssetInfo asset, string filePath)
    {
        await _downloadSemaphore.WaitAsync();

        while (true)
        {
            try
            {
                var fileData = await _downloadClient.GetByteArrayAsync(asset.DownloadPath);
                if (VerifyFileHash(fileData, asset.HashBytes))
                {
                    Interlocked.Add(ref _downloadedBytes, fileData.LongLength);
                    _downloadSemaphore.Release(); // File I/O is slow so we don't wait for it

                    await File.WriteAllBytesAsync(filePath, fileData);
                    Interlocked.Increment(ref _downloadedAssets); // Only increment this now so we make sure that the file has been written prior to exiting
                    return;
                }

                await Console.Out.WriteLineAsync(
                    $"File {asset.DownloadPath} was downloaded but did not have the proper hash, retrying."
                );
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(
                    $"Failed to download file {asset.DownloadPath}. This download will be retried later."
                );
                await Console.Out.WriteLineAsync($"(Cause: {ex.GetType().Name})");
#if DEBUG // Runs if the app is built in 'Debug' mode
                await Console.Out.WriteLineAsync($"{ex}");
#endif

                _failedAssets.Add(asset);
                _downloadSemaphore.Release();
                return;
            }
        }
    }
}
