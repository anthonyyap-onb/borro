using Borro.Domain.Entities;
using Borro.Domain.Enums;
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
        // Apply pending migrations automatically on startup.
        await _context.Database.MigrateAsync(cancellationToken);

        if (await _context.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already seeded — skipping.");
            return;
        }

        _logger.LogInformation("Seeding database with initial data...");

        var seedUserId = Guid.NewGuid();

        var dummyUser = new User
        {
            Id = seedUserId,
            Email = "seed@borro.dev",
            FirstName = "Borro",
            LastName = "Seed",
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        var car = Item.Create(
            ownerId: seedUserId,
            title: "Toyota Camry 2022",
            description: "Well-maintained sedan, great for road trips.",
            dailyPrice: 75.00m,
            location: "Sydney, NSW",
            category: Category.Vehicle);
        car.Attributes.Mileage = 15000;
        car.Attributes.Transmission = "Automatic";

        var camera = Item.Create(
            ownerId: seedUserId,
            title: "Sony Alpha A7 IV",
            description: "Full-frame mirrorless camera with 33MP sensor.",
            dailyPrice: 50.00m,
            location: "Melbourne, VIC",
            category: Category.Electronics);
        camera.Attributes.Megapixels = 33;
        camera.Attributes.Brand = "Sony";
        camera.Attributes.Condition = "Excellent";

        _context.Users.Add(dummyUser);
        _context.Items.AddRange(car, camera);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded: 1 User and 2 Items successfully.");
    }
}
