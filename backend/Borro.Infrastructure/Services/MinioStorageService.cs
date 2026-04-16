using Amazon.S3;
using Amazon.S3.Model;
using Borro.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Borro.Infrastructure.Services;

public class MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _publicBaseUrl;

    public MinioStorageService(IAmazonS3 s3, IConfiguration config)
    {
        _s3 = s3;
        _publicBaseUrl = config["MinIO:PublicBaseUrl"] ?? "http://localhost:9000/borro-assets";
    }

    public async Task<string> UploadAsync(
        string bucketName, string objectKey, Stream stream, string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3.PutObjectAsync(request, cancellationToken);
        return $"{_publicBaseUrl}/{objectKey}";
    }
}
