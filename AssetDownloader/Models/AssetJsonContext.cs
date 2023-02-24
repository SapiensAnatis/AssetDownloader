using System.Text.Json.Serialization;

namespace AssetDownloader.Models;

[JsonSerializable(typeof(AssetInfo))]
[JsonSerializable(typeof(AssetCategory))]
[JsonSerializable(typeof(Manifest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AssetJsonContext : JsonSerializerContext { }
