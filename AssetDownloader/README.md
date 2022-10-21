# Dragalia Lost Asset Downloader

Based on a Python script written by Ceris: https://gist.github.com/CerisWhite/bf160e54ab4b99668e4dc3a38f9185ea

Using archived file manifests, this program constructs URLs to the Dragalia Lost CDN and downloads asset files for archival purposes.

The total size of the master assets is about 38GB. The English localisation assets are about 5GB.

The program's main responsibility is to fetch the manifest JSON and parse it -- the list of URLs is then handed off to [aria2](https://aria2.github.io/manual/en/html/aria2c.html#description). It is capable of of resuming a partially-completed manifest download and it will also parallelize the downloads.

