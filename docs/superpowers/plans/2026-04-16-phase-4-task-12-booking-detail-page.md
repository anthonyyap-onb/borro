# Phase 4 Task 12: Update BookingDetailPage — Checklist Gating + Review Prompt

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace bare transition buttons in `BookingDetailPage` with `HandoverChecklist`-gated buttons, and show `ReviewPrompt` after completion.

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Tasks 9, 10, 11.

**Tech Stack:** React 18, TypeScript, Tailwind CSS

---

## File Map

| Action | File |
|--------|------|
| Modify | `frontend/src/features/bookings/BookingDetailPage.tsx` |

---

- [ ] **Step 1: Add imports to BookingDetailPage.tsx**

```tsx
import { HandoverChecklist } from './HandoverChecklist';
import { ReviewPrompt } from './ReviewPrompt';
import { bookingApi } from './bookingApi';
```

- [ ] **Step 2: Add modal state inside the component**

After existing state declarations:
```tsx
const [showPickupChecklist, setShowPickupChecklist] = useState(false);
const [showDropoffChecklist, setShowDropoffChecklist] = useState(false);
const [showReviewPrompt, setShowReviewPrompt] = useState(false);
```

- [ ] **Step 3: Replace Confirm Pickup button**

```tsx
{isRenter && booking.status === 'PaymentHeld' && (
  <button
    onClick={() => setShowPickupChecklist(true)}
    className="bg-primary text-on-primary rounded-full px-6 py-3 font-bold border-none hover:opacity-90 active:scale-95"
  >
    Confirm Pickup
  </button>
)}
```

- [ ] **Step 4: Replace Confirm Return button**

```tsx
{isRenter && booking.status === 'Active' && (
  <button
    onClick={() => setShowDropoffChecklist(true)}
    className="bg-primary text-on-primary rounded-full px-6 py-3 font-bold border-none hover:opacity-90 active:scale-95"
  >
    Confirm Return
  </button>
)}
```

- [ ] **Step 5: Add modals at end of return JSX (before closing `</div>`)**

```tsx
{showPickupChecklist && (
  <HandoverChecklist
    bookingId={booking.id}
    type="Pickup"
    onComplete={async () => {
      setShowPickupChecklist(false);
      await handleTransition('Active');
    }}
  />
)}

{showDropoffChecklist && (
  <HandoverChecklist
    bookingId={booking.id}
    type="Dropoff"
    onComplete={async () => {
      setShowDropoffChecklist(false);
      await handleTransition('Completed');
      setShowReviewPrompt(true);
    }}
  />
)}

{showReviewPrompt && booking && (
  <ReviewPrompt
    bookingId={booking.id}
    targetUserId={isRenter ? booking.lenderId : booking.renterId}
    targetName={isRenter ? booking.lenderName : booking.renterName}
    onComplete={() => setShowReviewPrompt(false)}
    onSkip={() => setShowReviewPrompt(false)}
  />
)}
```

- [ ] **Step 6: Build frontend**

```bash
cd frontend
npm run build 2>&1 | tail -20
```

Expected: `built in X.XXs` with no TypeScript errors.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/features/bookings/BookingDetailPage.tsx
git commit -m "feat: gate PaymentHeld→Active and Active→Completed transitions behind photo checklists; show review prompt on completion"
```
