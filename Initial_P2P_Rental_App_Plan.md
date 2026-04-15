# 🚀 Universal P2P Rental App - Initial MVP Implementation Plan

**Context for the AI Coding Agent:**
This document serves as the master blueprint for the initial MVP of a cross-platform peer-to-peer rental application. You will execute this plan phase by phase. Do not skip ahead. 

## 🛠️ Global Tech Stack & Architectural Rules
- **Frontend Repo:** React (Vite) + Capacitor (by Ionic) + TypeScript + Tailwind CSS + Apollo Client.
- **Backend Repo:** NestJS + TypeScript + GraphQL (Code-First Approach using `@nestjs/graphql`).
- **Database:** PostgreSQL running via Docker.
- **ORM:** Prisma.
- **Storage:** MinIO (S3-compatible) running via Docker.
- **Rules:**
  - **Timezone Safety:** ALWAYS store timestamps (`createdAt`, `startDate`, `endDate`) in **UTC** in the database. The frontend must handle converting UTC to the user's local timezone using a library like `date-fns` or `dayjs`.
  - Strict TypeScript everywhere. Share generated GraphQL types between frontend and backend.
  - Backend must follow NestJS Module architecture.

---

## 📦 Phase 1: Infrastructure, Auth & Base Setup
**Objective:** Spin up the local environment, define core users, and handle storage.

### 1.1 Docker & Environment
- **Action:** Create `docker-compose.yml` in the root.
- **Services:**
  - `db`: `postgres:15` (or `postgis/postgis` if spatial features are needed later). Expose port 5432.
  - `minio`: `minio/minio` (Local S3 storage). Expose ports 9000 and 9001. Create a `rentals-bucket`.

### 1.2 Prisma Setup & User Schema
- **Action:** Initialize Prisma (`npx prisma init`).
- **Model:** `User`
  - Fields: `id` (UUID), `email`, `passwordHash`, `firstName`, `lastName`, `profilePicUrl`, `createdAt`, `updatedAt`.
- **Backend Auth:** Generate `AuthModule`. Implement JWT-based authentication. Create `register`, `login`, and `me` GraphQL endpoints.
- **Frontend Auth:** Setup Apollo Provider with JWT headers. Create Login/Register screens.

---

## 🧰 Phase 2: Dynamic Listing Engine & Discovery
**Objective:** Allow lenders to list *anything* using dynamic JSONB data, and allow renters to discover them.

### 2.1 Prisma Setup: Items & Wishlists
- **Action:** Expand Prisma schema.
- **Model:** `Item`
  - Standard Fields: `id`, `ownerId`, `title`, `description`, `dailyPrice` (Float), `location` (String/Coords), `category` (String).
  - **Dynamic Field:** `attributes` (Type: `Json` - Stores category-specific details like `{"mileage": 50000, "megapixels": 24}`).
  - Booking Preferences: `instantBookEnabled` (Boolean).
  - Logistics: `handoverOptions` (Enum Array: `LOCAL_PICKUP`, `DELIVERY`, `DIGITAL`).
  - Storage: `imageUrls` (String[]).
- **Model:** `Wishlist` (Maps `userId` to `itemId` for saved searches).

### 2.2 Backend: Item Management
- **Action:** Generate `ItemsModule` and `WishlistModule`.
- **Implementation:**
  - S3/MinIO upload mutations for item images.
  - CRUD mutations for items utilizing Prisma's JSON querying capabilities.
  - **Query:** `searchItems(dates, minPrice, maxPrice, category, handoverType)`.
  - **Query:** `getAvailabilityCalendar(itemId)` to fetch blocked dates.

### 2.3 Frontend: Dynamic UI & Filters
- **Action:** Create `CreateListingScreen.tsx` and `DiscoveryScreen.tsx`.
- **Implementation:**
  - **Dynamic Form:** Render inputs conditionally based on the chosen category (e.g., "Bedrooms" for apartments, "Brand" for cameras). Pack these into a JSON object before sending the GraphQL mutation.
  - **Filters:** Build UI for "Advanced Filtering" (dates, price, categories, delivery options).
  - **Wishlists:** Add a heart icon to save items to the user's wishlist.

---

## 🔄 Phase 3: The Booking State Machine & Logistics
**Objective:** Implement strict rental lifecycles and real-time communication.

### 3.1 Prisma Setup: Bookings & Messages
- **Action:** Expand Prisma schema.
- **Model:** `Booking`
  - Fields: `id`, `itemId`, `renterId`, `startDate` (DateTime UTC), `endDate` (DateTime UTC), `totalPrice` (Float).
  - **Crucial Field - State Machine:** `status` (Enum: `PENDING_APPROVAL`, `APPROVED`, `PAYMENT_HELD`, `ACTIVE`, `COMPLETED`, `DISPUTED`).
- **Model:** `Message` (Fields: `id`, `bookingId` or `threadId`, `senderId`, `content`, `createdAt`).

### 3.2 Backend: Booking Lifecycle & Chat
- **Action:** Generate `BookingsModule` and `ChatModule`.
- **Implementation:**
  - **State Machine Logic:** Enforce strict transitions. A booking cannot go from `PENDING_APPROVAL` directly to `ACTIVE`.
  - **Instant Book vs Request:** If an item has `instantBookEnabled == true`, bypass `PENDING_APPROVAL` and go straight to `APPROVED`/`PAYMENT_HELD`.
  - **Real-Time Chat:** Use `@nestjs/websockets` (Socket.io) or GraphQL Subscriptions to allow real-time in-app messaging between lender and renter.

### 3.3 Frontend: In-App Messaging
- **Action:** Create `ChatScreen.tsx` and `BookingDetailScreen.tsx`.
- **Implementation:**
  - Build real-time chat UI so users don't share phone numbers.
  - Show the current booking status visually (e.g., a progress timeline).

---

## 🛡️ Phase 4: Trust, Security & Payments
**Objective:** Handle the money, enforce accountability, and manage condition checklists.

### 4.1 Prisma Setup: Trust Mechanisms
- **Action:** Expand Prisma schema.
- **Model:** `Review` (Two-Way Rating)
  - Fields: `bookingId`, `reviewerId`, `targetUserId`, `rating` (1-5), `comment`.
- **Model:** `ConditionReport` (Pre/Post Checklists)
  - Fields: `bookingId`, `type` (Enum: `PICKUP`, `DROPOFF`), `photos` (String[]), `notes` (String).

### 4.2 Backend: Escrow, Late Fees & Checklists
- **Action:** Generate `PaymentsModule` and `TrustModule`.
- **Implementation:**
  - **Escrow Payouts (Stripe):** Implement Stripe Connect. When booking is `APPROVED`, place a hold on the renter's card (`PAYMENT_HELD`). Release funds to the lender 24 hours *after* `startDate` (trigger via cron job or Stripe Webhooks).
  - **Late Fees:** Use `@nestjs/schedule` to run a Cron Job checking for `ACTIVE` bookings past their UTC `endDate`. Automatically apply penalty charges via Stripe.
  - **Condition Reports:** Mutations to save MinIO image URLs for pickup and drop-off conditions.

### 4.3 Frontend: Mandatory Checklists & Ratings
- **Action:** Create `HandoverChecklistScreen.tsx` and `ReviewScreen.tsx`.
- **Implementation:**
  - **Mandatory Flow:** Use Capacitor Camera to force the renter to take photos of the item's condition at pickup before the app updates the booking state to `ACTIVE`. Require the same for drop-off to transition to `COMPLETED`.
  - Build the rating UI prompting users to rate each other after a completed transaction.
