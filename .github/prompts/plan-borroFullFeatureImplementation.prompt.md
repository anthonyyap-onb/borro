# Plan: Borro Full Feature Implementation

## Current State
- Backend: Clean skeleton. Domain has `User` + `Item` (with JSONB `Attributes`). Only `/api/health` endpoint. No auth, no booking, no SignalR, no Stripe.
- Frontend: Single health-check page. `react-router-dom` installed but unused. No components, pages, or routes.
- DB: `Users` + `Items` tables only (via EF migration).

---

## Phase 0: Foundation — Auth & App Shell

### F0.1 — Auth Backend (JWT)
- Add `PasswordHash` to `User` entity
- New migration
- `RegisterCommand`, `LoginQuery` handlers (MediatR)
- JWT generation util (claim: `sub`, `email`, `role`)
- `POST /api/auth/register` + `POST /api/auth/login` endpoints
- Add `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet

### F0.2 — Auth Frontend
- `AuthContext` (React Context + localStorage JWT)
- `LoginPage` + `RegisterPage` forms (axios calls)
- `ProtectedRoute` wrapper
- App shell: nav bar with login/logout, routing setup in `App.tsx` via `react-router-dom`

---

## Phase 1: Item Listings (Lender Core)

### F1.1 — Expand Item Domain
- Add to `Item`: `string Description`, `Guid LenderId` (FK → User), `bool InstantBookEnabled`, `bool DeliveryAvailable`, `string[] ImageUrls`
- New migration
- Seed update

### F1.2 — Create & Get Listing APIs
- `POST /api/items` — `CreateItemCommand` (auth required, sets `LenderId`)
- `GET /api/items/{id}` — `GetItemByIdQuery`
- `GET /api/items/my` — lender's own listings

### F1.3 — Dynamic Listing Form UI
- `CreateListingPage` with category-driven dynamic fields
- Category → attributes schema map (e.g., `Vehicle` → Mileage, Transmission; `RealEstate` → Bedrooms)
- Form serializes extra fields into `attributes` object sent to API

### F1.4 — Availability (BlockedDate) Backend
- New `BlockedDate` entity: `Guid Id`, `Guid ItemId`, `DateOnly Date`
- New migration
- `POST /api/items/{id}/blocked-dates` — `BlockDatesCommand`
- `DELETE /api/items/{id}/blocked-dates` — `UnblockDatesCommand`
- `GET /api/items/{id}/blocked-dates` — `GetBlockedDatesQuery`

### F1.5 — Availability Calendar UI
- Calendar component on lender's listing management page
- Blocked dates shown in red; click to toggle block/unblock
- Calls blocked-dates API

---

## Phase 2: Discovery & Search (Renter Core)

### F2.1 — Search API
- `GET /api/items/search?category=&minPrice=&maxPrice=&startDate=&endDate=&delivery=`
- `SearchItemsQuery` handler: filter by category, price range, delivery flag
- Date filter: exclude items with any `BlockedDate` in the requested range
- JSONB attribute filter: optional `attributeKey`/`attributeValue` params

### F2.2 — Search & Listing UI
- `SearchPage`: filter sidebar + results grid (`ItemCard` component)
- `ItemDetailPage`: full listing view with attributes, availability calendar (read-only), lender info

### F2.3 — Wishlist Backend
- New `Wishlist` entity: `Guid UserId`, `Guid ItemId`, composite PK
- New migration
- `POST /api/wishlist/{itemId}` — toggle (add/remove)
- `GET /api/wishlist` — renter's saved items

### F2.4 — Wishlist UI
- Heart icon on `ItemCard` / `ItemDetailPage` (calls toggle API)
- `WishlistPage` showing saved items

---

## Phase 3: Booking State Machine

### F3.1 — Booking Domain
- New `Booking` entity: `Guid Id`, `Guid ItemId`, `Guid RenterId`, `BookingStatus Status` (enum), `DateTime StartDateUtc`, `DateTime EndDateUtc`, `decimal TotalPrice`, `string? StripePaymentIntentId`, timestamps
- `BookingStatus` enum: `PendingApproval`, `Approved`, `PaymentHeld`, `Active`, `Completed`, `Disputed`
- New migration

### F3.2 — Booking API Endpoints
- `POST /api/bookings` — `CreateBookingCommand` (sets `PendingApproval` or skips to `Approved` if `InstantBookEnabled`)
- `PUT /api/bookings/{id}/approve` — lender only; `PendingApproval → Approved`
- `PUT /api/bookings/{id}/dispute` — either party; from `PaymentHeld`/`Active`/`Completed → Disputed`
- `GET /api/bookings/{id}` + `GET /api/bookings/my`
- State machine guard: reject invalid transitions with `400`

### F3.3 — Booking UI (Renter)
- "Request to Book" button on `ItemDetailPage` (date picker + submit)
- `MyBookingsPage` — renter's bookings with status badges

### F3.4 — Booking UI (Lender)
- `LenderBookingsPage` — pending requests with approve/reject actions

---

## Phase 4: Trust & Condition Logistics

### F4.1 — MinIO Integration
- Add `Minio` NuGet package to Infrastructure
- `IFileStorageService` / `MinioFileStorageService` registered in DI
- MinIO configured in `docker-compose.yml`

### F4.2 — Photo Checklist API
- New `BookingPhoto` entity: `Guid Id`, `Guid BookingId`, `string Url`, `PhotoType` (`PreRental`/`PostRental`), `DateTime UploadedAtUtc`
- New migration
- `POST /api/bookings/{id}/photos` — upload photo (multipart), store in MinIO, save URL
- After pre-rental upload: transition `PaymentHeld → Active`
- After post-rental upload: transition `Active → Completed`

### F4.3 — Photo Checklist UI
- `PhotoChecklistPage` with file/camera input (web `<input type="file" capture>`)
- Upload button calls API; on success updates booking status in UI

---

## Phase 5: Payments & Escrow (Stripe)

### F5.1 — Stripe Setup
- Add `Stripe.net` NuGet
- Config: `Stripe:SecretKey`, `Stripe:WebhookSecret`
- `IPaymentService` / `StripePaymentService` in Infrastructure

### F5.2 — Payment Hold on Approval
- On `Approved → PaymentHeld`: create Stripe `PaymentIntent` with `capture_method: manual`; store `StripePaymentIntentId` on `Booking`
- Webhook: confirm hold → update DB

### F5.3 — Payout Background Service
- `IHostedService` (`PayoutBackgroundService`)
- Every 5 min: find bookings `PaymentHeld`/`Active` where `StartDateUtc + 24h ≤ Now`; capture Stripe PaymentIntent; release to lender connected account

### F5.4 — Late Fees Background Service
- `IHostedService` (`LateFeeBackgroundService`)
- Every 15 min: find bookings `Active` where `EndDateUtc < Now`; charge additional fee via Stripe

---

## Phase 6: Real-Time Chat (SignalR)

### F6.1 — SignalR Hub & Message Persistence
- New `ChatMessage` entity: `Guid Id`, `Guid BookingId`, `Guid SenderId`, `string Content`, `DateTime SentAtUtc`
- New migration
- `ChatHub` with `SendMessage(bookingId, content)` — broadcasts to group `booking-{id}`
- `GET /api/bookings/{id}/messages` — message history
- Wire up `AddSignalR()` + `MapHub<ChatHub>` in `Program.cs`

### F6.2 — Chat UI
- `ChatPage` (accessible only when booking `>= Approved`)
- `@microsoft/signalr` npm package
- `useChat(bookingId)` hook managing SignalR connection + message state
- Message bubble UI

---

## Phase 7: Ratings

### F7.1 — Rating Backend
- New `Rating` entity: `Guid Id`, `Guid BookingId`, `Guid RaterId`, `Guid RateeId`, `int Stars` (1-5), `string? Comment`, `DateTime CreatedAtUtc`
- New migration
- `POST /api/bookings/{id}/ratings` — submit rating (once per user per booking; booking must be `Completed`)
- `GET /api/users/{id}/ratings` — user's received ratings

### F7.2 — Rating UI
- Modal/page prompted automatically when booking hits `Completed`
- Star selector (1-5) + optional comment
- Two-way: both lender and renter prompted independently

---

## Dependencies
- Phase 0 must come first (auth gates everything)
- Phases 1–2 can overlap
- Phase 3 requires Phase 1
- Phases 4–7 each depend on Phase 3

## Key Decisions
- Auth: JWT (stateless), no external OAuth
- Storage: MinIO for photos
- All `DateTime` stored as UTC in DB; frontend converts for display
- Category attribute schemas defined as a static map in the frontend (not dynamic from DB)
- Stripe: `PaymentIntent` with manual capture for escrow behavior
