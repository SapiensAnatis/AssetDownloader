using Newtonsoft.Json;
using AssetDownloader.Models;
using static AssetDownloader.Functions;
using static AssetDownloader.Constants;
using System.Diagnostics;
using System.Text;

// Download and unzip dl-datamine repository
if (!Directory.Exists("dl-datamine"))
{
    Console.WriteLine("Downloading dl-datamine Git repository...");
    await CloneRepo();
}
else
{
    Console.WriteLine("Found existing dl-datamine Git repository.");
}

// Collect all hashes
HashSet<Asset> hashes = new();
Console.WriteLine("\nParsing manifests...");


{
    string manifestPath = Path.Combine("dl-datamine", "dl-datamine-master", "manifest");

    List<DirectoryInfo> manifestDirs = new DirectoryInfo(manifestPath)
        .GetDirectories()
        .OrderByDescending(x => x.Name)
        .ToList();

    for (int i = 0; i < manifestDirs.Count; i++)
    {
        DirectoryInfo directory = manifestDirs.ElementAt(i);
        string manifestName = directory.Name.Split("_")[1];

        Console.Write(
            $" - Parsing manifest {manifestName} ({i + 1}/{manifestDirs.Count})             \r"
        );

        List<string> paths =
            new() { Path.Combine(directory.FullName, "assetbundle.manifest.json") };

        if (Download_EN_US)
            paths.Add(Path.Combine(directory.FullName, "assetbundle.en_us.manifest.json"));

        if (Download_ZH_CN)
            paths.Add(Path.Combine(directory.FullName, "assetbundle.zh_cn.manifest.json"));

        if (Download_ZH_TW)
            paths.Add(Path.Combine(directory.FullName, "assetbundle.zh_tw.manifest.json"));

        foreach (string path in paths)
        {
            Manifest m =
                JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(path))
                ?? throw new JsonException("JSON deserialization failed");

            hashes.UnionWith(m.AllAssets);
        }
    }
}

int totalFiles = hashes.Count;

Console.WriteLine("\n\nDownloading...");

if (!File.Exists(AriaFilePath))
{
    using FileStream file = File.Create(AriaFilePath);
    // Create aria file
    foreach (string hash in hashes.Select(x => x.Hash))
    {
        string id = $"{hash[..2]}";

        string url = $"{BaseUrl}/{id}/{hash}";
        string opts = $"\n\tdir={DownloadOutputFolder}/{Platform}/{id}" + $"\n\tout={hash}";

        await file.WriteAsync(Encoding.UTF8.GetBytes(url + opts + "\n"));
    }
}
else
{
    // There is no strict need to have parsed the manifests in this case, but it is done anyway so that
    // the total filecount is known. Otherwise there is no way to estimate progress after resuming.
    Console.WriteLine(" - Existing session detected, resuming...");
}

// Free up some memory
hashes.Clear();

DirectoryInfo downloadDir = Directory.CreateDirectory($"{DownloadOutputFolder}/{Platform}");

ProcessStartInfo ariaInfo = CreateAriaProcess(AriaFilePath);
Process aria = Process.Start(ariaInfo) ?? throw new Exception("Failed to start aria2c.exe");
aria.OutputDataReceived += (_, args) =>
{
    if (!string.IsNullOrWhiteSpace(args.Data))
        Console.WriteLine(args.Data);
};

aria.BeginOutputReadLine();

long previousFileSize = 0;
while (!aria.HasExited)
{
    IEnumerable<FileInfo> downloadedFiles = downloadDir
        .EnumerateFiles("*.*", SearchOption.AllDirectories)
        .Where(x => !x.Name.Contains("aria2")); // Ignore aria2 temp files

    int currentFileCount = downloadedFiles.Count();

    long currentFileSize = downloadedFiles.Sum(x => x.Length);
    string writeSpeed = $"{GetHumanReadableFilesize(currentFileSize - previousFileSize, 3)} kB/s";
    previousFileSize = currentFileSize;

    string percent = GetFormattedPercent(currentFileCount, totalFiles);
    Console.Write(
        $" - {currentFileCount} / {totalFiles} files downloaded ({percent}) (write: {writeSpeed})                    \r"
    );

    await Task.Delay(1000);
}

// aria2 leaves behind an empty text file
File.Delete(AriaFilePath);

Console.WriteLine(
    $"\n\nDownload completed. Space taken up on disk: {GetHumanReadableFilesize(previousFileSize, 9)} GB."
);
