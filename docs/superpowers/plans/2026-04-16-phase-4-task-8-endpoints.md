# Phase 4 Task 8: ConditionReport & Review Endpoints

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create `ConditionReportEndpoints` and `ReviewEndpoints`, wire them in `Program.cs`, and add the `hold-payment` route to `BookingEndpoints`.

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Tasks 1–7.

**Tech Stack:** .NET 9, ASP.NET Core Minimal APIs, MediatR 12.5

---

## File Map

| Action | File |
|--------|------|
| Create | `backend/Borro.Api/Endpoints/ConditionReportEndpoints.cs` |
| Create | `backend/Borro.Api/Endpoints/ReviewEndpoints.cs` |
| Modify | `backend/Borro.Api/Program.cs` |
| Modify | `backend/Borro.Api/Endpoints/BookingEndpoints.cs` |

---

- [ ] **Step 1: Create ConditionReportEndpoints**

```csharp
// backend/Borro.Api/Endpoints/ConditionReportEndpoints.cs
using Borro.Application.ConditionReports.Commands.CreateReport;
using Borro.Domain.Enums;
using MediatR;
using System.Security.Claims;

namespace Borro.Api.Endpoints;

public static class ConditionReportEndpoints
{
    public static IEndpointRouteBuilder MapConditionReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/condition-reports").WithTags("ConditionReports").RequireAuthorization();

        // POST /api/condition-reports  (multipart/form-data with photos)
        group.MapPost("/", async (HttpRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            if (!request.Form.TryGetValue("bookingId", out var bookingIdStr)
                || !Guid.TryParse(bookingIdStr, out var bookingId))
                return Results.BadRequest(new { error = "bookingId is required." });

            if (!request.Form.TryGetValue("type", out var typeStr)
                || !Enum.TryParse<ConditionReportType>(typeStr, out var reportType))
                return Results.BadRequest(new { error = "type must be 'Pickup' or 'Dropoff'." });

            var notes = request.Form.TryGetValue("notes", out var n) ? n.ToString() : string.Empty;
            var files = request.Form.Files;

            if (files.Count == 0)
                return Results.BadRequest(new { error = "At least one photo is required." });

            var streams = files.Select(f => f.OpenReadStream()).ToList();
            var fileNames = files.Select(f => f.FileName).ToList();
            var contentTypes = files.Select(f => f.ContentType).ToList();

            try
            {
                var reportId = await mediator.Send(new CreateConditionReportCommand(
                    bookingId, userId, reportType, streams, fileNames, contentTypes, notes), ct);
                return Results.Created($"/api/condition-reports/{reportId}", new { id = reportId });
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            finally
            {
                foreach (var s in streams) await s.DisposeAsync();
            }
        });

        return app;
    }
}
```

- [ ] **Step 2: Create ReviewEndpoints**

```csharp
// backend/Borro.Api/Endpoints/ReviewEndpoints.cs
using Borro.Application.Reviews.Commands.CreateReview;
using Borro.Application.Reviews.Queries.GetReviews;
using MediatR;
using System.Security.Claims;

namespace Borro.Api.Endpoints;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reviews").WithTags("Reviews");

        // GET /api/reviews?userId=<guid>  — public
        group.MapGet("/", async (Guid userId, IMediator mediator, CancellationToken ct) =>
        {
            var reviews = await mediator.Send(new GetReviewsQuery(userId), ct);
            return Results.Ok(reviews);
        });

        // POST /api/reviews  (requires auth)
        group.MapPost("/", async (CreateReviewRequest req, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var reviewerId))
                return Results.Unauthorized();

            try
            {
                var review = await mediator.Send(
                    new CreateReviewCommand(req.BookingId, reviewerId, req.TargetUserId, req.Rating, req.Comment), ct);
                return Results.Created($"/api/reviews/{review.Id}", review);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        }).RequireAuthorization();

        return app;
    }

    private record CreateReviewRequest(Guid BookingId, Guid TargetUserId, int Rating, string Comment);
}
```

- [ ] **Step 3: Wire endpoints in Program.cs**

After `app.MapBookingEndpoints();`, add:
```csharp
app.MapConditionReportEndpoints();
app.MapReviewEndpoints();
```

- [ ] **Step 4: Add hold-payment route to BookingEndpoints.cs**

Add this route inside the group after the PATCH status route:

```csharp
// POST /api/bookings/{id}/hold-payment  — renter confirms payment, Stripe hold created
group.MapPost("/{id:guid}/hold-payment", async (Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    try
    {
        var booking = await mediator.Send(new HoldPaymentCommand(id, userId.Value), ct);
        return Results.Ok(booking);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (UnauthorizedAccessException) { return Results.Forbid(); }
});
```

Add using at the top of `BookingEndpoints.cs`:
```csharp
using Borro.Application.Payments.Commands.HoldPayment;
```

- [ ] **Step 5: Build full solution**

```bash
cd backend
dotnet build Borro.sln
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add backend/Borro.Api/
git commit -m "feat: add ConditionReport and Review endpoints, wire HoldPayment into BookingEndpoints"
```
