// frontend/src/features/items/SearchPage.tsx
import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { itemApi, type ItemDto } from './itemApi';
import { Navbar } from '../../components/Navbar';

const CATEGORIES = ['All Categories', 'Vehicles', 'Tools & Equipment', 'Electronics', 'Outdoors', 'Event Gear', 'Other'];

export function SearchPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [items, setItems] = useState<ItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [pendingMaxPrice, setPendingMaxPrice] = useState(450);

  const category = searchParams.get('category') ?? '';
  const location = searchParams.get('location') ?? '';
  const maxPrice = searchParams.get('maxPrice') ? Number(searchParams.get('maxPrice')) : undefined;

  useEffect(() => {
    setLoading(true);
    itemApi
      .search({ category: category || undefined, location: location || undefined, maxPrice })
      .then(res => setItems(res.data))
      .catch(() => setItems([]))
      .finally(() => setLoading(false));
  }, [category, location, maxPrice]);

  const setFilter = (key: string, value: string) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value); else next.delete(key);
    setSearchParams(next);
  };

  const applyFilters = () => {
    setFilter('maxPrice', pendingMaxPrice < 450 ? String(pendingMaxPrice) : '');
  };

  return (
    <div className="min-h-screen bg-[#f9f9f9] font-[Manrope]">
      <Navbar />
      <div className="max-w-screen-2xl mx-auto px-8 pt-24 pb-8 flex gap-8">

        {/* Left sidebar filters */}
        <aside className="w-72 shrink-0">
          <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
            <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-5">Filters</h2>

            <div className="mb-5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Category</label>
              <select
                className="w-full bg-[#f3f3f3] rounded-xl px-4 py-2.5 text-sm font-semibold text-[#1a1c1c]"
                value={category || 'All Categories'}
                onChange={e => setFilter('category', e.target.value === 'All Categories' ? '' : e.target.value)}
              >
                {CATEGORIES.map(c => <option key={c}>{c}</option>)}
              </select>
            </div>

            <div className="mb-5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Price Range (per day)</label>
              <div className="flex items-center justify-between text-sm text-[#3e494b] mb-2">
                <span>$0</span>
                <span>{pendingMaxPrice >= 450 ? '$450+' : `$${pendingMaxPrice}`}</span>
              </div>
              <input
                type="range" min="0" max="450" step="10"
                value={pendingMaxPrice}
                onChange={e => setPendingMaxPrice(Number(e.target.value))}
                className="w-full accent-[#005f6c]"
              />
            </div>

            <div className="mb-5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Location</label>
              <input
                className="w-full bg-[#f3f3f3] rounded-xl px-4 py-2.5 text-sm text-[#1a1c1c]"
                placeholder="City, State..."
                defaultValue={location}
                onBlur={e => setFilter('location', e.target.value)}
              />
            </div>

            <button
              onClick={applyFilters}
              className="w-full bg-[#005f6c] text-white rounded-full py-3 font-bold text-sm hover:bg-[#007a8a] transition-colors border-none cursor-pointer"
            >
              Apply Filters
            </button>
          </div>
        </aside>

        {/* Main results area */}
        <main className="flex-1 min-w-0">
          <div className="mb-6">
            <h1 className="font-[Plus_Jakarta_Sans] font-bold text-2xl text-[#1a1c1c]">
              {location ? `Items near ${location}` : 'All Items'}
            </h1>
            {!loading && (
              <p className="text-sm text-[#3e494b] mt-1">
                Showing {items.length} result{items.length !== 1 ? 's' : ''}
              </p>
            )}
          </div>

          {loading ? (
            <p className="text-[#3e494b]">Loading...</p>
          ) : items.length === 0 ? (
            <p className="text-[#3e494b]">No items found. Try adjusting the filters.</p>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
              {items.map(item => (
                <Link key={item.id} to={`/items/${item.id}`} className="group block">
                  <div className="bg-white rounded-2xl overflow-hidden shadow-[0_4px_24px_rgba(26,28,28,0.06)] hover:shadow-[0_8px_32px_rgba(26,28,28,0.10)] transition-shadow">
                    <div className="relative aspect-[4/3] overflow-hidden bg-[#e8e8e8]">
                      {item.imageUrls[0] ? (
                        <img
                          src={item.imageUrls[0]}
                          alt={item.title}
                          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                        />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-[#3e494b]">
                          <span className="material-symbols-outlined text-5xl">image</span>
                        </div>
                      )}
                      {item.instantBookEnabled && (
                        <span className="absolute top-3 left-3 bg-[#a91929]/90 text-white text-xs font-bold px-3 py-1 rounded-full backdrop-blur-sm">
                          Instant Book
                        </span>
                      )}
                      <button className="absolute top-3 right-3 w-8 h-8 bg-white/80 backdrop-blur-sm rounded-full flex items-center justify-center text-[#3e494b] hover:text-[#a91929] transition-colors border-none cursor-pointer">
                        <span className="material-symbols-outlined text-base">favorite</span>
                      </button>
                    </div>
                    <div className="p-4">
                      <h3 className="font-[Plus_Jakarta_Sans] font-bold text-[#1a1c1c] mb-1 truncate">{item.title}</h3>
                      <p className="text-xs text-[#3e494b] mb-2">{item.location}</p>
                      <div className="flex items-baseline gap-1">
                        <span className="font-black text-xl text-[#005f6c]">${item.dailyPrice}</span>
                        <span className="text-xs text-[#3e494b]">/day</span>
                      </div>
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </main>
      </div>

      {/* Floating "Show Map" pill */}
      <div className="fixed bottom-8 left-1/2 -translate-x-1/2 z-10">
        <button className="flex items-center gap-2 bg-[#1a1c1c]/80 backdrop-blur-md text-white px-5 py-3 rounded-full font-bold text-sm shadow-lg hover:bg-[#1a1c1c] transition-colors border-none cursor-pointer">
          <span className="material-symbols-outlined text-base">map</span>
          Show Map
        </button>
      </div>
    </div>
  );
}
