using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Borro.Application.Common.Interfaces;
using Borro.Application.Common.Settings;
using Borro.Infrastructure.Persistence;
using Borro.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Borro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ───────────────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<BorroDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register the context against the interface used by Application handlers.
        services.AddScoped<IBorroDbContext>(sp => sp.GetRequiredService<BorroDbContext>());

        services.AddScoped<DatabaseSeeder>();

        // ── MinIO / Object Storage ─────────────────────────────────────────────────
        // Application-layer storage settings (bucket name, etc.)
        services.AddOptions<StorageSettings>()
            .Bind(configuration.GetSection(StorageSettings.SectionName))
            .Configure(s => s.ItemImagesBucket =
                configuration[$"{StorageSettings.SectionName}:BucketName"] ?? s.ItemImagesBucket);

        services.AddOptions<MinioOptions>()
            .Bind(configuration.GetSection(MinioOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var minioOptions = configuration
            .GetSection(MinioOptions.SectionName)
            .Get<MinioOptions>()
            ?? throw new InvalidOperationException("MinIO configuration section is missing.");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = minioOptions.Endpoint,
            ForcePathStyle = true,   // Required for MinIO
            AuthenticationRegion = "us-east-1"
        };

        var credentials = new BasicAWSCredentials(minioOptions.AccessKey, minioOptions.SecretKey);
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(credentials, s3Config));
        services.AddScoped<IStorageService, MinioStorageService>();

        return services;
    }
}

