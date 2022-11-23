using System.Diagnostics;

namespace AssetDownloader.Models;

public class Arguments
{
    public string OutputFolder { get; set; } = "DownloaderOutput";

    public bool SkipOldAssets { get; set; } = false;

    public bool DownloadEn { get; set; } = false;

    public bool DownloadEu { get; set; } = false;

    public bool DownloadCn { get; set; } = false;

    public bool DownloadTw { get; set; } = false;

    public int MaxConcurrent { get; set; } = 16;

    public string PlatformName { get; set; } = Constants.Android;

    private bool isValid = true;

    public bool IsValid
    {
        get => this.isValid & (this.DownloadEn | this.DownloadCn | this.DownloadTw);
        set => this.isValid = value;
    }

    public override string ToString()
    {
        return $"""
            Output folder: {Path.Join(Directory.GetCurrentDirectory(), this.OutputFolder)}
            Skip old assets: {this.SkipOldAssets}
            Download English: {this.DownloadEn}
            Download EU English: {this.DownloadEu}
            Download Chinese: {this.DownloadCn}
            Download Taiwanese: {this.DownloadTw}
            Maximum concurrent downloads: {this.MaxConcurrent}
            Asset platform: {this.PlatformName}
            """;
    }
}
