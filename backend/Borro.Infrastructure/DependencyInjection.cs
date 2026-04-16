using Amazon.S3;
using Borro.Application.Common.Interfaces;
using Borro.Infrastructure.Persistence;
using Borro.Infrastructure.Services;
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
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<BorroDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<BorroDbContext>());
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IGoogleTokenVerifier, GoogleTokenVerifier>();
        services.AddScoped<DatabaseSeeder>();

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var minioUrl = configuration["MinIO:ServiceUrl"] ?? "http://localhost:9000";
            var accessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
            var secretKey = configuration["MinIO:SecretKey"] ?? "minioadmin";

            var config = new Amazon.S3.AmazonS3Config
            {
                ServiceURL = minioUrl,
                ForcePathStyle = true
            };
            return new Amazon.S3.AmazonS3Client(accessKey, secretKey, config);
        });
        services.AddScoped<IStorageService, MinioStorageService>();

        return services;
    }
}
