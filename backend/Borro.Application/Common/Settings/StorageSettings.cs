namespace Borro.Application.Common.Settings;

/// <summary>
/// Application-layer storage settings. Bound from configuration by Infrastructure
/// and injected via IOptions&lt;StorageSettings&gt;. Keeps Application layer free of
/// Infrastructure-specific option types.
/// </summary>
public sealed class StorageSettings
{
    public const string SectionName = "Minio";

    /// <summary>Default bucket for item images.</summary>
    public string ItemImagesBucket { get; set; } = "borro-assets";
}
