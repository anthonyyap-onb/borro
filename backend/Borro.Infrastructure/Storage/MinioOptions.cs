using System.ComponentModel.DataAnnotations;

namespace Borro.Infrastructure.Storage;

/// <summary>
/// Configuration options for the MinIO / S3-compatible storage service.
/// Bound from the "Minio" section in appsettings.json.
/// </summary>
public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    /// <summary>Internal endpoint used by the backend container to reach MinIO (e.g. http://minio:9000).</summary>
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Public-facing endpoint used to build object URLs returned to clients (e.g. http://localhost:9000).</summary>
    [Required]
    public string PublicEndpoint { get; set; } = string.Empty;

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Default bucket for item images.</summary>
    [Required]
    public string BucketName { get; set; } = "borro-assets";
}
