# Phase 4 Task 9: Frontend — conditionReportApi.ts + reviewApi.ts

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create Axios wrappers for condition report and review API calls.

**Context:** Part of Phase 4 — Trust, Security & Payments. Independent of backend tasks (can be done in parallel with Tasks 6–8).

**Tech Stack:** React 18, TypeScript, Axios (`apiClient` from `../../lib/apiClient`)

---

## File Map

| Action | File |
|--------|------|
| Create | `frontend/src/features/bookings/conditionReportApi.ts` |
| Create | `frontend/src/features/bookings/reviewApi.ts` |

---

- [ ] **Step 1: Create conditionReportApi.ts**

```typescript
// frontend/src/features/bookings/conditionReportApi.ts
import apiClient from '../../lib/apiClient';

export const conditionReportApi = {
  create: (bookingId: string, type: 'Pickup' | 'Dropoff', photos: File[], notes: string) => {
    const form = new FormData();
    form.append('bookingId', bookingId);
    form.append('type', type);
    form.append('notes', notes);
    photos.forEach(f => form.append('files', f));
    return apiClient.post<{ id: string }>('/api/condition-reports', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};
```

- [ ] **Step 2: Create reviewApi.ts**

```typescript
// frontend/src/features/bookings/reviewApi.ts
import apiClient from '../../lib/apiClient';

export interface ReviewDto {
  id: string;
  bookingId: string;
  reviewerId: string;
  reviewerName: string;
  targetUserId: string;
  targetUserName: string;
  rating: number;
  comment: string;
  createdAtUtc: string;
}

export const reviewApi = {
  getForUser: (userId: string) =>
    apiClient.get<ReviewDto[]>('/api/reviews', { params: { userId } }),

  create: (bookingId: string, targetUserId: string, rating: number, comment: string) =>
    apiClient.post<ReviewDto>('/api/reviews', { bookingId, targetUserId, rating, comment }),
};
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/bookings/conditionReportApi.ts frontend/src/features/bookings/reviewApi.ts
git commit -m "feat: add conditionReportApi and reviewApi Axios wrappers"
```
