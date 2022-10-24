using Newtonsoft.Json;
using AssetDownloader.Models;
using static AssetDownloader.Functions;
using static AssetDownloader.Constants;
using System.Diagnostics;
using System.Globalization;

// Download and unzip dl-datamine repository
if (!Directory.Exists("dl-datamine"))
{
    Console.WriteLine("Downloading dl-datamine git repository...");
    await CloneRepo();
}
else
{
    Console.WriteLine("Found existing dl-datamine git repository.");
}

string manifestPath = Path.Combine("dl-datamine", "dl-datamine-master", "manifest");
IEnumerable<DirectoryInfo> manifestDirs = new DirectoryInfo(manifestPath)
    .GetDirectories()
    .OrderByDescending(x => x.Name);

Directory.CreateDirectory("aria_session");

for (int i = 0; i < manifestDirs.Count(); i++)
{
    DirectoryInfo directory = manifestDirs.ElementAt(i);
    string manifestName = directory.Name.Split("_")[1];
    string ariaFilePath = Path.Combine("aria_session", $"aria_input_{manifestName}");

    Console.WriteLine($"Processing manifest {manifestName} ({i + 1}/{manifestDirs.Count()})");

    if (!File.Exists(ariaFilePath))
    {
        List<string> paths =
            new() { Path.Combine(directory.FullName, "assetbundle.manifest.json") };

        if (Download_EN_US)
            paths.Add(Path.Combine(directory.FullName, "assetbundle.en_us.manifest.json"));

        if (Download_ZH_CN)
            paths.Add(Path.Combine(directory.FullName, "assetbundle.zh_cn.manifest.json"));

        if (Download_ZH_TW)
            paths.Add(Path.Combine(directory.FullName, "assetbundle.zh_tw.manifest.json"));

        using var fs = File.Create(ariaFilePath);
        foreach (string path in paths)
        {
            Manifest m =
                JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(path))
                ?? throw new JsonException("JSON deserialization failed");

            m.ManifestName = manifestName;

            await WriteAriaFile(m, fs);
        }
    }
    else
    {
        Console.WriteLine("\tExisting session detected, resuming...");
    }

    // Each URL is one file
    int totalFiles = File.ReadLines(ariaFilePath)
        .Select(x => x.Contains("dragalialost.akamaized.net"))
        .Count();

    // Make the directory before aria does, because we might try and count before the first download completes
    DirectoryInfo downloadDir = Directory.CreateDirectory(
        $"{DownloadOutputFolder}/{Platform}/{manifestName}"
    );

    ProcessStartInfo ariaInfo = CreateAriaProcess(ariaFilePath);
    Process aria = Process.Start(ariaInfo) ?? throw new Exception("Failed to start aria2c.exe");

    while (!aria.HasExited)
    {
        int downloadedFiles = downloadDir
            .EnumerateFiles("*.*", SearchOption.AllDirectories)
            .Where(x => !x.Name.Contains("aria2")) // Ignore aria2 temp files, otherwise progress bar jumps up and down
            .Count();

        string percent = ((float)downloadedFiles / totalFiles).ToString(
            "P2",
            CultureInfo.InvariantCulture
        );
        Console.Write($"\t{downloadedFiles} / {totalFiles} files downloaded ({percent})\r");

        await Task.Delay(1000);
    }
}
