# Phase 2: Dynamic Listing Engine & Discovery — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow authenticated users to create item listings with dynamic category-specific attributes, upload photos to MinIO, and search/filter available items.

**Architecture:** Clean Architecture (Domain → Application → Infrastructure → API). Commands/queries via MediatR. MinIO image storage via an `IStorageService` abstraction. Item attributes stored as PostgreSQL JSONB via EF Core `OwnsOne().ToJson()` (already wired). Search filters against standard columns; JSONB attribute filters are deferred to Phase 3+.

**Tech Stack:** .NET 9, MediatR 12.5, EF Core 9 + Npgsql, AWSSDK.S3 (pointed at MinIO), xUnit + Moq, React 18 + Vite + TypeScript + Tailwind

---

## Phase 1 Status (Already Done — Do NOT re-implement)

- `User` entity, JWT auth (register / login / Google), `AuthContext`, `LoginPage`, `RegisterPage`, `HomePage`
- `Item` entity **stub** (Id, Title, DailyPrice, Category, Attributes, timestamps) — will be **extended** in Task 2
- `BorroDbContext` with User + Item (partial Item config) — will be **extended** in Task 6
- Docker compose with postgres + minio

---

## File Map

**Create:**
| File | Responsibility |
|------|----------------|
| `backend/Borro.Tests/Borro.Tests.csproj` | xUnit test project |
| `backend/Borro.Tests/Items/CreateItemCommandHandlerTests.cs` | Handler unit tests |
| `backend/Borro.Tests/Items/SearchItemsQueryHandlerTests.cs` | Query unit tests |
| `backend/Borro.Domain/Enums/HandoverOption.cs` | Enum for pickup/delivery types |
| `backend/Borro.Domain/Entities/Wishlist.cs` | Wishlist join entity |
| `backend/Borro.Application/Common/Interfaces/IStorageService.cs` | File upload abstraction |
| `backend/Borro.Application/Items/DTOs/ItemDto.cs` | Read-side DTO |
| `backend/Borro.Application/Items/Commands/CreateItem/CreateItemCommand.cs` | Create listing command |
| `backend/Borro.Application/Items/Commands/CreateItem/CreateItemCommandHandler.cs` | Handler |
| `backend/Borro.Application/Items/Commands/UploadItemImage/UploadItemImageCommand.cs` | Upload image command |
| `backend/Borro.Application/Items/Commands/UploadItemImage/UploadItemImageCommandHandler.cs` | Handler |
| `backend/Borro.Application/Items/Queries/GetItem/GetItemQuery.cs` | Get single item |
| `backend/Borro.Application/Items/Queries/GetItem/GetItemQueryHandler.cs` | Handler |
| `backend/Borro.Application/Items/Queries/SearchItems/SearchItemsQuery.cs` | Filtered item list |
| `backend/Borro.Application/Items/Queries/SearchItems/SearchItemsQueryHandler.cs` | Handler |
| `backend/Borro.Infrastructure/Services/MinioStorageService.cs` | S3/MinIO upload implementation |
| `backend/Borro.Api/Endpoints/ItemEndpoints.cs` | Minimal API route group |
| `frontend/src/features/items/itemApi.ts` | Axios wrappers for item endpoints |
| `frontend/src/features/items/CreateListingPage.tsx` | Dynamic listing form |
| `frontend/src/features/items/SearchPage.tsx` | Search + filter grid |
| `frontend/src/features/items/ItemDetailPage.tsx` | Single-item detail view |

**Modify:**
| File | Change |
|------|--------|
| `backend/Borro.Domain/Entities/Item.cs` | Add OwnerId, Description, Location, InstantBookEnabled, HandoverOptions, ImageUrls, Owner nav |
| `backend/Borro.Application/Common/Interfaces/IApplicationDbContext.cs` | Add `DbSet<Wishlist> Wishlists` |
| `backend/Borro.Infrastructure/Persistence/BorroDbContext.cs` | Complete Item config + Wishlist config |
| `backend/Borro.Infrastructure/DependencyInjection.cs` | Register `IStorageService` |
| `backend/Borro.Infrastructure/Borro.Infrastructure.csproj` | Add `AWSSDK.S3` |
| `backend/Borro.Api/Borro.Api.csproj` | Add `Microsoft.AspNetCore.SignalR` (prep for Phase 3) |
| `backend/Borro.Api/appsettings.json` | Add `MinIO` config section |
| `backend/Borro.Api/Program.cs` | `app.MapItemEndpoints()` |
| `frontend/src/App.tsx` | Add `/items/new`, `/search`, `/items/:id` routes |

---

## Task 1: Test Project Setup

**Files:**
- Create: `backend/Borro.Tests/Borro.Tests.csproj`

- [ ] **Step 1: Create the test project file**

```xml
<!-- backend/Borro.Tests/Borro.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Borro.Application\Borro.Application.csproj" />
    <ProjectReference Include="..\Borro.Infrastructure\Borro.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add test project to solution**

```bash
cd backend
dotnet sln add Borro.Tests/Borro.Tests.csproj
```

Expected: `Project 'Borro.Tests\Borro.Tests.csproj' added to the solution.`

- [ ] **Step 3: Verify test project builds**

```bash
cd backend
dotnet build Borro.Tests/Borro.Tests.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/Borro.Tests/ backend/Borro.sln
git commit -m "test: add Borro.Tests xUnit project"
```

---

## Task 2: Complete Item Entity & Supporting Domain Types

**Files:**
- Modify: `backend/Borro.Domain/Entities/Item.cs`
- Create: `backend/Borro.Domain/Enums/HandoverOption.cs`
- Create: `backend/Borro.Domain/Entities/Wishlist.cs`

- [ ] **Step 1: Create HandoverOption enum**

```csharp
// backend/Borro.Domain/Enums/HandoverOption.cs
namespace Borro.Domain.Enums;

public enum HandoverOption
{
    OwnerDelivers,
    RenterPicksUp,
    ThirdPartyDropOff
}
```

- [ ] **Step 2: Replace Item.cs with the completed entity**

```csharp
// backend/Borro.Domain/Entities/Item.cs
using Borro.Domain.Enums;

namespace Borro.Domain.Entities;

public class ItemAttributes
{
    public Dictionary<string, object> Values { get; set; } = new();
}

public class Item
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    /// <summary>Dynamic category-specific attributes stored as JSONB via EF Core .ToJson().</summary>
    public ItemAttributes Attributes { get; set; } = new();

    public bool InstantBookEnabled { get; set; }

    /// <summary>Stored as comma-delimited text, e.g. "OwnerDelivers,RenterPicksUp".</summary>
    public string HandoverOptionsRaw { get; set; } = string.Empty;

    /// <summary>Not mapped — computed from HandoverOptionsRaw.</summary>
    public List<HandoverOption> HandoverOptions
    {
        get => string.IsNullOrEmpty(HandoverOptionsRaw)
            ? new List<HandoverOption>()
            : HandoverOptionsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(Enum.Parse<HandoverOption>)
                .ToList();
        set => HandoverOptionsRaw = string.Join(',', value.Select(h => h.ToString()));
    }

    public List<string> ImageUrls { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
```

- [ ] **Step 3: Create Wishlist entity**

```csharp
// backend/Borro.Domain/Entities/Wishlist.cs
namespace Borro.Domain.Entities;

public class Wishlist
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
```

- [ ] **Step 4: Build Domain to verify no compile errors**

```bash
cd backend
dotnet build Borro.Domain/Borro.Domain.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Domain/
git commit -m "feat: complete Item entity with all fields, add HandoverOption enum and Wishlist entity"
```

---

## Task 3: Extend Application Interfaces & DTOs

**Files:**
- Modify: `backend/Borro.Application/Common/Interfaces/IApplicationDbContext.cs`
- Create: `backend/Borro.Application/Common/Interfaces/IStorageService.cs`
- Create: `backend/Borro.Application/Items/DTOs/ItemDto.cs`

- [ ] **Step 1: Extend IApplicationDbContext to include Wishlists**

```csharp
// backend/Borro.Application/Common/Interfaces/IApplicationDbContext.cs
using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Item> Items { get; }
    DbSet<Wishlist> Wishlists { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Create IStorageService**

```csharp
// backend/Borro.Application/Common/Interfaces/IStorageService.cs
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
```

- [ ] **Step 3: Create ItemDto**

```csharp
// backend/Borro.Application/Items/DTOs/ItemDto.cs
namespace Borro.Application.Items.DTOs;

public record ItemDto(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    string Title,
    string Description,
    decimal DailyPrice,
    string Location,
    string Category,
    Dictionary<string, object> Attributes,
    bool InstantBookEnabled,
    List<string> HandoverOptions,
    List<string> ImageUrls,
    DateTime CreatedAtUtc
);
```

- [ ] **Step 4: Build Application to verify**

```bash
cd backend
dotnet build Borro.Application/Borro.Application.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Application/
git commit -m "feat: extend IApplicationDbContext with Wishlists, add IStorageService and ItemDto"
```

---

## Task 4: CreateItem Command + Tests

**Files:**
- Create: `backend/Borro.Application/Items/Commands/CreateItem/CreateItemCommand.cs`
- Create: `backend/Borro.Application/Items/Commands/CreateItem/CreateItemCommandHandler.cs`
- Create: `backend/Borro.Tests/Items/CreateItemCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing test first**

```csharp
// backend/Borro.Tests/Items/CreateItemCommandHandlerTests.cs
using Borro.Application.Items.Commands.CreateItem;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Borro.Tests.Items;

public class CreateItemCommandHandlerTests
{
    private static BorroDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BorroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BorroDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesItemAndReturnsDto()
    {
        await using var ctx = CreateContext();
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@test.com",
            FirstName = "Alice",
            LastName = "Smith",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        ctx.Users.Add(owner);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateItemCommandHandler(ctx);
        var cmd = new CreateItemCommand(
            OwnerId: owner.Id,
            Title: "DeWalt Drill",
            Description: "Heavy-duty cordless drill",
            DailyPrice: 15m,
            Location: "Portland, OR",
            Category: "Tools",
            Attributes: new Dictionary<string, object> { ["Voltage"] = "20V" },
            InstantBookEnabled: true,
            HandoverOptions: new List<string> { "RenterPicksUp" }
        );

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.Equal("DeWalt Drill", result.Title);
        Assert.Equal(15m, result.DailyPrice);
        Assert.Equal(owner.Id, result.OwnerId);
        Assert.True(result.InstantBookEnabled);
        Assert.Single(ctx.Items);
    }

    [Fact]
    public async Task Handle_UnknownOwner_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var handler = new CreateItemCommandHandler(ctx);
        var cmd = new CreateItemCommand(
            OwnerId: Guid.NewGuid(),
            Title: "Camera",
            Description: "DSLR",
            DailyPrice: 50m,
            Location: "NYC",
            Category: "Electronics",
            Attributes: new Dictionary<string, object>(),
            InstantBookEnabled: false,
            HandoverOptions: new List<string>()
        );

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test to confirm it fails (handler doesn't exist yet)**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "CreateItemCommandHandlerTests" -v minimal
```

Expected: Build error — `CreateItemCommand` not found.

- [ ] **Step 3: Create the command record**

```csharp
// backend/Borro.Application/Items/Commands/CreateItem/CreateItemCommand.cs
using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Commands.CreateItem;

public record CreateItemCommand(
    Guid OwnerId,
    string Title,
    string Description,
    decimal DailyPrice,
    string Location,
    string Category,
    Dictionary<string, object> Attributes,
    bool InstantBookEnabled,
    List<string> HandoverOptions
) : IRequest<ItemDto>;
```

- [ ] **Step 4: Create the handler**

```csharp
// backend/Borro.Application/Items/Commands/CreateItem/CreateItemCommandHandler.cs
using Borro.Application.Common.Interfaces;
using Borro.Application.Items.DTOs;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands.CreateItem;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, ItemDto>
{
    private readonly IApplicationDbContext _db;

    public CreateItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ItemDto> Handle(CreateItemCommand cmd, CancellationToken ct)
    {
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == cmd.OwnerId, ct)
            ?? throw new InvalidOperationException($"User {cmd.OwnerId} not found.");

        var item = new Item
        {
            Id = Guid.NewGuid(),
            OwnerId = cmd.OwnerId,
            Title = cmd.Title,
            Description = cmd.Description,
            DailyPrice = cmd.DailyPrice,
            Location = cmd.Location,
            Category = cmd.Category,
            Attributes = new ItemAttributes { Values = cmd.Attributes },
            InstantBookEnabled = cmd.InstantBookEnabled,
            HandoverOptions = cmd.HandoverOptions
                .Select(Enum.Parse<HandoverOption>)
                .ToList(),
            ImageUrls = new List<string>(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync(ct);

        return ToDto(item, owner);
    }

    internal static ItemDto ToDto(Item item, User owner) => new(
        Id: item.Id,
        OwnerId: item.OwnerId,
        OwnerName: $"{owner.FirstName} {owner.LastName}",
        Title: item.Title,
        Description: item.Description,
        DailyPrice: item.DailyPrice,
        Location: item.Location,
        Category: item.Category,
        Attributes: item.Attributes.Values,
        InstantBookEnabled: item.InstantBookEnabled,
        HandoverOptions: item.HandoverOptions.Select(h => h.ToString()).ToList(),
        ImageUrls: item.ImageUrls,
        CreatedAtUtc: item.CreatedAtUtc
    );
}
```

- [ ] **Step 5: Run tests — both should pass**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "CreateItemCommandHandlerTests" -v minimal
```

Expected: `Passed: 2`

- [ ] **Step 6: Commit**

```bash
git add backend/Borro.Application/Items/Commands/CreateItem/ backend/Borro.Tests/Items/CreateItemCommandHandlerTests.cs
git commit -m "feat: add CreateItemCommand with handler and tests"
```

---

## Task 5: UploadItemImage Command

**Files:**
- Create: `backend/Borro.Application/Items/Commands/UploadItemImage/UploadItemImageCommand.cs`
- Create: `backend/Borro.Application/Items/Commands/UploadItemImage/UploadItemImageCommandHandler.cs`

- [ ] **Step 1: Create command**

```csharp
// backend/Borro.Application/Items/Commands/UploadItemImage/UploadItemImageCommand.cs
using MediatR;

namespace Borro.Application.Items.Commands.UploadItemImage;

public record UploadItemImageCommand(
    Guid ItemId,
    Guid RequestingUserId,
    Stream FileStream,
    string FileName,
    string ContentType
) : IRequest<string>;
```

- [ ] **Step 2: Create handler**

```csharp
// backend/Borro.Application/Items/Commands/UploadItemImage/UploadItemImageCommandHandler.cs
using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands.UploadItemImage;

public class UploadItemImageCommandHandler : IRequestHandler<UploadItemImageCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IStorageService _storage;

    public UploadItemImageCommandHandler(IApplicationDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<string> Handle(UploadItemImageCommand cmd, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == cmd.ItemId, ct)
            ?? throw new InvalidOperationException($"Item {cmd.ItemId} not found.");

        if (item.OwnerId != cmd.RequestingUserId)
            throw new UnauthorizedAccessException("Only the item owner can upload images.");

        var uniqueFileName = $"items/{cmd.ItemId}/{Guid.NewGuid()}_{cmd.FileName}";
        var url = await _storage.UploadFileAsync(cmd.FileStream, uniqueFileName, cmd.ContentType, ct);

        item.ImageUrls.Add(url);
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return url;
    }
}
```

- [ ] **Step 3: Build Application**

```bash
cd backend
dotnet build Borro.Application/Borro.Application.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/Borro.Application/Items/Commands/UploadItemImage/
git commit -m "feat: add UploadItemImageCommand with handler"
```

---

## Task 6: GetItem & SearchItems Queries + Tests

**Files:**
- Create: `backend/Borro.Application/Items/Queries/GetItem/GetItemQuery.cs`
- Create: `backend/Borro.Application/Items/Queries/GetItem/GetItemQueryHandler.cs`
- Create: `backend/Borro.Application/Items/Queries/SearchItems/SearchItemsQuery.cs`
- Create: `backend/Borro.Application/Items/Queries/SearchItems/SearchItemsQueryHandler.cs`
- Create: `backend/Borro.Tests/Items/SearchItemsQueryHandlerTests.cs`

- [ ] **Step 1: Write failing SearchItems test**

```csharp
// backend/Borro.Tests/Items/SearchItemsQueryHandlerTests.cs
using Borro.Application.Items.Queries.SearchItems;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Borro.Tests.Items;

public class SearchItemsQueryHandlerTests
{
    private static BorroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<BorroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (User owner, Item item) Seed(BorroDbContext ctx, string category, decimal price, string location)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(), Email = $"{Guid.NewGuid()}@t.com",
            FirstName = "A", LastName = "B",
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        var item = new Item
        {
            Id = Guid.NewGuid(), OwnerId = owner.Id, Owner = owner,
            Title = "Test Item", Description = "desc", DailyPrice = price,
            Location = location, Category = category,
            Attributes = new ItemAttributes { Values = new() },
            InstantBookEnabled = false, ImageUrls = new(),
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        ctx.Users.Add(owner);
        ctx.Items.Add(item);
        ctx.SaveChanges();
        return (owner, item);
    }

    [Fact]
    public async Task Handle_NullFilters_ReturnsAllItems()
    {
        await using var ctx = CreateContext();
        Seed(ctx, "Tools", 20m, "Portland");
        Seed(ctx, "Vehicles", 80m, "Seattle");

        var handler = new SearchItemsQueryHandler(ctx);
        var results = await handler.Handle(new SearchItemsQuery(null, null, null), CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Handle_CategoryFilter_ReturnsOnlyMatchingItems()
    {
        await using var ctx = CreateContext();
        Seed(ctx, "Tools", 20m, "Portland");
        Seed(ctx, "Vehicles", 80m, "Seattle");

        var handler = new SearchItemsQueryHandler(ctx);
        var results = await handler.Handle(new SearchItemsQuery("Tools", null, null), CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("Tools", results[0].Category);
    }

    [Fact]
    public async Task Handle_MaxPriceFilter_ExcludesExpensiveItems()
    {
        await using var ctx = CreateContext();
        Seed(ctx, "Tools", 20m, "Portland");
        Seed(ctx, "Vehicles", 80m, "Seattle");

        var handler = new SearchItemsQueryHandler(ctx);
        var results = await handler.Handle(new SearchItemsQuery(null, null, 50m), CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(20m, results[0].DailyPrice);
    }
}
```

- [ ] **Step 2: Run test — confirm build fails (handlers not yet created)**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "SearchItemsQueryHandlerTests" -v minimal 2>&1 | head -20
```

Expected: Build error — `SearchItemsQuery` not found.

- [ ] **Step 3: Create GetItemQuery**

```csharp
// backend/Borro.Application/Items/Queries/GetItem/GetItemQuery.cs
using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Queries.GetItem;

public record GetItemQuery(Guid ItemId) : IRequest<ItemDto>;
```

- [ ] **Step 4: Create GetItemQueryHandler**

```csharp
// backend/Borro.Application/Items/Queries/GetItem/GetItemQueryHandler.cs
using Borro.Application.Common.Interfaces;
using Borro.Application.Items.Commands.CreateItem;
using Borro.Application.Items.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries.GetItem;

public class GetItemQueryHandler : IRequestHandler<GetItemQuery, ItemDto>
{
    private readonly IApplicationDbContext _db;

    public GetItemQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ItemDto> Handle(GetItemQuery request, CancellationToken ct)
    {
        var item = await _db.Items
            .Include(i => i.Owner)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, ct)
            ?? throw new InvalidOperationException($"Item {request.ItemId} not found.");

        return CreateItemCommandHandler.ToDto(item, item.Owner);
    }
}
```

- [ ] **Step 5: Create SearchItemsQuery**

```csharp
// backend/Borro.Application/Items/Queries/SearchItems/SearchItemsQuery.cs
using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Queries.SearchItems;

public record SearchItemsQuery(
    string? Category,
    string? Location,
    decimal? MaxDailyPrice
) : IRequest<List<ItemDto>>;
```

- [ ] **Step 6: Create SearchItemsQueryHandler**

```csharp
// backend/Borro.Application/Items/Queries/SearchItems/SearchItemsQueryHandler.cs
using Borro.Application.Common.Interfaces;
using Borro.Application.Items.Commands.CreateItem;
using Borro.Application.Items.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries.SearchItems;

public class SearchItemsQueryHandler : IRequestHandler<SearchItemsQuery, List<ItemDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchItemsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ItemDto>> Handle(SearchItemsQuery q, CancellationToken ct)
    {
        var query = _db.Items.Include(i => i.Owner).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Category))
            query = query.Where(i => i.Category == q.Category);

        if (!string.IsNullOrWhiteSpace(q.Location))
            query = query.Where(i => EF.Functions.ILike(i.Location, $"%{q.Location}%"));

        if (q.MaxDailyPrice.HasValue)
            query = query.Where(i => i.DailyPrice <= q.MaxDailyPrice.Value);

        var items = await query.OrderByDescending(i => i.CreatedAtUtc).ToListAsync(ct);
        return items.Select(i => CreateItemCommandHandler.ToDto(i, i.Owner)).ToList();
    }
}
```

- [ ] **Step 7: Run all item tests**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj -v minimal
```

Expected: `Passed: 5` (2 from CreateItemCommandHandlerTests + 3 from SearchItemsQueryHandlerTests)

- [ ] **Step 8: Commit**

```bash
git add backend/Borro.Application/Items/Queries/ backend/Borro.Tests/Items/SearchItemsQueryHandlerTests.cs
git commit -m "feat: add GetItem and SearchItems queries with tests"
```

---

## Task 7: MinIO Storage Service

**Files:**
- Modify: `backend/Borro.Infrastructure/Borro.Infrastructure.csproj`
- Create: `backend/Borro.Infrastructure/Services/MinioStorageService.cs`
- Modify: `backend/Borro.Infrastructure/DependencyInjection.cs`
- Modify: `backend/Borro.Api/appsettings.json`

- [ ] **Step 1: Add AWSSDK.S3 to Infrastructure**

Edit `backend/Borro.Infrastructure/Borro.Infrastructure.csproj` — add inside the existing `<ItemGroup>` with PackageReferences:

```xml
<PackageReference Include="AWSSDK.S3" Version="3.7.416.3" />
```

- [ ] **Step 2: Run dotnet restore**

```bash
cd backend
dotnet restore
```

Expected: `Restore succeeded.`

- [ ] **Step 3: Create MinioStorageService**

```csharp
// backend/Borro.Infrastructure/Services/MinioStorageService.cs
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
```

- [ ] **Step 4: Register MinioStorageService in DependencyInjection.cs**

Open `backend/Borro.Infrastructure/DependencyInjection.cs` and add after the existing service registrations:

```csharp
// Add inside AddInfrastructure, after existing registrations:
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
```

Also add the required using at the top of the file:
```csharp
using Amazon.S3;
```

- [ ] **Step 5: Add MinIO config section to appsettings.json**

Edit `backend/Borro.Api/appsettings.json` — add at the end (before closing `}`):

```json
"MinIO": {
  "ServiceUrl": "http://localhost:9000",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "Bucket": "borro-assets",
  "PublicBaseUrl": "http://localhost:9000/borro-assets"
}
```

- [ ] **Step 6: Build Infrastructure**

```bash
cd backend
dotnet build Borro.Infrastructure/Borro.Infrastructure.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add backend/Borro.Infrastructure/ backend/Borro.Api/appsettings.json
git commit -m "feat: add MinIO storage service via AWSSDK.S3"
```

---

## Task 8: Update BorroDbContext + EF Migration

**Files:**
- Modify: `backend/Borro.Infrastructure/Persistence/BorroDbContext.cs`

- [ ] **Step 1: Extend BorroDbContext with full Item config and Wishlist**

Replace the full file:

```csharp
// backend/Borro.Infrastructure/Persistence/BorroDbContext.cs
using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Borro.Infrastructure.Persistence;

public class BorroDbContext : DbContext, IApplicationDbContext
{
    public BorroDbContext(DbContextOptions<BorroDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Description).IsRequired().HasMaxLength(2000);
            entity.Property(i => i.DailyPrice).HasColumnType("numeric(18,2)");
            entity.Property(i => i.Category).IsRequired().HasMaxLength(100);
            entity.Property(i => i.Location).IsRequired().HasMaxLength(200);
            entity.Property(i => i.HandoverOptionsRaw).HasColumnName("handover_options").HasMaxLength(500);

            entity.HasOne(i => i.Owner)
                .WithMany()
                .HasForeignKey(i => i.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // ImageUrls stored as PostgreSQL text[]
            entity.Property(i => i.ImageUrls)
                .HasColumnType("text[]");

            // Ignore the computed HandoverOptions property — only persist HandoverOptionsRaw
            entity.Ignore(i => i.HandoverOptions);

            // Map ItemAttributes as JSONB via EF Core 8+ owned entity JSON mapping
            entity.OwnsOne(i => i.Attributes, builder =>
            {
                builder.ToJson();

                var jsonOptions = (System.Text.Json.JsonSerializerOptions?)null;
                var comparer = new ValueComparer<Dictionary<string, object>>(
                    (c1, c2) => System.Text.Json.JsonSerializer.Serialize(c1, jsonOptions)
                                == System.Text.Json.JsonSerializer.Serialize(c2, jsonOptions),
                    c => System.Text.Json.JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
                    c => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                             System.Text.Json.JsonSerializer.Serialize(c, jsonOptions), jsonOptions)
                         ?? new Dictionary<string, object>()
                );

                builder.Property(a => a.Values)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, jsonOptions),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions)
                             ?? new Dictionary<string, object>()
                    )
                    .Metadata.SetValueComparer(comparer);
            });
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(w => new { w.UserId, w.ItemId });
            entity.HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(w => w.Item).WithMany().HasForeignKey(w => w.ItemId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

- [ ] **Step 2: Build the full solution to verify no errors**

```bash
cd backend
dotnet build Borro.sln
```

Expected: `Build succeeded.`

- [ ] **Step 3: Add EF migration**

```bash
cd backend
dotnet ef migrations add Phase2_ItemsAndWishlist \
  --project Borro.Infrastructure \
  --startup-project Borro.Api
```

Expected: Migration files created in `Borro.Infrastructure/Persistence/Migrations/`.

- [ ] **Step 4: Apply migration (requires running postgres)**

```bash
cd backend
dotnet ef database update \
  --project Borro.Infrastructure \
  --startup-project Borro.Api
```

Expected: `Done.`

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Infrastructure/Persistence/
git commit -m "feat: extend BorroDbContext with full Item config, Wishlist, and Phase2 migration"
```

---

## Task 9: ItemEndpoints + Wire in Program.cs

**Files:**
- Create: `backend/Borro.Api/Endpoints/ItemEndpoints.cs`
- Modify: `backend/Borro.Api/Program.cs`

- [ ] **Step 1: Create ItemEndpoints**

```csharp
// backend/Borro.Api/Endpoints/ItemEndpoints.cs
using Borro.Application.Items.Commands.CreateItem;
using Borro.Application.Items.Commands.UploadItemImage;
using Borro.Application.Items.Queries.GetItem;
using Borro.Application.Items.Queries.SearchItems;
using MediatR;
using System.Security.Claims;

namespace Borro.Api.Endpoints;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/items").WithTags("Items");

        // GET /api/items/search?category=Tools&location=Portland&maxPrice=50
        group.MapGet("/search", async (
            string? category, string? location, decimal? maxPrice,
            IMediator mediator, CancellationToken ct) =>
        {
            var results = await mediator.Send(new SearchItemsQuery(category, location, maxPrice), ct);
            return Results.Ok(results);
        });

        // GET /api/items/{id}
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var item = await mediator.Send(new GetItemQuery(id), ct);
                return Results.Ok(item);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        });

        // GET /api/items/{id}/availability  — returns blocked dates (Phase 3 will populate this)
        group.MapGet("/{id:guid}/availability", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            // Stub: Phase 3 replaces this with real booked-date logic
            await Task.CompletedTask;
            return Results.Ok(Array.Empty<object>());
        });

        // POST /api/items  (requires auth)
        group.MapPost("/", async (CreateItemRequest req, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var ownerIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");
            if (ownerIdClaim is null || !Guid.TryParse(ownerIdClaim, out var ownerId))
                return Results.Unauthorized();

            try
            {
                var result = await mediator.Send(
                    new CreateItemCommand(
                        ownerId, req.Title, req.Description, req.DailyPrice,
                        req.Location, req.Category, req.Attributes,
                        req.InstantBookEnabled, req.HandoverOptions), ct);
                return Results.Created($"/api/items/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // POST /api/items/images  (requires auth, multipart/form-data)
        group.MapPost("/images", async (
            HttpRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            if (!request.Form.TryGetValue("itemId", out var itemIdStr)
                || !Guid.TryParse(itemIdStr, out var itemId))
                return Results.BadRequest(new { error = "itemId is required." });

            var file = request.Form.Files.FirstOrDefault();
            if (file is null)
                return Results.BadRequest(new { error = "No file provided." });

            try
            {
                await using var stream = file.OpenReadStream();
                var url = await mediator.Send(
                    new UploadItemImageCommand(itemId, userId, stream, file.FileName, file.ContentType), ct);
                return Results.Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }).RequireAuthorization();

        return app;
    }

    private record CreateItemRequest(
        string Title,
        string Description,
        decimal DailyPrice,
        string Location,
        string Category,
        Dictionary<string, object> Attributes,
        bool InstantBookEnabled,
        List<string> HandoverOptions
    );
}
```

- [ ] **Step 2: Wire up in Program.cs — add one line after `app.MapAuthEndpoints()`**

```csharp
app.MapItemEndpoints();
```

- [ ] **Step 3: Build and run API**

```bash
cd backend
dotnet build Borro.Api/Borro.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/Borro.Api/
git commit -m "feat: add ItemEndpoints (CRUD, search, image upload) and wire in Program.cs"
```

---

## Task 10: Frontend — itemApi.ts

**Files:**
- Create: `frontend/src/features/items/itemApi.ts`

- [ ] **Step 1: Create the item API client**

```typescript
// frontend/src/features/items/itemApi.ts
import apiClient from '../../lib/apiClient';

export interface ItemAttributes {
  [key: string]: string | number | boolean;
}

export interface ItemDto {
  id: string;
  ownerId: string;
  ownerName: string;
  title: string;
  description: string;
  dailyPrice: number;
  location: string;
  category: string;
  attributes: ItemAttributes;
  instantBookEnabled: boolean;
  handoverOptions: string[];
  imageUrls: string[];
  createdAtUtc: string;
}

export interface CreateItemPayload {
  title: string;
  description: string;
  dailyPrice: number;
  location: string;
  category: string;
  attributes: ItemAttributes;
  instantBookEnabled: boolean;
  handoverOptions: string[];
}

export interface SearchParams {
  category?: string;
  location?: string;
  maxPrice?: number;
}

export const itemApi = {
  search: (params: SearchParams) =>
    apiClient.get<ItemDto[]>('/api/items/search', { params }),

  getById: (id: string) =>
    apiClient.get<ItemDto>(`/api/items/${id}`),

  create: (payload: CreateItemPayload) =>
    apiClient.post<ItemDto>('/api/items', payload),

  uploadImage: (itemId: string, file: File) => {
    const form = new FormData();
    form.append('itemId', itemId);
    form.append('file', file);
    return apiClient.post<{ url: string }>('/api/items/images', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/features/items/itemApi.ts
git commit -m "feat: add itemApi Axios wrappers"
```

---

## Stitch UI Design Reference (project `4142831471135952271`)

> These designs are the source of truth for Tasks 11–13. Implement TSX to match them exactly.

| Task | Screen | Screen ID | Screenshot |
|------|--------|-----------|------------|
| Task 11 — CreateListingPage | Create / Edit Listing | `8d3ef420217047a491524278db414634` | ![Create/Edit Listing](https://lh3.googleusercontent.com/aida/ADBb0ui2-s4h8gUyRiXrlkXsBlX2Bj-4iXGtHqJWzEfPu2to-FnVWhEwzHhUsfpDAJAkCvG-Q-VebaRChIlFmE4qpZW-zYa1YIEEJJ-d8ohaL6BLvJtl6cjdBH5OlqrNSKn9ZuOb8999wMNUys7ayzRgWroZ6m_fgxflna01ya9VHUHiK9BMw0uHLHNUa2YFkm-QRFojTawAuYjJOTLZyWtS1LpZ3eqf8VfUssiwXzVkAmQYyNPupC6JDAErAoo) |
| Task 12 — SearchPage | Search Results | `c08956c0fcfc4c079a6b1973fa0edc6d` | ![Search Results](https://lh3.googleusercontent.com/aida/ADBb0uivke8mQrjD9xa26YT0J--9xxLvGjuFq8XgR2NdcqPk8K6gqoMGkw537ydUyEHKjtOkgKTfJ-BXiSIOKm3afRfENa3jJ7ifg6YCz92Z8ojfv-yPu41wROS1_gYexDMgbtg9gdt0vdTm2f16f-AMSljm1_iR3ERLdSvQwm-XgKhvsYnMR7R0P1pMOL6i41kZEhbY6uhtm0kUxsRfWqhCXKMSGlhewwa9JrtmlNxxNu0_9uXO72jKUqfPVg) |
| Task 13 — ItemDetailPage | Item Detail Page | `39ff8de157d34ebd86a9bd58f76eb986` | ![Item Detail](https://lh3.googleusercontent.com/aida/ADBb0ujIm4hC7K-o4c6DcEx9SmdLMU3B5HBDllB6CxCsb_Wzkhgvm5Nf_YQghxwmxDaz7tnm9y52u_ICIIMn32P8l0nzwOHf95IoIXCuEdFDQiUbIFBDHmnKBKlcUMSLOlOUo9bsj6T8pP2XSkq4Vryg7fF0W_dBmmoTh-K5RPpACSPRQqWXRBrHUYVsMaIgCxx5TGImafBl-SMIhcKNqddpVIV0KRh2VW_PwlViik96vkOK5E0qwpiKS64rmw) |

### Design System ("The Editorial Exchange")
- **Colors:** Primary `#005f6c`, Surface `#f9f9f9`, On-surface `#1a1c1c`, On-surface-variant `#3e494b`, Tertiary/action `#a91929`
- **Fonts:** Headlines — `Plus Jakarta Sans` (bold); Body/labels — `Manrope`
- **No 1px solid borders** — use `surface-container` background shifts (`bg-[#f3f3f3]`, `bg-[#eeeeee]`) or ghost borders at 15% opacity
- **Card shadows:** `shadow-[0_4px_24px_rgba(26,28,28,0.06)]`; nav shadow: `shadow-[0_2px_12px_rgba(26,28,28,0.06)]`
- **Primary CTA:** Pill shape, gradient `bg-gradient-to-r from-[#005f6c] to-[#007a8a]`
- **Glassmorphism** for floating elements: `bg-white/80 backdrop-blur-md`
- **Roundness:** `rounded-2xl` (24px) for cards/images; `rounded-xl` (12px) for inputs; `rounded-full` for buttons/chips

### CreateListingPage — Stitch Design Points
- **Two-column layout:** form fields (left, `flex-1`) + sticky photo upload panel (right, `w-[340px]`)
- Photo panel: `{imageFiles.length} of 8 uploaded` label; 3-column grid of numbered slots (`1–8`); "Add Photos" dashed-border upload trigger
- Fields organized in cards (`bg-white rounded-2xl p-6 shadow-...`): Item Details, Category Specs, Pricing, Handover & Options
- Spec fields rendered in a **2-column grid** per category
- Pricing: daily rate + minimum rental days (weekend rate is display-only in MVP)
- Handover options: checkboxes inside `bg-[#f3f3f3] rounded-xl px-4 py-2.5` pill labels
- Instant Book toggle has a subtitle: "Renters can book without waiting for your approval"

### SearchPage — Stitch Design Points
- **Left sidebar** (w-72, sticky): filters — Category select, Price Range slider (`$0–$450+`), Location input, Apply Filters pill button
- Main content: `"Items near {location}"` heading + result count subline
- Item cards: `bg-white rounded-2xl` with `shadow-[0_4px_24px_...]`; image overlay badges (Instant Book in `#a91929`, heart/favorite); title, location, `$/day`
- Floating **"Show Map"** dark glassmorphism pill fixed at bottom-center
- No top filter bar; no explicit 1px borders on cards

### ItemDetailPage — Stitch Design Points
- **Full-width photo gallery** at top: tall `aspect-[16/7]` main image + horizontal thumbnail strip + `"Show all photos"` ghost button (bottom-right of image)
- Category badge (`bg-[#daf8ff] text-[#005f6c]`) + star rating with review count (stub: 4.9 / 128)
- **Host card** (`bg-white rounded-2xl p-5 flex items-center gap-4`): avatar placeholder, owner name, "Identity Verified" badge, "Contact Host" secondary button
- Description → "Technical Specifications" grid (`grid-cols-2`) → Handover Options chips → Reviews stub
- **Sticky right booking sidebar** (`lg:sticky lg:top-24 self-start`):
  - `$X / day` in `font-black text-4xl`
  - Instant Book badge in `#a91929`
  - Pick-up / Return `<input type="date">` inside `bg-[#f3f3f3] rounded-xl p-4`
  - Cost breakdown: daily×days, insurance $15, service $12, total; separator line above total
  - Gradient "Book Now" / "Request to Book" CTA
  - `"You won't be charged yet"` footnote in `text-xs text-[#3e494b]`

---

## Task 11: Frontend — CreateListingPage

**Files:**
- Create: `frontend/src/features/items/CreateListingPage.tsx`

- [ ] **Step 1: Create the dynamic listing form** _(matches Stitch "Create / Edit Listing" screen)_

```tsx
// frontend/src/features/items/CreateListingPage.tsx
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { itemApi, type CreateItemPayload, type ItemAttributes } from './itemApi';

const CATEGORIES = ['Vehicles', 'Tools & Equipment', 'Electronics', 'Outdoors', 'Event Gear', 'Other'];

const CATEGORY_FIELDS: Record<string, { label: string; key: string; type: 'text' | 'number' }[]> = {
  Vehicles: [
    { label: 'Make & Model', key: 'Model', type: 'text' },
    { label: 'Year', key: 'Year', type: 'number' },
    { label: 'Mileage', key: 'Mileage', type: 'number' },
    { label: 'Transmission', key: 'Transmission', type: 'text' },
  ],
  'Tools & Equipment': [
    { label: 'Brand', key: 'Brand', type: 'text' },
    { label: 'Voltage / Power', key: 'Power', type: 'text' },
  ],
  Electronics: [
    { label: 'Brand', key: 'Brand', type: 'text' },
    { label: 'Model', key: 'Model', type: 'text' },
    { label: 'Megapixels / Spec', key: 'Spec', type: 'text' },
  ],
  Outdoors: [
    { label: 'Type', key: 'Type', type: 'text' },
    { label: 'Capacity / Size', key: 'Size', type: 'text' },
  ],
  'Event Gear': [
    { label: 'Type', key: 'Type', type: 'text' },
    { label: 'Capacity', key: 'Capacity', type: 'number' },
  ],
  Other: [],
};

const HANDOVER_OPTIONS = ['RenterPicksUp', 'OwnerDelivers', 'ThirdPartyDropOff'];
const MAX_PHOTOS = 8;

export function CreateListingPage() {
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [imageFiles, setImageFiles] = useState<File[]>([]);
  const [imagePreviewUrls, setImagePreviewUrls] = useState<string[]>([]);

  const [form, setForm] = useState<CreateItemPayload>({
    title: '',
    description: '',
    dailyPrice: 0,
    location: '',
    category: 'Tools & Equipment',
    attributes: {},
    instantBookEnabled: false,
    handoverOptions: [],
  });

  const set = (key: keyof CreateItemPayload, value: unknown) =>
    setForm(f => ({ ...f, [key]: value }));

  const setAttr = (key: string, value: string) =>
    setForm(f => ({ ...f, attributes: { ...f.attributes, [key]: value } }));

  const toggleHandover = (opt: string) =>
    set(
      'handoverOptions',
      form.handoverOptions.includes(opt)
        ? form.handoverOptions.filter(o => o !== opt)
        : [...form.handoverOptions, opt]
    );

  const handlePhotoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []).slice(0, MAX_PHOTOS);
    setImageFiles(files);
    setImagePreviewUrls(files.map(f => URL.createObjectURL(f)));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const { data: item } = await itemApi.create(form);
      for (const file of imageFiles) {
        await itemApi.uploadImage(item.id, file);
      }
      navigate(`/items/${item.id}`);
    } catch {
      setError('Failed to create listing. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const dynamicFields = CATEGORY_FIELDS[form.category] ?? [];

  return (
    <div className="min-h-screen bg-[#f9f9f9] font-[Manrope]">
      {/* Nav */}
      <header className="sticky top-0 z-20 bg-white/90 backdrop-blur-md shadow-[0_2px_12px_rgba(26,28,28,0.06)]">
        <div className="max-w-screen-xl mx-auto px-8 h-16 flex items-center gap-4">
          <button
            onClick={() => navigate(-1)}
            className="flex items-center gap-1 text-[#3e494b] bg-transparent border-none cursor-pointer hover:text-[#005f6c] text-sm font-semibold"
          >
            <span className="material-symbols-outlined text-base">arrow_back</span>
          </button>
          <span className="font-[Plus_Jakarta_Sans] font-black text-xl text-[#005f6c]">Borro</span>
          <h1 className="font-[Plus_Jakarta_Sans] font-bold text-xl text-[#1a1c1c] ml-4">Create a Listing</h1>
        </div>
      </header>

      <div className="max-w-screen-lg mx-auto px-8 py-10">
        {error && (
          <div className="bg-[#ffdad6] text-[#93000a] px-5 py-3 rounded-xl mb-6 text-sm font-semibold">{error}</div>
        )}

        <form onSubmit={handleSubmit}>
          {/* Two-column layout: form left, photo panel right */}
          <div className="grid lg:grid-cols-[1fr_340px] gap-8">

            {/* Left: form cards */}
            <div className="space-y-6">

              {/* Item Details card */}
              <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">Item Details</h2>
                <div className="space-y-4">

                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Category</label>
                    <select
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm font-semibold text-[#1a1c1c]"
                      value={form.category}
                      onChange={e => set('category', e.target.value)}
                    >
                      {CATEGORIES.map(c => <option key={c}>{c}</option>)}
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Title</label>
                    <input
                      required
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                      placeholder="e.g. DeWalt 20V Hammer Drill"
                      value={form.title}
                      onChange={e => set('title', e.target.value)}
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Description</label>
                    <textarea
                      required
                      rows={4}
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c] resize-none"
                      placeholder="Describe your item, condition, and any usage rules"
                      value={form.description}
                      onChange={e => set('description', e.target.value)}
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Location</label>
                    <input
                      required
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                      placeholder="City, State"
                      value={form.location}
                      onChange={e => set('location', e.target.value)}
                    />
                  </div>
                </div>
              </div>

              {/* Dynamic category specs card */}
              {dynamicFields.length > 0 && (
                <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
                  <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">{form.category} Specs</h2>
                  <div className="grid grid-cols-2 gap-4">
                    {dynamicFields.map(field => (
                      <div key={field.key}>
                        <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">{field.label}</label>
                        <input
                          type={field.type}
                          className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                          value={(form.attributes as ItemAttributes)[field.key] as string ?? ''}
                          onChange={e => setAttr(field.key, e.target.value)}
                        />
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Pricing card */}
              <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">Pricing</h2>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Daily Rate ($)</label>
                    <input
                      required
                      type="number"
                      min="1"
                      step="0.01"
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                      value={form.dailyPrice || ''}
                      onChange={e => set('dailyPrice', parseFloat(e.target.value))}
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Minimum Rental (days)</label>
                    <input
                      type="number"
                      min="1"
                      defaultValue={1}
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                    />
                  </div>
                </div>
              </div>

              {/* Handover & Options card */}
              <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">Handover & Options</h2>

                <div className="mb-4">
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-3">Handover Options</label>
                  <div className="flex flex-wrap gap-3">
                    {HANDOVER_OPTIONS.map(opt => (
                      <label key={opt} className="flex items-center gap-2 cursor-pointer bg-[#f3f3f3] rounded-xl px-4 py-2.5">
                        <input
                          type="checkbox"
                          checked={form.handoverOptions.includes(opt)}
                          onChange={() => toggleHandover(opt)}
                          className="accent-[#005f6c]"
                        />
                        <span className="text-sm font-semibold text-[#1a1c1c]">{opt.replace(/([A-Z])/g, ' $1').trim()}</span>
                      </label>
                    ))}
                  </div>
                </div>

                <label className="flex items-center gap-3 cursor-pointer bg-[#f3f3f3] rounded-xl px-4 py-3">
                  <input
                    type="checkbox"
                    checked={form.instantBookEnabled}
                    onChange={e => set('instantBookEnabled', e.target.checked)}
                    className="accent-[#005f6c]"
                  />
                  <div>
                    <p className="font-bold text-sm text-[#1a1c1c]">Enable Instant Book</p>
                    <p className="text-xs text-[#3e494b]">Renters can book without waiting for your approval</p>
                  </div>
                </label>
              </div>
            </div>

            {/* Right: sticky photo upload panel */}
            <div>
              <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)] lg:sticky lg:top-24">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-1">Photos</h2>
                <p className="text-xs text-[#3e494b] mb-4">{imageFiles.length} of {MAX_PHOTOS} uploaded</p>

                {/* Numbered photo slots */}
                <div className="grid grid-cols-3 gap-2 mb-4">
                  {Array.from({ length: MAX_PHOTOS }).map((_, i) => (
                    <div key={i} className="aspect-square rounded-xl bg-[#f3f3f3] overflow-hidden flex items-center justify-center">
                      {imagePreviewUrls[i] ? (
                        <img src={imagePreviewUrls[i]} alt="" className="w-full h-full object-cover" />
                      ) : (
                        <span className="text-[#bdc8cb] font-bold text-lg">{i + 1}</span>
                      )}
                    </div>
                  ))}
                </div>

                <label className="flex items-center justify-center gap-2 w-full border-2 border-dashed border-[#bdc8cb]/40 rounded-xl py-3 cursor-pointer hover:border-[#005f6c]/40 transition-colors">
                  <span className="material-symbols-outlined text-[#3e494b]">add_photo_alternate</span>
                  <span className="text-sm font-semibold text-[#3e494b]">Add Photos</span>
                  <input
                    type="file"
                    accept="image/*"
                    multiple
                    onChange={handlePhotoChange}
                    className="hidden"
                  />
                </label>

                <button
                  type="submit"
                  disabled={submitting}
                  className="w-full mt-6 bg-gradient-to-r from-[#005f6c] to-[#007a8a] text-white rounded-full py-4 font-bold hover:opacity-90 transition-opacity disabled:opacity-50 border-none"
                >
                  {submitting ? 'Creating...' : 'Create Listing'}
                </button>
              </div>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/features/items/CreateListingPage.tsx
git commit -m "feat: add CreateListingPage with dynamic category fields"
```

---

## Task 12: Frontend — SearchPage

**Files:**
- Create: `frontend/src/features/items/SearchPage.tsx`

- [ ] **Step 1: Create the search and filter grid** _(matches Stitch "Search Results" screen)_

```tsx
// frontend/src/features/items/SearchPage.tsx
import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { itemApi, type ItemDto } from './itemApi';

const CATEGORIES = ['All Categories', 'Vehicles', 'Tools & Equipment', 'Electronics', 'Outdoors', 'Event Gear', 'Other'];

export function SearchPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [items, setItems] = useState<ItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [pendingMaxPrice, setPendingMaxPrice] = useState(450);

  const category = searchParams.get('category') ?? '';
  const location = searchParams.get('location') ?? '';
  const maxPrice = searchParams.get('maxPrice') ? Number(searchParams.get('maxPrice')) : undefined;

  useEffect(() => {
    setLoading(true);
    itemApi
      .search({ category: category || undefined, location: location || undefined, maxPrice })
      .then(res => setItems(res.data))
      .catch(() => setItems([]))
      .finally(() => setLoading(false));
  }, [category, location, maxPrice]);

  const setFilter = (key: string, value: string) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value); else next.delete(key);
    setSearchParams(next);
  };

  const applyFilters = () => {
    setFilter('maxPrice', pendingMaxPrice < 450 ? String(pendingMaxPrice) : '');
  };

  return (
    <div className="min-h-screen bg-[#f9f9f9] font-[Manrope]">
      {/* Nav */}
      <header className="sticky top-0 z-20 bg-white/90 backdrop-blur-md shadow-[0_2px_12px_rgba(26,28,28,0.06)]">
        <div className="max-w-screen-2xl mx-auto px-8 h-16 flex items-center gap-6">
          <span className="font-[Plus_Jakarta_Sans] font-black text-2xl text-[#005f6c]">Borro</span>
          <nav className="flex gap-6 ml-auto text-sm font-semibold text-[#1a1c1c]">
            <a href="#" className="text-[#005f6c]">Explore</a>
            <Link to="/items/new">List an Item</Link>
            <a href="#">How it Works</a>
          </nav>
        </div>
      </header>

      <div className="max-w-screen-2xl mx-auto px-8 py-8 flex gap-8">

        {/* Left sidebar filters */}
        <aside className="w-72 shrink-0">
          <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
            <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-5">Filters</h2>

            <div className="mb-5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Category</label>
              <select
                className="w-full bg-[#f3f3f3] rounded-xl px-4 py-2.5 text-sm font-semibold text-[#1a1c1c]"
                value={category || 'All Categories'}
                onChange={e => setFilter('category', e.target.value === 'All Categories' ? '' : e.target.value)}
              >
                {CATEGORIES.map(c => <option key={c}>{c}</option>)}
              </select>
            </div>

            <div className="mb-5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Price Range (per day)</label>
              <div className="flex items-center justify-between text-sm text-[#3e494b] mb-2">
                <span>$0</span>
                <span>{pendingMaxPrice >= 450 ? '$450+' : `$${pendingMaxPrice}`}</span>
              </div>
              <input
                type="range" min="0" max="450" step="10"
                value={pendingMaxPrice}
                onChange={e => setPendingMaxPrice(Number(e.target.value))}
                className="w-full accent-[#005f6c]"
              />
            </div>

            <div className="mb-5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Location</label>
              <input
                className="w-full bg-[#f3f3f3] rounded-xl px-4 py-2.5 text-sm text-[#1a1c1c]"
                placeholder="City, State..."
                defaultValue={location}
                onBlur={e => setFilter('location', e.target.value)}
              />
            </div>

            <button
              onClick={applyFilters}
              className="w-full bg-[#005f6c] text-white rounded-full py-3 font-bold text-sm hover:bg-[#007a8a] transition-colors border-none cursor-pointer"
            >
              Apply Filters
            </button>
          </div>
        </aside>

        {/* Main results area */}
        <main className="flex-1 min-w-0">
          <div className="mb-6">
            <h1 className="font-[Plus_Jakarta_Sans] font-bold text-2xl text-[#1a1c1c]">
              {location ? `Items near ${location}` : 'All Items'}
            </h1>
            {!loading && (
              <p className="text-sm text-[#3e494b] mt-1">
                Showing {items.length} result{items.length !== 1 ? 's' : ''}
              </p>
            )}
          </div>

          {loading ? (
            <p className="text-[#3e494b]">Loading...</p>
          ) : items.length === 0 ? (
            <p className="text-[#3e494b]">No items found. Try adjusting the filters.</p>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
              {items.map(item => (
                <Link key={item.id} to={`/items/${item.id}`} className="group block">
                  <div className="bg-white rounded-2xl overflow-hidden shadow-[0_4px_24px_rgba(26,28,28,0.06)] hover:shadow-[0_8px_32px_rgba(26,28,28,0.10)] transition-shadow">
                    <div className="relative aspect-[4/3] overflow-hidden bg-[#e8e8e8]">
                      {item.imageUrls[0] ? (
                        <img
                          src={item.imageUrls[0]}
                          alt={item.title}
                          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                        />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-[#3e494b]">
                          <span className="material-symbols-outlined text-5xl">image</span>
                        </div>
                      )}
                      {item.instantBookEnabled && (
                        <span className="absolute top-3 left-3 bg-[#a91929]/90 text-white text-xs font-bold px-3 py-1 rounded-full backdrop-blur-sm">
                          Instant Book
                        </span>
                      )}
                      <button className="absolute top-3 right-3 w-8 h-8 bg-white/80 backdrop-blur-sm rounded-full flex items-center justify-center text-[#3e494b] hover:text-[#a91929] transition-colors border-none cursor-pointer">
                        <span className="material-symbols-outlined text-base">favorite</span>
                      </button>
                    </div>
                    <div className="p-4">
                      <h3 className="font-[Plus_Jakarta_Sans] font-bold text-[#1a1c1c] mb-1 truncate">{item.title}</h3>
                      <p className="text-xs text-[#3e494b] mb-2">{item.location}</p>
                      <div className="flex items-baseline gap-1">
                        <span className="font-black text-xl text-[#005f6c]">${item.dailyPrice}</span>
                        <span className="text-xs text-[#3e494b]">/day</span>
                      </div>
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </main>
      </div>

      {/* Floating "Show Map" pill */}
      <div className="fixed bottom-8 left-1/2 -translate-x-1/2 z-10">
        <button className="flex items-center gap-2 bg-[#1a1c1c]/80 backdrop-blur-md text-white px-5 py-3 rounded-full font-bold text-sm shadow-lg hover:bg-[#1a1c1c] transition-colors border-none cursor-pointer">
          <span className="material-symbols-outlined text-base">map</span>
          Show Map
        </button>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/features/items/SearchPage.tsx
git commit -m "feat: add SearchPage with category/location/price filters"
```

---

## Task 13: Frontend — ItemDetailPage

**Files:**
- Create: `frontend/src/features/items/ItemDetailPage.tsx`

- [ ] **Step 1: Create item detail view** _(matches Stitch "Item Detail Page" screen)_

```tsx
// frontend/src/features/items/ItemDetailPage.tsx
import { useEffect, useState } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { itemApi, type ItemDto } from './itemApi';

export function ItemDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [item, setItem] = useState<ItemDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedImage, setSelectedImage] = useState(0);

  useEffect(() => {
    if (!id) return;
    itemApi.getById(id)
      .then(res => setItem(res.data))
      .catch(() => navigate('/search'))
      .finally(() => setLoading(false));
  }, [id, navigate]);

  if (loading) return <div className="min-h-screen flex items-center justify-center font-[Manrope]">Loading...</div>;
  if (!item) return null;

  const attrs = Object.entries(item.attributes);

  return (
    <div className="min-h-screen bg-[#f9f9f9] font-[Manrope]">
      {/* Nav */}
      <header className="sticky top-0 z-20 bg-white/90 backdrop-blur-md shadow-[0_2px_12px_rgba(26,28,28,0.06)]">
        <div className="max-w-screen-xl mx-auto px-8 h-16 flex items-center gap-4">
          <button
            onClick={() => navigate(-1)}
            className="flex items-center gap-1 text-[#3e494b] bg-transparent border-none cursor-pointer hover:text-[#005f6c] transition-colors text-sm font-semibold"
          >
            <span className="material-symbols-outlined text-base">arrow_back</span> Back
          </button>
          <span className="font-[Plus_Jakarta_Sans] font-black text-xl text-[#005f6c] ml-2">Borro</span>
          <div className="flex gap-4 ml-auto text-sm font-semibold">
            <Link to="/items/new" className="text-[#1a1c1c] no-underline">List an Item</Link>
          </div>
        </div>
      </header>

      <div className="max-w-screen-xl mx-auto px-8 py-8">
        {/* Full-width photo gallery */}
        <div className="relative mb-8">
          <div className="aspect-[16/7] rounded-2xl overflow-hidden bg-[#e8e8e8]">
            {item.imageUrls[selectedImage] ? (
              <img src={item.imageUrls[selectedImage]} alt={item.title} className="w-full h-full object-cover" />
            ) : (
              <div className="w-full h-full flex items-center justify-center text-[#3e494b]">
                <span className="material-symbols-outlined text-7xl">image</span>
              </div>
            )}
          </div>
          {item.imageUrls.length > 1 && (
            <>
              <div className="flex gap-3 mt-3 overflow-x-auto pb-1">
                {item.imageUrls.map((url, i) => (
                  <button
                    key={i}
                    onClick={() => setSelectedImage(i)}
                    className={`w-20 h-16 rounded-xl overflow-hidden shrink-0 border-2 transition-all p-0 ${i === selectedImage ? 'border-[#005f6c]' : 'border-transparent opacity-60'}`}
                  >
                    <img src={url} alt="" className="w-full h-full object-cover" />
                  </button>
                ))}
              </div>
              <button className="absolute bottom-4 right-4 flex items-center gap-2 bg-white/80 backdrop-blur-sm text-[#1a1c1c] px-4 py-2 rounded-full font-semibold text-sm shadow border-none cursor-pointer">
                <span className="material-symbols-outlined text-base">grid_view</span> Show all photos
              </button>
            </>
          )}
        </div>

        {/* Two-column: details left, booking sidebar right */}
        <div className="grid lg:grid-cols-[1fr_380px] gap-10">

          {/* Left: item details */}
          <div>
            {/* Category + rating */}
            <div className="flex items-center gap-3 mb-3">
              <span className="bg-[#daf8ff] text-[#005f6c] text-xs font-bold px-3 py-1 rounded-full uppercase tracking-wider">
                {item.category}
              </span>
              <span className="flex items-center gap-1 text-sm text-[#3e494b]">
                <span className="material-symbols-outlined text-base text-amber-400">star</span>
                <span className="font-semibold">4.9</span>
                <span>(128 reviews)</span>
              </span>
            </div>

            <h1 className="font-[Plus_Jakarta_Sans] font-bold text-3xl text-[#1a1c1c] mb-2">{item.title}</h1>
            <p className="flex items-center gap-2 text-sm text-[#3e494b] mb-6">
              <span className="material-symbols-outlined text-base">category</span>{item.category}
              <span className="mx-1">·</span>
              <span className="material-symbols-outlined text-base">location_on</span>{item.location}
            </p>

            {/* Host card */}
            <div className="bg-white rounded-2xl p-5 flex items-center gap-4 shadow-[0_4px_24px_rgba(26,28,28,0.06)] mb-6">
              <div className="w-12 h-12 rounded-full bg-[#e8e8e8] flex items-center justify-center shrink-0">
                <span className="material-symbols-outlined text-2xl text-[#3e494b]">person</span>
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-xs text-[#3e494b] mb-0.5">Hosted by</p>
                <p className="font-[Plus_Jakarta_Sans] font-bold text-[#1a1c1c]">{item.ownerName}</p>
                <p className="text-xs text-[#3e494b] flex items-center gap-1">
                  <span className="material-symbols-outlined text-xs text-[#005f6c]">verified</span>
                  Identity Verified
                </p>
              </div>
              <button className="bg-[#d5e0f7] text-[#545f72] px-4 py-2 rounded-full font-semibold text-sm hover:bg-[#bcc7dd] transition-colors border-none cursor-pointer shrink-0">
                Contact Host
              </button>
            </div>

            {/* Description */}
            <div className="mb-6">
              <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-3">About this item</h2>
              <p className="text-[#3e494b] leading-relaxed">{item.description}</p>
            </div>

            {/* Technical specs */}
            {attrs.length > 0 && (
              <div className="bg-white rounded-2xl p-5 shadow-[0_4px_24px_rgba(26,28,28,0.06)] mb-6">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">Technical Specifications</h2>
                <dl className="grid grid-cols-2 gap-y-3 gap-x-6">
                  {attrs.map(([k, v]) => (
                    <div key={k} className="flex flex-col">
                      <dt className="text-xs text-[#3e494b] font-semibold uppercase tracking-wider">{k}</dt>
                      <dd className="font-semibold text-[#1a1c1c]">{String(v)}</dd>
                    </div>
                  ))}
                </dl>
              </div>
            )}

            {/* Handover options */}
            {item.handoverOptions.length > 0 && (
              <div className="mb-6">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-3">Handover Options</h2>
                <div className="flex flex-wrap gap-2">
                  {item.handoverOptions.map(opt => (
                    <span key={opt} className="bg-white rounded-full px-4 py-1.5 text-sm font-semibold text-[#1a1c1c] shadow-[0_2px_8px_rgba(26,28,28,0.06)]">
                      {opt.replace(/([A-Z])/g, ' $1').trim()}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Reviews stub */}
            <div>
              <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-3">Community Reviews</h2>
              <div className="flex items-center gap-2 mb-3">
                <span className="material-symbols-outlined text-amber-400">star</span>
                <span className="font-bold text-[#1a1c1c]">4.9</span>
              </div>
              <p className="text-sm text-[#3e494b]">Reviews will appear here once renters have completed their bookings.</p>
            </div>
          </div>

          {/* Right: sticky booking sidebar */}
          <div className="lg:sticky lg:top-24 self-start">
            <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
              <div className="flex items-baseline gap-1 mb-2">
                <span className="font-[Plus_Jakarta_Sans] font-black text-4xl text-[#005f6c]">${item.dailyPrice}</span>
                <span className="text-[#3e494b] text-sm">/ day</span>
              </div>
              {item.instantBookEnabled && (
                <span className="inline-flex items-center gap-1 text-xs font-bold text-[#a91929] bg-[#ffdad8] px-3 py-1 rounded-full mb-4">
                  <span className="material-symbols-outlined text-xs">bolt</span> Instant Book
                </span>
              )}

              {/* Date inputs */}
              <div className="bg-[#f3f3f3] rounded-xl p-4 mb-4 space-y-3">
                <div>
                  <p className="text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-1">Pick up</p>
                  <input type="date" className="w-full bg-white rounded-lg px-3 py-2 text-sm text-[#1a1c1c]" />
                </div>
                <div>
                  <p className="text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-1">Return</p>
                  <input type="date" className="w-full bg-white rounded-lg px-3 py-2 text-sm text-[#1a1c1c]" />
                </div>
              </div>

              {/* Cost breakdown */}
              <div className="space-y-2 text-sm mb-4">
                <div className="flex justify-between text-[#3e494b]">
                  <span>${item.dailyPrice} × 1 day</span><span>${item.dailyPrice}</span>
                </div>
                <div className="flex justify-between text-[#3e494b]">
                  <span>Insurance fee</span><span>$15</span>
                </div>
                <div className="flex justify-between text-[#3e494b]">
                  <span>Service fee</span><span>$12</span>
                </div>
                <div className="flex justify-between font-bold text-[#1a1c1c] pt-2 border-t border-[#bdc8cb]/20">
                  <span>Total</span><span>${item.dailyPrice + 27}</span>
                </div>
              </div>

              {/* Book CTA — Phase 3 wires to booking flow */}
              <button className="w-full bg-gradient-to-r from-[#005f6c] to-[#007a8a] text-white rounded-full py-4 font-bold text-base hover:opacity-90 transition-opacity border-none cursor-pointer mb-2">
                {item.instantBookEnabled ? 'Book Now' : 'Request to Book'}
              </button>
              <p className="text-center text-xs text-[#3e494b]">You won't be charged yet</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/features/items/ItemDetailPage.tsx
git commit -m "feat: add ItemDetailPage with image gallery, specs, and book button stub"
```

---

## Task 14: Wire Routes in App.tsx

**Files:**
- Modify: `frontend/src/App.tsx`

- [ ] **Step 1: Add item routes to App.tsx**

Current `App.tsx` routes section:
```tsx
<Route path="/login" element={<LoginPage />} />
<Route path="/register" element={<RegisterPage />} />
<Route path="/" element={<ProtectedRoute><HomePage /></ProtectedRoute>} />
<Route path="*" element={<Navigate to="/" replace />} />
```

Replace with:
```tsx
import { CreateListingPage } from './features/items/CreateListingPage';
import { ItemDetailPage } from './features/items/ItemDetailPage';
import { SearchPage } from './features/items/SearchPage';

// ...inside <Routes>:
<Route path="/login" element={<LoginPage />} />
<Route path="/register" element={<RegisterPage />} />
<Route path="/" element={<ProtectedRoute><HomePage /></ProtectedRoute>} />
<Route path="/search" element={<ProtectedRoute><SearchPage /></ProtectedRoute>} />
<Route path="/items/new" element={<ProtectedRoute><CreateListingPage /></ProtectedRoute>} />
<Route path="/items/:id" element={<ProtectedRoute><ItemDetailPage /></ProtectedRoute>} />
<Route path="*" element={<Navigate to="/" replace />} />
```

- [ ] **Step 2: Connect "List an Item" nav link in HomePage.tsx**

In `frontend/src/features/home/HomePage.tsx`, replace:
```tsx
<a href="#" className="text-primary font-semibold border-b-2 border-primary transition-colors">List an Item</a>
```
with:
```tsx
<Link to="/items/new" className="text-primary font-semibold border-b-2 border-primary transition-colors">List an Item</Link>
```

Add the import at the top of `HomePage.tsx`:
```tsx
import { Link } from 'react-router-dom';
```

Also replace the "Explore" search button `onClick` stub with navigation:
```tsx
// In the hero search bar's Explore button:
import { useNavigate } from 'react-router-dom';
// ...
const navigate = useNavigate();
// button onClick:
onClick={() => navigate('/search')}
```

- [ ] **Step 3: Run frontend and verify pages load**

```bash
cd frontend
npm run dev
```

Open http://localhost:5173/search — filter bar renders, items list is empty (no data yet).
Open http://localhost:5173/items/new — dynamic form renders, category switching shows/hides fields.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/
git commit -m "feat: wire item routes in App.tsx and connect nav links in HomePage"
```

---

## Phase 2 Complete

At this point:
- Backend: Item CRUD + image upload + search endpoint all functional with tests
- Frontend: CreateListingPage, SearchPage, ItemDetailPage all route correctly
- MinIO integration ready (stub returns empty availability for Phase 3)

Proceed to **Phase 3** plan: `2026-04-16-phase-3-booking-state-machine-chat.md`
