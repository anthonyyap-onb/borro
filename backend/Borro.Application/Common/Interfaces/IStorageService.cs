namespace Borro.Application.Common.Interfaces;

public interface IStorageService
{
    /// <summary>Uploads a file and returns its public URL.</summary>
    Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
}
