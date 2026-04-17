# Phase 4 Task 7: Update BorroDbContext + EF Migration

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `ConditionReport` and `Review` DbSets and entity configurations to `BorroDbContext`, generate and apply the EF Core migration.

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Tasks 1, 2, 5, 6 (entities and DbContext interfaces must exist).

**Tech Stack:** .NET 9, EF Core 9 + Npgsql

---

## File Map

| Action | File |
|--------|------|
| Modify | `backend/Borro.Infrastructure/Persistence/BorroDbContext.cs` |

---

- [ ] **Step 1: Add ConditionReport + Review to BorroDbContext**

Add DbSets after `Messages`:
```csharp
public DbSet<ConditionReport> ConditionReports => Set<ConditionReport>();
public DbSet<Review> Reviews => Set<Review>();
```

Add entity configurations inside `OnModelCreating` after Message config:

```csharp
modelBuilder.Entity<ConditionReport>(entity =>
{
    entity.HasKey(r => r.Id);
    entity.Property(r => r.Notes).HasMaxLength(2000);
    entity.Property(r => r.Type).HasConversion<string>();
    entity.Property(r => r.PhotoUrls).HasColumnType("text[]");

    entity.HasOne(r => r.Booking).WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(r => r.SubmittedBy).WithMany().HasForeignKey(r => r.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<Review>(entity =>
{
    entity.HasKey(r => r.Id);
    entity.HasIndex(r => new { r.BookingId, r.ReviewerId }).IsUnique();
    entity.Property(r => r.Comment).HasMaxLength(1000);

    entity.HasOne(r => r.Booking).WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(r => r.Reviewer).WithMany().HasForeignKey(r => r.ReviewerId).OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(r => r.Target).WithMany().HasForeignKey(r => r.TargetUserId).OnDelete(DeleteBehavior.Restrict);
});
```

- [ ] **Step 2: Add migration**

```bash
cd backend
dotnet ef migrations add Phase4_ReviewsAndConditionReports \
  --project Borro.Infrastructure \
  --startup-project Borro.Api
```

- [ ] **Step 3: Apply migration**

```bash
cd backend
dotnet ef database update \
  --project Borro.Infrastructure \
  --startup-project Borro.Api
```

Expected: `Done.`

- [ ] **Step 4: Run all tests**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj -v minimal
```

Expected: All passing (at minimum the 12+ tests from Phases 2/3/4).

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Infrastructure/Persistence/
git commit -m "feat: add ConditionReport/Review to BorroDbContext with Phase4 migration"
```
