using Amazon.S3;
using Amazon.S3.Model;
using Borro.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Borro.Infrastructure.Services;

public class MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string _publicBaseUrl;

    public MinioStorageService(IAmazonS3 s3, IConfiguration config)
    {
        _s3 = s3;
        _bucket = config["MinIO:Bucket"] ?? "borro-assets";
        _publicBaseUrl = config["MinIO:PublicBaseUrl"] ?? "http://localhost:9000/borro-assets";
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = fileName,
            InputStream = fileStream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3.PutObjectAsync(request, ct);
        return $"{_publicBaseUrl}/{fileName}";
    }
}
