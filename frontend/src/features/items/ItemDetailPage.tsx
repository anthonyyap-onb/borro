// frontend/src/features/items/ItemDetailPage.tsx
import { useEffect, useState } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { itemApi, type ItemDto } from './itemApi';

export function ItemDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [item, setItem] = useState<ItemDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedImage, setSelectedImage] = useState(0);

  useEffect(() => {
    if (!id) return;
    itemApi.getById(id)
      .then(res => setItem(res.data))
      .catch(() => navigate('/search'))
      .finally(() => setLoading(false));
  }, [id, navigate]);

  if (loading) return <div className="min-h-screen flex items-center justify-center font-[Manrope]">Loading...</div>;
  if (!item) return null;

  const attrs = Object.entries(item.attributes);

  return (
    <div className="min-h-screen bg-[#f9f9f9] font-[Manrope]">
      {/* Nav */}
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

      <div className="max-w-screen-xl mx-auto px-8 py-8">
        {/* Full-width photo gallery */}
        <div className="relative mb-8">
          <div className="aspect-[16/7] rounded-2xl overflow-hidden bg-[#e8e8e8]">
            {item.imageUrls[selectedImage] ? (
              <img src={item.imageUrls[selectedImage]} alt={item.title} className="w-full h-full object-cover" />
            ) : (
              <div className="w-full h-full flex items-center justify-center text-[#3e494b]">
                <span className="material-symbols-outlined text-7xl">image</span>
              </div>
            )}
          </div>
          {item.imageUrls.length > 1 && (
            <>
              <div className="flex gap-3 mt-3 overflow-x-auto pb-1">
                {item.imageUrls.map((url, i) => (
                  <button
                    key={i}
                    onClick={() => setSelectedImage(i)}
                    className={`w-20 h-16 rounded-xl overflow-hidden shrink-0 border-2 transition-all p-0 ${i === selectedImage ? 'border-[#005f6c]' : 'border-transparent opacity-60'}`}
                  >
                    <img src={url} alt="" className="w-full h-full object-cover" />
                  </button>
                ))}
              </div>
              <button className="absolute bottom-4 right-4 flex items-center gap-2 bg-white/80 backdrop-blur-sm text-[#1a1c1c] px-4 py-2 rounded-full font-semibold text-sm shadow border-none cursor-pointer">
                <span className="material-symbols-outlined text-base">grid_view</span> Show all photos
              </button>
            </>
          )}
        </div>

        {/* Two-column: details left, booking sidebar right */}
        <div className="grid lg:grid-cols-[1fr_380px] gap-10">

          {/* Left: item details */}
          <div>
            {/* Category + rating */}
            <div className="flex items-center gap-3 mb-3">
              <span className="bg-[#daf8ff] text-[#005f6c] text-xs font-bold px-3 py-1 rounded-full uppercase tracking-wider">
                {item.category}
              </span>
              <span className="flex items-center gap-1 text-sm text-[#3e494b]">
                <span className="material-symbols-outlined text-base text-amber-400">star</span>
                <span className="font-semibold">4.9</span>
                <span>(128 reviews)</span>
              </span>
            </div>

            <h1 className="font-[Plus_Jakarta_Sans] font-bold text-3xl text-[#1a1c1c] mb-2">{item.title}</h1>
            <p className="flex items-center gap-2 text-sm text-[#3e494b] mb-6">
              <span className="material-symbols-outlined text-base">category</span>{item.category}
              <span className="mx-1">·</span>
              <span className="material-symbols-outlined text-base">location_on</span>{item.location}
            </p>

            {/* Host card */}
            <div className="bg-white rounded-2xl p-5 flex items-center gap-4 shadow-[0_4px_24px_rgba(26,28,28,0.06)] mb-6">
              <div className="w-12 h-12 rounded-full bg-[#e8e8e8] flex items-center justify-center shrink-0">
                <span className="material-symbols-outlined text-2xl text-[#3e494b]">person</span>
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-xs text-[#3e494b] mb-0.5">Hosted by</p>
                <p className="font-[Plus_Jakarta_Sans] font-bold text-[#1a1c1c]">{item.ownerName}</p>
                <p className="text-xs text-[#3e494b] flex items-center gap-1">
                  <span className="material-symbols-outlined text-xs text-[#005f6c]">verified</span>
                  Identity Verified
                </p>
              </div>
              <button className="bg-[#d5e0f7] text-[#545f72] px-4 py-2 rounded-full font-semibold text-sm hover:bg-[#bcc7dd] transition-colors border-none cursor-pointer shrink-0">
                Contact Host
              </button>
            </div>

            {/* Description */}
            <div className="mb-6">
              <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-3">About this item</h2>
              <p className="text-[#3e494b] leading-relaxed">{item.description}</p>
            </div>

            {/* Technical specs */}
            {attrs.length > 0 && (
              <div className="bg-white rounded-2xl p-5 shadow-[0_4px_24px_rgba(26,28,28,0.06)] mb-6">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">Technical Specifications</h2>
                <dl className="grid grid-cols-2 gap-y-3 gap-x-6">
                  {attrs.map(([k, v]) => (
                    <div key={k} className="flex flex-col">
                      <dt className="text-xs text-[#3e494b] font-semibold uppercase tracking-wider">{k}</dt>
                      <dd className="font-semibold text-[#1a1c1c]">{String(v)}</dd>
                    </div>
                  ))}
                </dl>
              </div>
            )}

            {/* Handover options */}
            {item.handoverOptions.length > 0 && (
              <div className="mb-6">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-3">Handover Options</h2>
                <div className="flex flex-wrap gap-2">
                  {item.handoverOptions.map(opt => (
                    <span key={opt} className="bg-white rounded-full px-4 py-1.5 text-sm font-semibold text-[#1a1c1c] shadow-[0_2px_8px_rgba(26,28,28,0.06)]">
                      {opt.replace(/([A-Z])/g, ' $1').trim()}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Reviews stub */}
            <div>
              <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-3">Community Reviews</h2>
              <div className="flex items-center gap-2 mb-3">
                <span className="material-symbols-outlined text-amber-400">star</span>
                <span className="font-bold text-[#1a1c1c]">4.9</span>
              </div>
              <p className="text-sm text-[#3e494b]">Reviews will appear here once renters have completed their bookings.</p>
            </div>
          </div>

          {/* Right: sticky booking sidebar */}
          <div className="lg:sticky lg:top-24 self-start">
            <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
              <div className="flex items-baseline gap-1 mb-2">
                <span className="font-[Plus_Jakarta_Sans] font-black text-4xl text-[#005f6c]">${item.dailyPrice}</span>
                <span className="text-[#3e494b] text-sm">/ day</span>
              </div>
              {item.instantBookEnabled && (
                <span className="inline-flex items-center gap-1 text-xs font-bold text-[#a91929] bg-[#ffdad8] px-3 py-1 rounded-full mb-4">
                  <span className="material-symbols-outlined text-xs">bolt</span> Instant Book
                </span>
              )}

              {/* Date inputs */}
              <div className="bg-[#f3f3f3] rounded-xl p-4 mb-4 space-y-3">
                <div>
                  <p className="text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-1">Pick up</p>
                  <input type="date" className="w-full bg-white rounded-lg px-3 py-2 text-sm text-[#1a1c1c]" />
                </div>
                <div>
                  <p className="text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-1">Return</p>
                  <input type="date" className="w-full bg-white rounded-lg px-3 py-2 text-sm text-[#1a1c1c]" />
                </div>
              </div>

              {/* Cost breakdown */}
              <div className="space-y-2 text-sm mb-4">
                <div className="flex justify-between text-[#3e494b]">
                  <span>${item.dailyPrice} × 1 day</span><span>${item.dailyPrice}</span>
                </div>
                <div className="flex justify-between text-[#3e494b]">
                  <span>Insurance fee</span><span>$15</span>
                </div>
                <div className="flex justify-between text-[#3e494b]">
                  <span>Service fee</span><span>$12</span>
                </div>
                <div className="flex justify-between font-bold text-[#1a1c1c] pt-2 border-t border-[#bdc8cb]/20">
                  <span>Total</span><span>${item.dailyPrice + 27}</span>
                </div>
              </div>

              {/* Book CTA — Phase 3 wires to booking flow */}
              <button className="w-full bg-gradient-to-r from-[#005f6c] to-[#007a8a] text-white rounded-full py-4 font-bold text-base hover:opacity-90 transition-opacity border-none cursor-pointer mb-2">
                {item.instantBookEnabled ? 'Book Now' : 'Request to Book'}
              </button>
              <p className="text-center text-xs text-[#3e494b]">You won't be charged yet</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
