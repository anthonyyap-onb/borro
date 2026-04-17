# Phase 4 Task 11: Frontend — ReviewPrompt Component

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the `ReviewPrompt` modal component with a 5-star rating UI that appears after booking completion.

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Task 9 (`reviewApi`).

**Tech Stack:** React 18, TypeScript, Tailwind CSS

---

## File Map

| Action | File |
|--------|------|
| Create | `frontend/src/features/bookings/ReviewPrompt.tsx` |

---

- [ ] **Step 1: Create ReviewPrompt**

```tsx
// frontend/src/features/bookings/ReviewPrompt.tsx
import { useState } from 'react';
import { reviewApi } from './reviewApi';

interface Props {
  bookingId: string;
  targetUserId: string;
  targetName: string;
  onComplete: () => void;
  onSkip: () => void;
}

export function ReviewPrompt({ bookingId, targetUserId, targetName, onComplete, onSkip }: Props) {
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (rating === 0) { setError('Please select a rating.'); return; }
    setSubmitting(true);
    setError(null);
    try {
      await reviewApi.create(bookingId, targetUserId, rating, comment);
      onComplete();
    } catch {
      setError('Failed to submit review. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-surface rounded-2xl p-6 max-w-md w-full shadow-2xl">
        <h2 className="font-headline text-xl font-bold mb-2">Rate your experience</h2>
        <p className="text-on-surface-variant text-sm mb-6">How was your experience with {targetName}?</p>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-2 rounded-lg mb-4 text-sm">{error}</div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="flex gap-2">
            {[1, 2, 3, 4, 5].map(star => (
              <button
                key={star}
                type="button"
                onClick={() => setRating(star)}
                className={`text-3xl bg-transparent border-none p-0 cursor-pointer transition-transform active:scale-110 ${star <= rating ? 'text-yellow-400' : 'text-gray-300'}`}
              >
                ★
              </button>
            ))}
          </div>

          <div>
            <label className="block text-sm font-bold mb-1">Comment (optional)</label>
            <textarea
              rows={3}
              className="w-full border border-outline-variant rounded-lg px-4 py-2 text-sm"
              placeholder="Share your experience..."
              value={comment}
              onChange={e => setComment(e.target.value)}
            />
          </div>

          <div className="flex gap-3">
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 bg-primary text-on-primary rounded-full py-3 font-bold hover:opacity-90 transition-opacity disabled:opacity-50 border-none"
            >
              {submitting ? 'Submitting...' : 'Submit Review'}
            </button>
            <button
              type="button"
              onClick={onSkip}
              className="px-6 py-3 bg-surface-container border border-outline-variant rounded-full font-bold text-sm hover:bg-surface-container-high transition-colors border-none"
            >
              Skip
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/features/bookings/ReviewPrompt.tsx
git commit -m "feat: add ReviewPrompt star-rating component"
```
