using Newtonsoft.Json;
using AssetDownloader.Models;
using static AssetDownloader.Functions;
using static AssetDownloader.Constants;
using System.Diagnostics;
using System.Globalization;
using System.Text;

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

List<DirectoryInfo> manifestDirs = new DirectoryInfo(manifestPath)
    .GetDirectories()
    .OrderByDescending(x => x.Name)
    .ToList();

// Collect all hashes
HashSet<string> hashes = new();
for (int i = 0; i < manifestDirs.Count(); i++)
{
    DirectoryInfo directory = manifestDirs.ElementAt(i);
    string manifestName = directory.Name.Split("_")[1];

    Console.Write(
        $"Reading manifest {manifestName} ({i + 1}/{manifestDirs.Count()})             \r"
    );

    List<string> paths = new() { Path.Combine(directory.FullName, "assetbundle.manifest.json") };

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

        hashes.UnionWith(m.AllAssets.Select(x => x.Hash));
    }
}

Console.WriteLine();
int totalFiles = hashes.Count;

if (!File.Exists(AriaFilePath))
{
    using FileStream file = File.Create(AriaFilePath);
    // Create aria file
    foreach (string hash in hashes)
    {
        string id = $"{hash[..2]}";

        string url = $"{BaseUrl}/{id}/{hash}";
        string opts = $"\n\tdir={DownloadOutputFolder}/{Platform}/{id}" + $"\n\tout={hash}";

        await file.WriteAsync(Encoding.UTF8.GetBytes(url + opts + "\n"));
    }
}
else
{
    Console.WriteLine("Existing session detected, resuming...");
}

// Free up some memory
hashes.Clear();
manifestDirs.Clear();

// Make the directory before aria does, because we might try and count before the first download completes
DirectoryInfo downloadDir = Directory.CreateDirectory($"{DownloadOutputFolder}/{Platform}");

ProcessStartInfo ariaInfo = CreateAriaProcess(AriaFilePath);
Process aria = Process.Start(ariaInfo) ?? throw new Exception("Failed to start aria2c.exe");
aria.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
{
    if (!string.IsNullOrWhiteSpace(e.Data))
    {
        Console.WriteLine(e.Data);
    }
};
aria.BeginOutputReadLine();

Console.WriteLine("Commencing download...");
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
    Console.Write($"{downloadedFiles} / {totalFiles} files downloaded ({percent})\r");

    await Task.Delay(1000);
}
