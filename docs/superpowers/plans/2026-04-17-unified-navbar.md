# Unified Navbar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract a single shared `<Navbar />` component used on every page, removing the inline navbars from all pages, removing the search bar from the nav in `HomePage`, and adding an "Explore" link that navigates to `/search`.

**Architecture:** Create one `Navbar` component at `frontend/src/components/Navbar.tsx`. It reads auth state via `useAuth()` and renders: Borro brand (links to `/`), Home link (links to `/`), Explore link (links to `/search`), List an item, notification/chat icon buttons, and user avatar + logout. Each page replaces its existing inline `<nav>` / `<header>` nav section with `<Navbar />`. Pages that had a back button in their header (ItemDetailPage, BookingDetailPage) keep that button inside their own page content below the navbar.

**Tech Stack:** React, TypeScript, react-router-dom `<Link>`, Tailwind CSS, existing `useAuth()` hook.

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| **Create** | `frontend/src/components/Navbar.tsx` | Single shared navbar: Borro brand → `/`, Home → `/`, Explore → `/search`, right-side actions |
| **Modify** | `frontend/src/features/home/HomePage.tsx` | Remove inline `<nav>` block (lines 11–58), remove search-bar `<input>` inside that nav; add `<Navbar />` |
| **Modify** | `frontend/src/features/items/SearchPage.tsx` | Remove inline `<header>` block (lines 39–49); add `<Navbar />` |
| **Modify** | `frontend/src/features/items/ItemDetailPage.tsx` | Remove inline `<header>` nav (lines 44–57); add `<Navbar />`; move back-button into page body |
| **Modify** | `frontend/src/features/bookings/BookingDetailPage.tsx` | Remove inline `<header>` nav (lines 121–135); add `<Navbar />` |

---

## Task 1: Create the shared `Navbar` component

**Files:**
- Create: `frontend/src/components/Navbar.tsx`

- [ ] **Step 1: Create the file with full implementation**

```tsx
// frontend/src/components/Navbar.tsx
import { Link } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';

export function Navbar() {
  const { user, logout } = useAuth();

  return (
    <nav className="fixed top-0 w-full z-50 bg-white/80 backdrop-blur-xl shadow-sm">
      <div className="flex justify-between items-center px-6 py-4 max-w-screen-2xl mx-auto">
        <div className="flex items-center gap-12">
          <Link
            to="/"
            className="text-2xl font-black text-primary font-headline tracking-tight no-underline"
          >
            Borro
          </Link>
          <div className="hidden md:flex items-center gap-8">
            <Link
              to="/"
              className="text-on-surface-variant font-medium hover:text-primary transition-colors no-underline"
            >
              Home
            </Link>
            <Link
              to="/search"
              className="text-on-surface-variant font-medium hover:text-primary transition-colors no-underline"
            >
              Explore
            </Link>
          </div>
        </div>
        <div className="flex items-center gap-6">
          <div className="flex items-center gap-4 text-on-surface-variant">
            <Link
              to="/listings/new"
              className="hidden md:flex items-center gap-1.5 text-sm font-bold text-primary border border-primary/30 rounded-full px-4 py-1.5 hover:bg-primary hover:text-on-primary transition-all no-underline"
            >
              <span className="material-symbols-outlined text-base">add</span>
              List an item
            </Link>
            <button className="material-symbols-outlined hover:text-primary transition-colors active:scale-95 bg-transparent border-none p-0 cursor-pointer">
              notifications
            </button>
            <button className="material-symbols-outlined hover:text-primary transition-colors active:scale-95 bg-transparent border-none p-0 cursor-pointer">
              chat_bubble
            </button>
            <div className="flex items-center gap-3">
              <div className="h-8 w-8 rounded-full bg-primary flex items-center justify-center text-on-primary text-sm font-bold border border-outline-variant/30">
                {user?.firstName?.[0]?.toUpperCase() ?? '?'}
              </div>
              <button
                onClick={logout}
                className="hidden md:block text-xs font-bold text-on-surface-variant hover:text-primary transition-colors bg-transparent border-none p-0 cursor-pointer"
              >
                Log out
              </button>
            </div>
          </div>
        </div>
      </div>
    </nav>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/components/Navbar.tsx
git commit -m "feat: add shared Navbar component with Borro→/, Home→/, Explore→/search"
```

---

## Task 2: Update `HomePage` — use `<Navbar />`, remove nav search bar

**Files:**
- Modify: `frontend/src/features/home/HomePage.tsx`

- [ ] **Step 1: Add the `Navbar` import at the top of the file**

Replace the existing import block (which already has `useAuth`, `Link`, `useNavigate`):

```tsx
import { useAuth } from '../auth/AuthContext';
import { useNavigate } from 'react-router-dom';
import { Navbar } from '../../components/Navbar';
```

(Remove the `Link` import if it is no longer used elsewhere in `HomePage.tsx` after this change — check for any remaining `<Link>` usages first.)

- [ ] **Step 2: Remove the inline `<nav>` block and replace with `<Navbar />`**

Remove this entire block (the `{/* Top Nav */}` comment through the closing `</nav>` tag, lines 11–58):

```tsx
      {/* Top Nav */}
      <nav className="fixed top-0 w-full z-50 bg-white/80 backdrop-blur-xl shadow-sm">
        <div className="flex justify-between items-center px-6 py-4 max-w-screen-2xl mx-auto">
          <div className="flex items-center gap-12">
            <span className="text-2xl font-black text-primary font-headline tracking-tight">Borro</span>
            <div className="hidden md:flex items-center gap-8">
              <a href="#" className="text-primary font-semibold border-b-2 border-primary transition-colors">Home</a>
              <a href="#" className="text-on-surface-variant font-medium hover:text-primary transition-colors">How it Works</a>
            </div>
          </div>
          <div className="flex items-center gap-6">
            <div className="hidden lg:flex items-center bg-surface-container-low rounded-full px-4 py-2 border border-outline-variant/15">
              <span className="material-symbols-outlined text-on-surface-variant text-sm mr-2">search</span>
              <input
                className="bg-transparent border-none focus:ring-0 text-sm w-48 font-medium outline-none"
                placeholder="Search anything..."
                type="text"
              />
            </div>
            <div className="flex items-center gap-4 text-on-surface-variant">
              <Link
                to="/listings/new"
                className="hidden md:flex items-center gap-1.5 text-sm font-bold text-primary border border-primary/30 rounded-full px-4 py-1.5 hover:bg-primary hover:text-on-primary transition-all"
              >
                <span className="material-symbols-outlined text-base">add</span>
                List an item
              </Link>
              <button className="material-symbols-outlined hover:text-primary transition-colors active:scale-95 bg-transparent border-none p-0">
                notifications
              </button>
              <button className="material-symbols-outlined hover:text-primary transition-colors active:scale-95 bg-transparent border-none p-0">
                chat_bubble
              </button>
              <div className="flex items-center gap-3">
                <div className="h-8 w-8 rounded-full bg-primary flex items-center justify-center text-on-primary text-sm font-bold border border-outline-variant/30">
                  {user?.firstName?.[0]?.toUpperCase() ?? '?'}
                </div>
                <button
                  onClick={logout}
                  className="hidden md:block text-xs font-bold text-on-surface-variant hover:text-primary transition-colors bg-transparent border-none p-0"
                >
                  Log out
                </button>
              </div>
            </div>
          </div>
        </div>
      </nav>
```

And place `<Navbar />` immediately after the outer `<div>` opening tag:

```tsx
  return (
    <div className="bg-surface font-body text-on-surface">
      <Navbar />
      <main className="pt-20 pb-32">
```

- [ ] **Step 3: Remove unused `user` and `logout` from the destructure (if no longer needed in the page body)**

Check the rest of `HomePage.tsx` for any remaining uses of `user` or `logout`. If none remain after removing the nav, delete:

```tsx
  const { user, logout } = useAuth();
```

and remove the `useAuth` import. If they are used elsewhere in the page body, keep them.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/home/HomePage.tsx
git commit -m "refactor: replace HomePage inline nav with shared Navbar, remove nav search bar"
```

---

## Task 3: Update `SearchPage` — use `<Navbar />`

**Files:**
- Modify: `frontend/src/features/items/SearchPage.tsx`

- [ ] **Step 1: Add `Navbar` import**

Add at the top of the file (after existing imports):

```tsx
import { Navbar } from '../../components/Navbar';
```

- [ ] **Step 2: Replace the inline `<header>` nav block with `<Navbar />`**

Remove lines 39–49:

```tsx
      {/* Nav */}
      <header className="sticky top-0 z-20 bg-white/90 backdrop-blur-md shadow-[0_2px_12px_rgba(26,28,28,0.06)]">
        <div className="max-w-screen-2xl mx-auto px-8 h-16 flex items-center gap-6">
          <span className="font-[Plus_Jakarta_Sans] font-black text-2xl text-[#005f6c]">Borro</span>
          <nav className="flex gap-6 ml-auto text-sm font-semibold text-[#1a1c1c]">
            <a href="#" className="text-[#005f6c]">Explore</a>
            <Link to="/items/new">List an Item</Link>
            <a href="#">How it Works</a>
          </nav>
        </div>
      </header>
```

And replace with `<Navbar />` at the top of the returned JSX:

```tsx
  return (
    <div className="min-h-screen bg-[#f9f9f9] font-[Manrope]">
      <Navbar />
      <div className="max-w-screen-2xl mx-auto px-8 py-8 flex gap-8 pt-24">
```

> Note: Update `py-8` to `pt-24` on the container `<div>` so content clears the now-fixed navbar. If `pt-24` already exists or padding is handled differently, adjust accordingly to match other pages (which use `pt-20` or `pt-24`).

- [ ] **Step 3: Remove `Link` import if no longer used**

Check if `Link` is still used in `SearchPage.tsx` after removing the header. Remove it from the import if not.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/items/SearchPage.tsx
git commit -m "refactor: replace SearchPage inline nav with shared Navbar"
```

---

## Task 4: Update `ItemDetailPage` — use `<Navbar />`, move back button into page body

**Files:**
- Modify: `frontend/src/features/items/ItemDetailPage.tsx`

- [ ] **Step 1: Add `Navbar` import**

```tsx
import { Navbar } from '../../components/Navbar';
```

- [ ] **Step 2: Replace the inline `<header>` nav with `<Navbar />`**

Remove lines 44–57:

```tsx
      <header className="sticky top-0 z-20 bg-white/90 backdrop-blur-md shadow-[0_2px_12px_rgba(26,28,28,0.06)]">
        <div className="max-w-screen-xl mx-auto px-8 h-16 flex items-center gap-4">
          <button
            onClick={() => navigate(-1)}
            className="flex items-center gap-1 text-[#3e494b] bg-transparent border-none cursor-pointer hover:text-[#005f6c] transition-colors text-sm font-semibold"
          >
            <span className="material-symbols-outlined text-base">arrow_back</span> Back
          </button>
          <span className="font-[Plus_Jakarta_Sans] font-black text-xl text-[#005f6c] ml-2">Borro</span>
          <div className="flex gap-4 ml-auto text-sm font-semibold">
            <Link to="/items/new" className="text-[#1a1c1c] no-underline">List an Item</Link>
          </div>
        </div>
      </header>
```

Replace with `<Navbar />` and add the back button at the top of the page content:

```tsx
      <Navbar />
      <div className="max-w-screen-xl mx-auto px-8 py-8 pt-24">
        {/* Back button */}
        <button
          onClick={() => navigate(-1)}
          className="flex items-center gap-1 text-[#3e494b] bg-transparent border-none cursor-pointer hover:text-[#005f6c] transition-colors text-sm font-semibold mb-4"
        >
          <span className="material-symbols-outlined text-base">arrow_back</span> Back
        </button>
        {/* rest of page content follows */}
```

- [ ] **Step 3: Remove `Link` import if no longer used**

Check if `Link` remains in use. Remove from imports if not.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/items/ItemDetailPage.tsx
git commit -m "refactor: replace ItemDetailPage inline nav with shared Navbar, move back button to body"
```

---

## Task 5: Update `BookingDetailPage` — use `<Navbar />`

**Files:**
- Modify: `frontend/src/features/bookings/BookingDetailPage.tsx`

- [ ] **Step 1: Add `Navbar` import**

```tsx
import { Navbar } from '../../components/Navbar';
```

- [ ] **Step 2: Replace the inline `<header>` nav block with `<Navbar />`**

Remove lines 121–135:

```tsx
      <header className="bg-[#f9f9f9]/90 backdrop-blur-xl fixed top-0 z-50 w-full">
        <div className="flex justify-between items-center px-6 py-4 w-full max-w-7xl mx-auto">
          <div className="flex items-center gap-8">
            <span className="font-[Plus_Jakarta_Sans] font-extrabold text-2xl text-[#007A8A] tracking-tight">Borro</span>
            <nav className="hidden md:flex gap-6 items-center">
              <a className="text-[#3e494b] hover:bg-[#f3f3f3] px-3 py-2 rounded-lg transition-colors font-medium cursor-pointer" onClick={() => navigate('/search')}>Explore</a>
              <a className="text-[#007A8A] font-bold px-3 py-2 rounded-lg transition-colors cursor-pointer">Bookings</a>
            </nav>
          </div>
          <div className="flex items-center gap-2">
            <button className="material-symbols-outlined text-[#3e494b] p-2 hover:bg-[#f3f3f3] rounded-full transition-colors border-none bg-transparent cursor-pointer">notifications</button>
          </div>
        </div>
        <div className="h-px bg-[#e2e2e2]/50 w-full" />
      </header>
```

And replace with `<Navbar />` at the top of the returned JSX (before the `<main>` tag):

```tsx
      <Navbar />
      <main className="pt-24 pb-12 px-6 max-w-7xl mx-auto min-h-screen">
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/bookings/BookingDetailPage.tsx
git commit -m "refactor: replace BookingDetailPage inline nav with shared Navbar"
```

---

## Task 6: Smoke-test all pages

- [ ] **Step 1: Start the dev server**

```bash
cd frontend && npm run dev
```

- [ ] **Step 2: Verify each page shows the same navbar**

Visit the following routes and confirm each shows the unified navbar (Borro brand, Home, Explore — no search input in the nav):
- `http://localhost:5173/` (Home)
- `http://localhost:5173/search` (Search/Explore)
- `http://localhost:5173/items/<any-id>` (Item Detail)
- `http://localhost:5173/bookings/<any-id>` (Booking Detail)

- [ ] **Step 3: Verify navigation links**

| Click target | Expected URL |
|---|---|
| "Borro" text | `/` |
| "Home" link | `/` |
| "Explore" link | `/search` |

- [ ] **Step 4: Confirm no search bar in the navbar on any page**

The search bar that was in the top-right of the `HomePage` nav should be gone. The hero search bar lower on the page is unaffected.
