# 🚀 Borro ("Airbnb for Everything") - AI Implementation Plan

**Context for the AI Coding Agent:**
This document serves as the master blueprint for the MVP of **Borro**, a web-based peer-to-peer rental application. You will execute this plan phase by phase. Do not deviate from the tech stack or architectural rules.

## 🛠️ Global Tech Stack & Architectural Rules
- **Frontend:** React (Vite) + TypeScript + Tailwind CSS (Responsive Web ONLY. No mobile wrappers).
- **Backend:** ASP.NET Core (C#) using **Minimal APIs**.
- **Database:** PostgreSQL running via Docker.
- **ORM:** Entity Framework (EF) Core (using `Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Storage:** MinIO (S3-compatible) running via Docker for image uploads.
- **Real-Time:** SignalR (for in-app messaging).
- **Rules:**
  - **Timezone Safety:** ALWAYS store timestamps (`CreatedAt`, `StartDate`, `EndDate`) in **UTC** (`DateTime.UtcNow`). The React frontend must handle converting UTC to the user's local timezone.
  - **Dynamic Data Requirement:** The `Item` entity must use EF Core's native JSON mapping (e.g., `.ToJson()`) to store category-specific attributes in a PostgreSQL `JSONB` column. 
  - Strictly follow the minimal API architecture (keep `Program.cs` clean by using extension methods for route groups).

---

## 📦 Phase 1: Infrastructure, Auth & Base Setup
**Objective:** Spin up the local environment, establish the .NET architecture, and handle authentication.

### 1.1 Docker & Environment
- **Action:** Create `docker-compose.yml` in the root.
- **Services:**
  - `db`: `postgres:15`. Expose host port 5234 (mapped to container 5432).
  - `minio`: `minio/minio`. Expose ports 9000 and 9001. Create a `borro-assets` bucket.

### 1.2 EF Core Setup & User Schema
- **Action:** Initialize the ASP.NET Core Web API project and configure EF Core with Npgsql.
- **Model:** `User`
  - Fields: `Id` (Guid), `Email`, `PasswordHash`, `FirstName`, `LastName`, `ProfilePicUrl`, `CreatedAtUtc`, `UpdatedAtUtc`.
- **Backend Auth:** Implement JWT authentication. Create Minimal API endpoints: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`.
- **Frontend Auth:** Initialize React (Vite). Set up Axios interceptors for JWT. Create Login/Register views.

---

## 🧰 Phase 2: Dynamic Listing Engine & Discovery
**Objective:** Allow lenders to list items across varying categories using EF Core's JSON mapping.

### 2.1 EF Core Setup: Items & JSONB
- **Action:** Create the `Item` model.
  - Standard Fields: `Id` (Guid), `OwnerId` (Guid), `Title`, `Description`, `DailyPrice` (Decimal), `Location` (String), `Category` (String).
  - **Dynamic Field:** `Attributes` (Mapped to `JSONB` via EF Core `.ToJson()`. Stores dynamic details like `{"Mileage": 50000, "Megapixels": 24}`).
  - Settings: `InstantBookEnabled` (Bool), `HandoverOptions` (Enum Array).
  - Storage: `ImageUrls` (List<string>).
- **Model:** `Wishlist` (Maps `UserId` to `ItemId`).

### 2.2 Backend: Item Management
- **Action:** Create `ItemEndpoints` route group.
- **Implementation:**
  - AWS SDK for .NET (configured for MinIO) to handle image uploads (`POST /api/items/images`).
  - CRUD Minimal APIs for items. Ensure queries can filter by fields *inside* the JSONB column.
  - **Endpoint:** `GET /api/items/search` (supports filtering by dates, price, category).
  - **Endpoint:** `GET /api/items/{id}/availability` to fetch blocked dates.

### 2.3 Frontend: Dynamic UI & Discovery
- **Action:** Create `CreateListing.tsx` and `Search.tsx`.
- **Implementation:**
  - **Dynamic Form:** Render inputs conditionally based on category selection (e.g., "Bedrooms" vs "Brand"). Serialize these into a JSON object before sending the POST request.
  - **Filters:** Build UI for dates, price range, and delivery options.

---

## 🔄 Phase 3: The Booking State Machine & Real-Time Chat
**Objective:** Implement strict rental lifecycles and SignalR communication.

### 3.1 EF Core Setup: Bookings & Messages
- **Model:** `Booking`
  - Fields: `Id`, `ItemId`, `RenterId`, `StartDateUtc` (DateTime), `EndDateUtc` (DateTime), `TotalPrice` (Decimal).
  - **Crucial Field - State Machine:** `Status` (Enum: `PendingApproval`, `Approved`, `PaymentHeld`, `Active`, `Completed`, `Disputed`).
- **Model:** `Message` (Fields: `Id`, `BookingId`, `SenderId`, `Content`, `CreatedAtUtc`).

### 3.2 Backend: Booking Lifecycle & SignalR
- **Action:** Create `BookingEndpoints` and `ChatHub`.
- **Implementation:**
  - **State Machine Logic:** Enforce strict state transitions in the API. Reject requests that bypass required states.
  - **Instant Book vs Request:** If an item has `InstantBookEnabled`, transition immediately to `Approved`.
  - **Real-Time Chat:** Set up SignalR to broadcast messages to users within a specific `BookingId` group.

### 3.3 Frontend: Booking Flow & Messaging
- **Implementation:**
  - Create `BookingDetail.tsx` showing the current status (e.g., a progress timeline).
  - Implement a SignalR client in React for real-time messaging without sharing phone numbers.

---

## 🛡️ Phase 4: Trust, Security & Payments
**Objective:** Manage funds, late fees, and condition verification.

### 4.1 EF Core Setup: Trust Mechanisms
- **Model:** `Review` (Two-Way Rating)
  - Fields: `Id`, `BookingId`, `ReviewerId`, `TargetUserId`, `Rating` (1-5), `Comment`.
- **Model:** `ConditionReport` (Pre/Post Checklists)
  - Fields: `Id`, `BookingId`, `Type` (Enum: `Pickup`, `Dropoff`), `PhotoUrls` (List<string>), `Notes`.

### 4.2 Backend: Stripe, Escrow & Background Jobs
- **Action:** Integrate Stripe & `IHostedService`.
- **Implementation:**
  - **Escrow (Stripe Connect):** When booking is `Approved`, create a hold on the renter's card (`PaymentHeld`). Use a background worker or Stripe Webhooks to capture and route funds 24 hours *after* `StartDateUtc`.
  - **Automated Late Fees:** Create an ASP.NET Core `BackgroundService` that runs periodically. If an `Active` booking's `EndDateUtc` has passed, trigger a penalty charge.
  - **Condition Reports:** Endpoints to handle MinIO image uploads for pickup/drop-off.

### 4.3 Frontend: Mandatory Web Checklists
- **Implementation:**
  - Create `HandoverChecklist.tsx`. This view forces the renter to upload photos via the browser before unlocking the API call to update the booking to `Active` (Pickup) or `Completed` (Drop-off).
  - Build the rating prompt for completed transactions.
