using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Borro.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly BorroDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(BorroDbContext context, TimeProvider timeProvider, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Apply pending migrations automatically on startup
        await _context.Database.MigrateAsync(cancellationToken);

        if (await _context.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already seeded — skipping.");
            return;
        }

        _logger.LogInformation("Seeding database with initial data...");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var dummyUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "seed@borro.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("seed-password123"),
            FirstName = "Borro",
            LastName = "Seed",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var car = new Item
        {
            Id = Guid.NewGuid(),
            Title = "Toyota Camry 2022",
            DailyPrice = 75.00m,
            Category = "Vehicle",
            Attributes = new ItemAttributes
            {
                Values = new Dictionary<string, object>
                {
                    { "Mileage", 15000 },
                    { "Transmission", "Automatic" }
                }
            },
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var camera = new Item
        {
            Id = Guid.NewGuid(),
            Title = "Sony Alpha A7 IV",
            DailyPrice = 50.00m,
            Category = "Electronics",
            Attributes = new ItemAttributes
            {
                Values = new Dictionary<string, object>
                {
                    { "Megapixels", 24 },
                    { "Brand", "Sony" }
                }
            },
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Users.Add(dummyUser);
        _context.Items.AddRange(car, camera);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded: 1 User and 2 Items successfully.");
    }
}
