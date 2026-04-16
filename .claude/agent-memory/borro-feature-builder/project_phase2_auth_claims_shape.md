---
name: Phase 2 Auth Claims Shape
description: JWT claim shape expected by Phase 2 endpoints — must be matched by Phase 1 JWT middleware
type: project
---

Phase 2 Minimal API endpoints will read the authenticated user's id from the JWT `sub` claim.

Required claim:
- **Claim type:** `sub`
- **Value format:** `Guid` serialized as a lowercase string (e.g., `"3fa85f64-5717-4562-b3fc-2c963f66afa6"`)

**Why:** Standard JWT `sub` claim is used for portability and simplicity. Phase 1 must emit this claim when issuing tokens. Phase 2 endpoints parse it via `Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"))` or equivalent.

**How to apply:** Before implementing Phase 2 endpoints (Sub-Phase E), verify with Phase 1 contributor that their `JwtSecurityTokenHandler` / identity middleware emits `sub` = `UserId.ToString()`. If they use `ClaimTypes.NameIdentifier` instead of literal `"sub"`, adjust the endpoint claim parsing accordingly (they are equivalent under ASP.NET Core's JWT middleware which maps `sub` → `NameIdentifier`).
