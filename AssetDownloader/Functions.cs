using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Handlers;
using System.Text;
using AssetDownloader.Models;
using static AssetDownloader.Constants;

namespace AssetDownloader;

public static class Functions
{
    public static async Task WriteAriaFile(Manifest m, FileStream file)
    {
        foreach (Asset a in m.AllAssets)
        {
            string id = $"{a.Hash[..2]}";

            string url = $"{BaseUrl}/{id}/{a.Hash}";
            string opts =
                $"\n\tdir={DownloadOutputFolder}/{Platform}/{m.ManifestName}/{id}"
                + $"\n\tout={a.Hash}";

            await file.WriteAsync(Encoding.UTF8.GetBytes(url + opts + "\n"));
        }
    }

    public static async Task CloneRepo()
    {
        HttpClientHandler handler = new() { AllowAutoRedirect = true };
        ProgressMessageHandler ph = new(handler);

        ph.HttpReceiveProgress += (_, args) =>
        {
            // GitHub doesn't send args.TotalBytes :(
            Console.Write(
                $"Download progress: {Math.Round((double)args.BytesTransferred / 1e6)}MB of approx. 636MB\r"
            );
        };

        var client = new HttpClient(ph);
        string zipFilepath = System.IO.Path.GetTempFileName();

        using (HttpResponseMessage response = await client.GetAsync(RepoUrl))
        {
            using FileStream fs = File.Open(zipFilepath, FileMode.Open);
            await response.Content.CopyToAsync(fs);
        }

        Console.WriteLine("\nUnzipping download...");
        using ZipArchive zip = ZipFile.OpenRead(zipFilepath);
        zip.ExtractToDirectory(ClonedRepoFolder);
    }

    public static async Task InvokeAria(string inputFile)
    {
        ProcessStartInfo startInfo =
            new()
            {
                FileName = "aria2c.exe",
                Arguments =
                    $"-i {inputFile} --save-session {inputFile} --stop-with-process {Environment.ProcessId} "
                    + AriaOptions,
                CreateNoWindow = false,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

        using Process p =
            Process.Start(startInfo) ?? throw new Exception("Failed to start aria2c.exe");
        await p.WaitForExitAsync();
    }
}
