using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Tests.Infrastructure;

public static class TestDbContextFactory
{
    public static TestDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
