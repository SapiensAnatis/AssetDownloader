# Dragalia Lost Asset Downloader

Download from the [releases page](https://github.com/SapiensAnatis/AssetDownloader/releases/latest)

Based on a Python script written by Ceris: https://gist.github.com/CerisWhite/bf160e54ab4b99668e4dc3a38f9185ea

Using archived file manifests, this program constructs URLs to the Dragalia Lost CDN and downloads asset files for archival purposes.

The expected download size, depending on the enabled localisations, is about 16GB.

The program's main responsibility is to fetch the manifest JSON, parse it, and download the contained files. This tool is capable of resuming a partially-completed manifest download and it will also parallelize the downloads.

## Requirements

- [.NET 7 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) installed 

## Configuration

```
Usage: AssetDownloader.exe <arguments>
Valid arguments:
    "-h" or "--help": Prints this help text.
    "-s" or "--skip-old-assets": Only downloads the newest assets. (default: false)
    "-en" or "--download-en": Adds EN files to the download. (default: false)
    "-cn" or "--download-cn": Adds CN files to the download. (default: false)
    "-tw" or "--download-tw": Adds TW files to the download. (default: false)
    "-o <folder>" or "--output-folder <folder>": Sets the output folder. (default: DownloadOutput)
    "-m <count>" or "--max-downloads <count>": Sets the maximum number of concurrent downloads. (default: 16)
    "-p <platform>" or "--platform <platform>": Sets the version of assets to download.
        Valid options: Android, iOS (default: Android)
```