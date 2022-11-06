# Dragalia Lost Asset Downloader

Based on a Python script written by Ceris: https://gist.github.com/CerisWhite/bf160e54ab4b99668e4dc3a38f9185ea

Using archived file manifests, this program constructs URLs to the Dragalia Lost CDN and downloads asset files for archival purposes.

The expected download size, depending on the enabled localisations, is about 16GB.

The program's main responsibility is to fetch the manifest JSON and parse it -- the list of URLs is then handed off to [aria2](https://aria2.github.io/manual/en/html/aria2c.html#description). This tool is capable of resuming a partially-completed manifest download and it will also parallelize the downloads. **On Linux and Mac, you will need to install this**, and ensure that it can be invoked by typing `aria2c` in a terminal window. For Windows, the binary is bundled with the software.

## Configuration

Some useful configuration properties can be found in `AssetDownloader/Constants.cs`.
