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

        var seedUserId = dummyUser.Id;

        // Placeholder image sourced from Picsum Photos (public domain)
        const string placeholderImage = "https://picsum.photos/seed/borro/800/600";

        var items = new List<Item>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Toyota Camry 2022",
                Description = "Well-maintained sedan, perfect for weekend road trips. Non-smoker vehicle.",
                DailyPrice = 75.00m,
                Category = "Vehicle",
                LenderId = seedUserId,
                InstantBookEnabled = true,
                DeliveryAvailable = false,
                ImageUrls = [$"{placeholderImage}?vehicle1"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Mileage", 15000 },
                        { "Transmission", "Automatic" },
                        { "FuelType", "Petrol" },
                        { "Seats", 5 }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Ford Mustang GT 2021",
                Description = "Iconic muscle car for a thrilling driving experience. Available on weekends.",
                DailyPrice = 120.00m,
                Category = "Vehicle",
                LenderId = seedUserId,
                InstantBookEnabled = false,
                DeliveryAvailable = false,
                ImageUrls = [$"{placeholderImage}?vehicle2"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Mileage", 8500 },
                        { "Transmission", "Manual" },
                        { "FuelType", "Petrol" },
                        { "Seats", 4 }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Sony Alpha A7 IV",
                Description = "Full-frame mirrorless camera with 33MP sensor. Includes 28-70mm kit lens.",
                DailyPrice = 50.00m,
                Category = "Electronics",
                LenderId = seedUserId,
                InstantBookEnabled = true,
                DeliveryAvailable = true,
                ImageUrls = [$"{placeholderImage}?electronics1"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Megapixels", 33 },
                        { "Brand", "Sony" },
                        { "IncludesLens", true }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "DJI Mavic 3 Pro Drone",
                Description = "Professional-grade drone with Hasselblad camera. Includes extra batteries and carry case.",
                DailyPrice = 80.00m,
                Category = "Electronics",
                LenderId = seedUserId,
                InstantBookEnabled = false,
                DeliveryAvailable = true,
                ImageUrls = [$"{placeholderImage}?electronics2"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "MaxFlightTime", "46 min" },
                        { "Brand", "DJI" },
                        { "CameraResolution", "5.1K" }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Beachfront Studio Apartment",
                Description = "Cozy studio with ocean views, fully furnished. 5 min walk to the beach.",
                DailyPrice = 150.00m,
                Category = "RealEstate",
                LenderId = seedUserId,
                InstantBookEnabled = true,
                DeliveryAvailable = false,
                ImageUrls = [$"{placeholderImage}?realestate1"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Bedrooms", 0 },
                        { "Bathrooms", 1 },
                        { "MaxGuests", 2 },
                        { "HasWifi", true }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Downtown 2-Bedroom Apartment",
                Description = "Modern apartment in the city centre. Close to restaurants, bars and public transport.",
                DailyPrice = 220.00m,
                Category = "RealEstate",
                LenderId = seedUserId,
                InstantBookEnabled = false,
                DeliveryAvailable = false,
                ImageUrls = [$"{placeholderImage}?realestate2"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Bedrooms", 2 },
                        { "Bathrooms", 1 },
                        { "MaxGuests", 4 },
                        { "HasWifi", true }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Kayak — Sea Touring Double",
                Description = "Two-person sea kayak with paddles, life vests and dry bags included.",
                DailyPrice = 35.00m,
                Category = "Sports",
                LenderId = seedUserId,
                InstantBookEnabled = true,
                DeliveryAvailable = false,
                ImageUrls = [$"{placeholderImage}?sports1"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Capacity", "2 persons" },
                        { "Length", "5.2m" },
                        { "IncludesPaddles", true }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Mountain Bike — Trek Marlin 7",
                Description = "Hardtail MTB in excellent condition. Suitable for trails up to blue difficulty.",
                DailyPrice = 28.00m,
                Category = "Sports",
                LenderId = seedUserId,
                InstantBookEnabled = true,
                DeliveryAvailable = false,
                ImageUrls = [$"{placeholderImage}?sports2"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "FrameSize", "M" },
                        { "WheelSize", "29 inch" },
                        { "Gears", 21 }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "DeWalt Power Tool Set (20-piece)",
                Description = "Professional 20-piece power tool set. Drill, circular saw, jigsaw, and more.",
                DailyPrice = 45.00m,
                Category = "Tools",
                LenderId = seedUserId,
                InstantBookEnabled = false,
                DeliveryAvailable = true,
                ImageUrls = [$"{placeholderImage}?tools1"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Pieces", 20 },
                        { "Brand", "DeWalt" },
                        { "Voltage", "20V" }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Pressure Washer — Kärcher K5",
                Description = "High-pressure cleaner ideal for driveways, patios and vehicles.",
                DailyPrice = 30.00m,
                Category = "Tools",
                LenderId = seedUserId,
                InstantBookEnabled = true,
                DeliveryAvailable = true,
                ImageUrls = [$"{placeholderImage}?tools2"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Brand", "Kärcher" },
                        { "MaxPressureBar", 145 },
                        { "HoseLengthMeters", 8 }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "4-Person Camping Tent",
                Description = "Lightweight waterproof tent. Sets up in under 10 minutes. Includes pegs and guy ropes.",
                DailyPrice = 20.00m,
                Category = "Outdoor",
                LenderId = seedUserId,
                InstantBookEnabled = true,
                DeliveryAvailable = true,
                ImageUrls = [$"{placeholderImage}?outdoor1"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Capacity", 4 },
                        { "Season", "3-season" },
                        { "WeightKg", 2.8 }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Portable PA System — Bose S1 Pro",
                Description = "Battery-powered PA system with built-in mixer. Great for outdoor events and busking.",
                DailyPrice = 60.00m,
                Category = "Audio",
                LenderId = seedUserId,
                InstantBookEnabled = false,
                DeliveryAvailable = true,
                ImageUrls = [$"{placeholderImage}?audio1"],
                Attributes = new ItemAttributes
                {
                    Values = new Dictionary<string, object>
                    {
                        { "Brand", "Bose" },
                        { "Watts", 11 },
                        { "BatteryLife", "11 hours" },
                        { "Channels", 3 }
                    }
                },
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        _context.Users.Add(dummyUser);
        _context.Items.AddRange(items);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded: 1 User and {ItemCount} Items successfully.", items.Count);
    }
}
