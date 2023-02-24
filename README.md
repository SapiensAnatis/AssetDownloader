# Dragalia Lost Asset Downloader

## Installation

Download from the [releases page](https://github.com/SapiensAnatis/AssetDownloader/releases/latest)

NOTE: On newer versions of OS X, app signing requirements may prevent you from running the binary directly. If you encounter issues, download the source code and the .NET SDK, and use `dotnet run AssetDownloader.csproj`.

## About

This is a program to download assets from the Dragalia Lost content distribution network (i.e. the files the game downloads during the in-app loading screen) for archival purposes.
Using archived file manifests, this program constructs the URLs used by the game and downloads the files to your PC.

Based on a Python script written by Ceris: https://gist.github.com/CerisWhite/bf160e54ab4b99668e4dc3a38f9185ea

### Expected download sizes

The expected download size depends on the options you choose. Broadly speaking, you can expect a download of about 15 to 20GB if skipping old assets, and 40 to 50GB otherwise. Some example configurations and their resultant download size are listed below.

| Localisations       | Skipping old assets | With iOS | Size |
|---------------------|---------------------|----------|------|
| en_us, en_eu        | No                  | No       | 43GB |
| en_us               | Yes                 | Yes      | 37GB |
| en_us, en_eu, zh_cn | Yes                 | No       | 19GB |  
| en_us               | Yes                 | No       | 18GB |

### FAQ

> Why isn't there an option to download the Japanese localisation?

The Japanese localisation files are included in the core game files list and are always downloaded.

> What does 'downloading the most recent version of each asset', from the first prompt, mean?

It determines what the downloader will do when it encounters a file which has the same name as one it has already downloaded, but whose contents are different. The downloader starts at the most recent files, so by skipping files with identical names but different contents, it grabs only the most recent version of any given file. These identically named files are likely to be files that have been updated and are not in use anymore, such as v1.0 models.

> I have an iOS device and I only want to download iOS assets. Why do I have to download Android assets first?

The program works by parsing lists of files that the app has historically used. Thanks to the efforts of the Dragalia Lost Wiki team, file lists for Android have been archived as far back as mid 2019. However, for iOS, only the two most recent file lists dating back to early 2022 have been archived. This means that if you were to only download iOS assets, you would be missing a significant amount of old event data. In the interests of not putting anyone in this position, Android assets must be downloaded.

## Command-line arguments

By default, the program will start in a guided mode and help you select your options. However, for advanced users, it is possible to skip this by passing in command-line flags.

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
    "-ios" or "--download-ios": Toggles additional downloading of iOS assets.
```
