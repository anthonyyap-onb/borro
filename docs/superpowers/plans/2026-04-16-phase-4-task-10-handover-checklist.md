# Phase 4 Task 10: Frontend — HandoverChecklist Component

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the `HandoverChecklist` modal component that gates pickup/dropoff transitions behind mandatory photo upload.

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Task 9 (`conditionReportApi`).

**Tech Stack:** React 18, TypeScript, Tailwind CSS

---

## File Map

| Action | File |
|--------|------|
| Create | `frontend/src/features/bookings/HandoverChecklist.tsx` |

---

- [ ] **Step 1: Create HandoverChecklist**

```tsx
// frontend/src/features/bookings/HandoverChecklist.tsx
import { useState } from 'react';
import { conditionReportApi } from './conditionReportApi';

interface Props {
  bookingId: string;
  type: 'Pickup' | 'Dropoff';
  onComplete: () => void;
}

export function HandoverChecklist({ bookingId, type, onComplete }: Props) {
  const [photos, setPhotos] = useState<File[]>([]);
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (photos.length === 0) {
      setError('Please upload at least one photo.');
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await conditionReportApi.create(bookingId, type, photos, notes);
      onComplete();
    } catch {
      setError('Failed to submit checklist. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const label = type === 'Pickup' ? 'Pickup Checklist' : 'Drop-off Checklist';
  const description = type === 'Pickup'
    ? 'Upload photos of the item\'s condition before you take it. This protects you from false damage claims.'
    : 'Upload photos proving the item was returned in the agreed condition.';

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-surface rounded-2xl p-6 max-w-md w-full shadow-2xl">
        <h2 className="font-headline text-xl font-bold mb-2">{label}</h2>
        <p className="text-on-surface-variant text-sm mb-6">{description}</p>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-2 rounded-lg mb-4 text-sm">{error}</div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-bold mb-1">Photos (required)</label>
            <input
              type="file"
              accept="image/*"
              multiple
              required
              className="w-full"
              onChange={e => setPhotos(Array.from(e.target.files ?? []))}
            />
            {photos.length > 0 && (
              <p className="text-sm text-on-surface-variant mt-1">{photos.length} photo(s) selected</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-bold mb-1">Notes (optional)</label>
            <textarea
              rows={3}
              className="w-full border border-outline-variant rounded-lg px-4 py-2 text-sm"
              placeholder="Any notes about the item's condition..."
              value={notes}
              onChange={e => setNotes(e.target.value)}
            />
          </div>

          <button
            type="submit"
            disabled={submitting}
            className="w-full bg-primary text-on-primary rounded-full py-3 font-bold hover:opacity-90 transition-opacity disabled:opacity-50 border-none"
          >
            {submitting ? 'Submitting...' : `Submit ${label}`}
          </button>
        </form>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/features/bookings/HandoverChecklist.tsx
git commit -m "feat: add HandoverChecklist component for mandatory photo upload"
```
