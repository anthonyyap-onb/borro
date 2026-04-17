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
