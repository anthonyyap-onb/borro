namespace Borro.Application.Common.Interfaces;

/// <summary>
/// Abstraction over object storage (MinIO / S3).
/// Application layer depends on this interface; Infrastructure provides the implementation.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a file stream and returns the public URL to the stored object.
    /// </summary>
    /// <param name="bucketName">Destination bucket.</param>
    /// <param name="objectKey">Full object key (path) within the bucket.</param>
    /// <param name="stream">Content stream.</param>
    /// <param name="contentType">MIME type of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Public URL of the uploaded object.</returns>
    Task<string> UploadAsync(
        string bucketName,
        string objectKey,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);
}
