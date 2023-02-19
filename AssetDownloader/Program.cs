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

IEqualityComparer<AssetInfo> equalityComparer = parsedArgs.SkipOldAssets
    ? new FilenameEqualityComparer()
    : new HashEqualityComparer();

List<AssetInfo> ParseManifests(string platform)
{
    Console.WriteLine("\nParsing manifests...");

    var manifestPath = Path.Combine(
        Constants.ClonedRepoFolder,
        "DragaliaManifests-master",
        platform
    );

    var manifestDirs = new DirectoryInfo(manifestPath)
        .GetDirectories()
        .OrderByDescending(x => x.Name)
        .ToList();

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

        Console.Write(
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

    Console.WriteLine("\nFinished manifest parsing.\n");

    return finalAssets;
}

var androidFinalAssets = ParseManifests(Constants.Android);

var downloader = new Downloader(
    androidFinalAssets,
    parsedArgs.OutputFolder,
    Constants.Android,
    parsedArgs.MaxConcurrent
);
await downloader.DownloadFiles();

if (parsedArgs.DownloadIos)
{
    var hashEqualityComparer = new HashEqualityComparer();

    Console.WriteLine("Commencing iOS download...");

    var iosFinalAssets = ParseManifests(Constants.Ios);
    var sharedAssets = androidFinalAssets.Intersect(iosFinalAssets, hashEqualityComparer).ToList();

    long copyFilesize = sharedAssets.Sum(x => x.Size);
    string copyFilesizeStr = $"{Utils.GetHumanReadableFilesize(copyFilesize, 6)} MB";
    long copiedFilesize = 0;
    int copyCount = sharedAssets.Count;

    string androidFolder = Path.Join(parsedArgs.OutputFolder, Constants.Android);
    string iosFolder = Path.Join(parsedArgs.OutputFolder, Constants.Ios);

    Console.WriteLine(
        $"Copying assets shared with existing Android download... (to copy: {Utils.GetHumanReadableFilesize(copyFilesize, 6)} MB)"
    );

    foreach ((AssetInfo asset, int index) in sharedAssets.Select((value, index) => (value, index)))
    {
        Directory.CreateDirectory(Path.Join(iosFolder, asset.HashId));

        if (!Path.Exists(Path.Join(iosFolder, asset.DownloadPath)))
        {
            File.Copy(
                Path.Join(androidFolder, asset.DownloadPath),
                Path.Join(iosFolder, asset.DownloadPath)
            );
        }

        copiedFilesize += asset.Size;

        string countProgress =
            $"{index}/{copyCount} assets ({Utils.GetFormattedPercent(index, copyCount)})";
        string sizeProgress =
            $"{Utils.GetHumanReadableFilesize(copiedFilesize, 6)}/{copyFilesizeStr}";

        Console.Write($"Copying file {asset.Hash} {sizeProgress}, {countProgress}            \r");
    }

    Console.WriteLine();

    iosFinalAssets = iosFinalAssets.Except(sharedAssets, hashEqualityComparer).ToList();

    downloader = new Downloader(
        iosFinalAssets,
        parsedArgs.OutputFolder,
        Constants.Ios,
        parsedArgs.MaxConcurrent
    );
    await downloader.DownloadFiles();
}

await Console.Out.WriteLineAsync("Program finished.");
Utils.FriendlyExit();
