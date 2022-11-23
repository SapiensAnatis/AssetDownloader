using AssetDownloader.Models;
using AssetDownloader;
using Newtonsoft.Json;

AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    Exception e = (Exception)args.ExceptionObject;
    Console.WriteLine($"An unhandled exception occurred: {e}");

    if (args.IsTerminating)
        Utils.FriendlyExit();
};

Arguments parsedArgs = Utils.VerifyArguments(args);
if (!parsedArgs.IsValid)
{
    Console.WriteLine("Detected invalid command-line arguments. Starting interactive mode...");
    parsedArgs = Utils.InteractiveArgs();
}

Console.WriteLine();

if (!parsedArgs.IsValid)
{
    Console.WriteLine(
        "Invalid arguments detected. You must specify at least one localisation to download."
    );
    Console.Write(Constants.HelpText);
    Utils.FriendlyExit();
}

if (!parsedArgs.SkipOldAssets && parsedArgs.PlatformName == Constants.Ios)
{
    Console.WriteLine(
        "Error: Cannot download all iOS assets as only the latest manifest has been preserved."
    );
    Console.WriteLine("Please use the --skip-old-assets flag to acknowledge this issue.");
    Utils.FriendlyExit();
}

Console.WriteLine($"You have chosen the following options:{Environment.NewLine}");
Console.WriteLine($"{parsedArgs}{Environment.NewLine}");

// Download and unzip dl-datamine repository
if (!Directory.Exists(Constants.ClonedRepoFolder))
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

var manifestPath = Path.Combine(
    Constants.ClonedRepoFolder,
    "DragaliaManifests-master",
    parsedArgs.PlatformName
);

var manifestDirs = parsedArgs.SkipOldAssets
    ? new List<DirectoryInfo>
    {
        new(
            Path.Join(
                manifestPath,
                parsedArgs.PlatformName == Constants.Android
                    ? Constants.LatestAndroidManifestName
                    : Constants.LatestIosManifestName
            )
        )
    }
    : new DirectoryInfo(manifestPath).GetDirectories().OrderByDescending(x => x.Name).ToList();

await Console.Out.WriteLineAsync("Starting manifest parsing.");

for (int i = 0; i < manifestDirs.Count; i++)
{
    var directory = manifestDirs.ElementAt(i);
    var manifestName = directory.Name.Split("_")[1];

    await Console.Out.WriteAsync(
        $" - Parsing manifest {manifestName} ({i + 1}/{manifestDirs.Count})             \r"
    );

    List<string> paths = new() { Path.Combine(directory.FullName, "assetbundle.manifest.json") };

    if (parsedArgs.DownloadEu)
        paths.Add(Path.Combine(directory.FullName, "assetbundle.en_eu.manifest.json"));

    if (parsedArgs.DownloadEn)
        paths.Add(Path.Combine(directory.FullName, "assetbundle.en_us.manifest.json"));

    if (parsedArgs.DownloadCn)
        paths.Add(Path.Combine(directory.FullName, "assetbundle.zh_cn.manifest.json"));

    if (parsedArgs.DownloadTw)
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

var downloader = new Downloader(
    hashes,
    parsedArgs.OutputFolder,
    parsedArgs.PlatformName,
    parsedArgs.MaxConcurrent
);
await downloader.DownloadFiles();

await Console.Out.WriteLineAsync("Program finished.");
Utils.FriendlyExit();
