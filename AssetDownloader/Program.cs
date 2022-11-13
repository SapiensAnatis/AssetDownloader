using AssetDownloader.Models;
using AssetDownloader;
using Newtonsoft.Json;

if (!Utils.VerifyArguments(args, 
        out var outputFolder, out var platformName,
        out var skipOldAssets, out var downloadEn,
        out var downloadCn, out var downloadTw,
        out var maxConcurrent)
    )
{
    Console.WriteLine(Constants.HelpText);
    return;
}

if (!skipOldAssets && platformName == Constants.Ios)
{
    Console.WriteLine("Error: Cannot download all iOS assets as only the latest manifest has been preserved.");
    Console.WriteLine("Please use the --skip-old-assets flag to acknowledge this issue.");
    return;
}

// Download and unzip dl-datamine repository
if (!Directory.Exists("dl-datamine"))
{
    Console.WriteLine("Downloading required manifests from the dl-datamine repository...");
    await Utils.CloneRepo();
}
else
{
    await Console.Out.WriteLineAsync("Found existing manifests. Skipping download.");
}

// Collect all hashes
HashSet<AssetInfo> hashes = new();
await Console.Out.WriteLineAsync("\nParsing manifests...");

var manifestPath = Path.Combine("DragaliaManifests", "DragaliaManifests-master", platformName);

var manifestDirs = skipOldAssets
    ? new List<DirectoryInfo> {new(Path.Join(manifestPath, Constants.LatestManifestName))}
    : new DirectoryInfo(manifestPath)
        .GetDirectories()
        .OrderByDescending(x => x.Name)
        .ToList();

await Console.Out.WriteLineAsync("Starting manifest parsing.");

for (int i = 0; i < manifestDirs.Count; i++)
{
    var directory = manifestDirs.ElementAt(i);
    var manifestName = directory.Name.Split("_")[1];

    await Console.Out.WriteAsync(
        $" - Parsing manifest {manifestName} ({i + 1}/{manifestDirs.Count})             \r"
    );

    List<string> paths =
        new() { Path.Combine(directory.FullName, "assetbundle.manifest.json") };

    if (downloadEn)
        paths.Add(Path.Combine(directory.FullName, "assetbundle.en_us.manifest.json"));

    if (downloadCn)
        paths.Add(Path.Combine(directory.FullName, "assetbundle.zh_cn.manifest.json"));

    if (downloadTw)
        paths.Add(Path.Combine(directory.FullName, "assetbundle.zh_tw.manifest.json"));

    foreach (string path in paths)
    {
        var m =
            JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(path))
            ?? throw new JsonException("JSON deserialization failed.");

        hashes.UnionWith(m.AllAssets);
    }
}

await Console.Out.WriteLineAsync("\nFinished manifest parsing.\n");

var downloader = new Downloader(hashes, outputFolder, platformName, maxConcurrent);
await downloader.DownloadFiles();

await Console.Out.WriteLineAsync("Program finished.");