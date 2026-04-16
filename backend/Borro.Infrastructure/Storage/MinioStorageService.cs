using Amazon.S3;
using Amazon.S3.Model;
using Borro.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Borro.Infrastructure.Storage;

/// <summary>
/// IStorageService implementation backed by MinIO via the AWS S3 SDK.
/// ForcePathStyle=true is required for MinIO compatibility.
/// </summary>
public sealed class MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(
        IAmazonS3 s3Client,
        IOptions<MinioOptions> options,
        ILogger<MinioStorageService> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        string bucketName,
        string objectKey,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(bucketName, cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
            // Make the object publicly readable.
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        var publicUrl = $"{_options.PublicEndpoint.TrimEnd('/')}/{bucketName}/{objectKey}";

        _logger.LogInformation(
            "Uploaded object '{ObjectKey}' to bucket '{Bucket}'. URL: {Url}",
            objectKey, bucketName, publicUrl);

        return publicUrl;
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
        if (!exists)
        {
            await _s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true
            }, cancellationToken);

            _logger.LogInformation("Created MinIO bucket '{Bucket}'.", bucketName);
        }
    }
}
