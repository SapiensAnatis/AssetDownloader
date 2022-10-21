using Newtonsoft.Json;
using AssetDownloader.Models;
using static AssetDownloader.Functions;
using static AssetDownloader.Constants;

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
DirectoryInfo[] manifestDirs = new DirectoryInfo(manifestPath).GetDirectories();

Directory.CreateDirectory("aria_session");

for (int i = 0; i < manifestDirs.Length; i++)
{
    DirectoryInfo directory = manifestDirs[i];
    string manifestName = directory.Name.Split("_")[1];
    string ariaFilePath = Path.Combine("aria_session", $"aria_input_{manifestName}.txt");

    Console.WriteLine($"Processing manifest {manifestName} ({i + 1}/{manifestDirs.Length})");

    if (File.Exists(ariaFilePath))
    {
        Console.WriteLine("Existing download detected, resuming...");
        await InvokeAria(ariaFilePath);
        continue;
    }

    using var fs = File.Open(ariaFilePath, FileMode.CreateNew);

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

        m.ManifestName = manifestName;

        WriteAriaFile(m, fs);
    }

    await InvokeAria(ariaFilePath);
}
