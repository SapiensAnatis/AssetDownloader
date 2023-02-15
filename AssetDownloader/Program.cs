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
    Console.WriteLine("Could not read command-line arguments. Starting interactive mode...");
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

if (parsedArgs is { SkipOldAssets: false, PlatformName: Constants.Ios })
{
    Console.WriteLine(
        "Error: Cannot download all iOS assets as only the latest manifest has been preserved."
    );
    Console.WriteLine(
        "Please use the --skip-old-assets flag or select 'n' to the first question to acknowledge this issue."
    );
    Utils.FriendlyExit();
}

Console.WriteLine($"You have chosen the following options:{Environment.NewLine}");
Console.WriteLine($"{parsedArgs}{Environment.NewLine}");
Console.WriteLine(
    "Press Enter to begin the download, or restart the program if you wish to choose different options."
);
Console.ReadLine();

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

await Console.Out.WriteLineAsync("\nParsing manifests...");

var manifestPath = Path.Combine(
    Constants.ClonedRepoFolder,
    "DragaliaManifests-master",
    parsedArgs.PlatformName
);

var manifestDirs = new DirectoryInfo(manifestPath)
    .GetDirectories()
    .OrderByDescending(x => x.Name)
    .ToList();

IEqualityComparer<AssetInfo> equalityComparer = parsedArgs.SkipOldAssets
    ? new FilenameEqualityComparer()
    : new HashEqualityComparer();

await Console.Out.WriteLineAsync("Starting manifest parsing.");

// Collect all hashes
Dictionary<string, HashSet<AssetInfo>> localeHashmaps =
    new() { { Constants.JpManifest, new HashSet<AssetInfo>(equalityComparer) } };

// Separate hashmaps per localisation to avoid overwriting across languages
if (parsedArgs.DownloadEn)
    localeHashmaps.Add(Constants.EnManifest, new HashSet<AssetInfo>(equalityComparer));
if (parsedArgs.DownloadEu)
    localeHashmaps.Add(Constants.EuManifest, new HashSet<AssetInfo>(equalityComparer));
if (parsedArgs.DownloadCn)
    localeHashmaps.Add(Constants.CnManifest, new HashSet<AssetInfo>(equalityComparer));
if (parsedArgs.DownloadTw)
    localeHashmaps.Add(Constants.TwManifest, new HashSet<AssetInfo>(equalityComparer));

for (int i = 0; i < manifestDirs.Count; i++)
{
    var currentManifestDir = manifestDirs.ElementAt(i);
    var manifestName = currentManifestDir.Name.Split("_")[1];

    await Console.Out.WriteAsync(
        $" - Parsing manifest {manifestName} ({i + 1}/{manifestDirs.Count})             \r"
    );

    foreach ((string filename, HashSet<AssetInfo> assetCollection) in localeHashmaps)
    {
        string path = Path.Join(currentManifestDir.FullName, filename);
        Manifest m =
            JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(path))
            ?? throw new NullReferenceException("JSON deserialization failure");

        assetCollection.UnionWith(m.AllAssets);
    }
}

// Combine disparate hashmaps
List<AssetInfo> finalAssets = new();
foreach ((string _, HashSet<AssetInfo> assetCollection) in localeHashmaps)
    finalAssets.AddRange(assetCollection);

await Console.Out.WriteLineAsync("\nFinished manifest parsing.\n");

var downloader = new Downloader(
    finalAssets,
    parsedArgs.OutputFolder,
    parsedArgs.PlatformName,
    parsedArgs.MaxConcurrent
);
await downloader.DownloadFiles();

await Console.Out.WriteLineAsync("Program finished.");
Utils.FriendlyExit();
