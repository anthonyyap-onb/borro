# 🤖 Borro - Business Requirements

**App Overview:** "Borro" is a responsive web application (no mobile apps) serving as a universal peer-to-peer rental marketplace ("Airbnb for everything").
**Tech Stack Reminder:** React Web, C# ASP.NET Core (Minimal APIs), Clean Architecture, MediatR (CQRS), EF Core (Npgsql), PostgreSQL, MinIO, SignalR.

---

## 🛠️ Agent Operational Directives

### Strategy
* Write a plan with success criteria for each phase to be checked off. Include rigorous unit testing.
* Execute the plan ensuring all criteria are met.
* Only complete when the feature being worked on is finished and tested.

### Coding Standards
* Use latest versions of libraries and idiomatic approaches as of today.
* Keep it simple. NEVER over-engineer, ALWAYS simplify. No extra features, focus on simplicity.

---

## 📋 Business Requirements

### 1. Dynamic Listing Engine (Lender Core)
* **Behavior:** Lenders can list any type of item. The UI forms must dynamically change based on the selected `Category`.
    * *Example:* If `Category == 'Vehicle'`, ask for 'Mileage' and 'Transmission'. If `Category == 'RealEstate'`, ask for 'Bedrooms'.
* **Data Interaction:** These dynamic fields must be serialized and saved exclusively into a `JSONB` column named `Attributes` on the `Item` table using EF Core's native `.ToJson()` mapping.
* **Availability Interaction:** Lenders must be able to view a calendar UI and select specific dates to block out (making the item unavailable for renters on those dates).
* **Booking Preference Interaction:** Lenders toggle a boolean `InstantBookEnabled`. This directly dictates how the Booking State Machine behaves (see Section 4).

### 2. Discovery & Search (Renter Core)
* **Behavior:** Renters must be able to search for items using advanced filters (Dates, Price Range, Category, and Delivery Options).
* **Data Interaction:** The backend API must be able to query and filter against properties stored *inside* the `JSONB` Attributes column.
* **Wishlists:** Renters can click a heart icon to save an item to their `Wishlist` (maps `UserId` to `ItemId`). 

### 3. Trust & Condition Logistics
* **Behavior:** Due to the risk of damage, renters must complete a mandatory photo checklist using their web browser.
* **State Machine Interaction:** * **Pre-Rental:** The renter must upload photos of the item's condition at pickup. Only upon successful upload does the system unlock the booking state from `PaymentHeld` to `Active`.
    * **Post-Rental:** The renter must upload drop-off photos. Only then does the system transition the state from `Active` to `Completed`.
* **Ratings Interaction:** Once a booking hits `Completed`, the UI prompts a mandatory Two-Way Rating system (Lender rates Renter, Renter rates Lender). Ratings dictate future borrowing privileges.

### 4. The Booking State Machine
* **Behavior:** A booking lifecycle is strictly governed by a sequential state machine. Minimal API endpoints must reject invalid transitions.
* **Transitions:**
    * `PendingApproval`: Default state when a renter requests an item. (If the item has `InstantBookEnabled == true`, this state is skipped entirely).
    * `Approved`: Lender accepts the request.
    * `PaymentHeld`: Automatically triggered immediately after `Approved`. Triggers Stripe to hold funds on the renter's card.
    * `Active`: Rental has officially begun. Triggered *only* when the pre-rental photo checklist is completed.
    * `Completed`: Item returned safely. Triggered *only* when the post-rental photo checklist is completed.
    * `Disputed`: Can be triggered by either party from `PaymentHeld`, `Active`, or `Completed` if there is an issue (late return, damage).

### 5. Payments & Escrow (Stripe Interaction)
* **Escrow Behavior:** Borro does not pay the lender immediately. Funds are held (`PaymentHeld`).
* **Payout Trigger:** The system must automatically release funds to the lender's connected Stripe account exactly 24 hours *after* the `StartDateUtc`.
* **Late Fees Behavior:** The system must include a .NET Background Service (`IHostedService`). This cron job periodically checks for bookings stuck in the `Active` state where `EndDateUtc` has passed. If found, it automatically applies a penalty fee via Stripe.

### 6. Real-Time Communication (SignalR)
* **Behavior:** Lenders and renters must be able to chat in real-time on the web app without exposing personal phone numbers.
* **Interaction:** The chat UI (SignalR connection) is tied directly to a `BookingId`.
* **Lifecycle Rules:** The chat interface should only become available *after* a booking reaches the `Approved` state, allowing them to discuss Handover Logistics (Local Pickup, Delivery, or Digital Handover).

### 7. Timezone Architecture
* **Behavior:** The application supports cross-timezone rentals.
* **Interaction:** The C# backend must *always* store `DateTime` variables as `DateTime.UtcNow`. The React frontend is solely responsible for converting these UTC times into the user's local timezone for UI display.
