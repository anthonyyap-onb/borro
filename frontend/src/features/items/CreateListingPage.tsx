// frontend/src/features/items/CreateListingPage.tsx
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { itemApi, type CreateItemPayload, type ItemAttributes } from './itemApi';

const CATEGORIES = ['Vehicles', 'Tools & Equipment', 'Electronics', 'Outdoors', 'Event Gear', 'Other'];

const CATEGORY_FIELDS: Record<string, { label: string; key: string; type: 'text' | 'number' }[]> = {
  Vehicles: [
    { label: 'Make & Model', key: 'Model', type: 'text' },
    { label: 'Year', key: 'Year', type: 'number' },
    { label: 'Mileage', key: 'Mileage', type: 'number' },
    { label: 'Transmission', key: 'Transmission', type: 'text' },
  ],
  'Tools & Equipment': [
    { label: 'Brand', key: 'Brand', type: 'text' },
    { label: 'Voltage / Power', key: 'Power', type: 'text' },
  ],
  Electronics: [
    { label: 'Brand', key: 'Brand', type: 'text' },
    { label: 'Model', key: 'Model', type: 'text' },
    { label: 'Megapixels / Spec', key: 'Spec', type: 'text' },
  ],
  Outdoors: [
    { label: 'Type', key: 'Type', type: 'text' },
    { label: 'Capacity / Size', key: 'Size', type: 'text' },
  ],
  'Event Gear': [
    { label: 'Type', key: 'Type', type: 'text' },
    { label: 'Capacity', key: 'Capacity', type: 'number' },
  ],
  Other: [],
};

const HANDOVER_OPTIONS = ['RenterPicksUp', 'OwnerDelivers', 'ThirdPartyDropOff'];
const MAX_PHOTOS = 8;

export function CreateListingPage() {
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [imageFiles, setImageFiles] = useState<File[]>([]);
  const [imagePreviewUrls, setImagePreviewUrls] = useState<string[]>([]);

  const [form, setForm] = useState<CreateItemPayload>({
    title: '',
    description: '',
    dailyPrice: 0,
    location: '',
    category: 'Tools & Equipment',
    attributes: {},
    instantBookEnabled: false,
    handoverOptions: [],
  });

  const set = (key: keyof CreateItemPayload, value: unknown) =>
    setForm(f => ({ ...f, [key]: value }));

  const setAttr = (key: string, value: string) =>
    setForm(f => ({ ...f, attributes: { ...f.attributes, [key]: value } }));

  const toggleHandover = (opt: string) =>
    set(
      'handoverOptions',
      form.handoverOptions.includes(opt)
        ? form.handoverOptions.filter(o => o !== opt)
        : [...form.handoverOptions, opt]
    );

  const handlePhotoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []).slice(0, MAX_PHOTOS);
    setImageFiles(files);
    setImagePreviewUrls(files.map(f => URL.createObjectURL(f)));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const { data: item } = await itemApi.create(form);
      for (const file of imageFiles) {
        await itemApi.uploadImage(item.id, file);
      }
      navigate(`/items/${item.id}`);
    } catch {
      setError('Failed to create listing. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const dynamicFields = CATEGORY_FIELDS[form.category] ?? [];

  return (
    <div className="min-h-screen bg-[#f9f9f9] font-[Manrope]">
      {/* Nav */}
      <header className="sticky top-0 z-20 bg-white/90 backdrop-blur-md shadow-[0_2px_12px_rgba(26,28,28,0.06)]">
        <div className="max-w-screen-xl mx-auto px-8 h-16 flex items-center gap-4">
          <button
            onClick={() => navigate(-1)}
            className="flex items-center gap-1 text-[#3e494b] bg-transparent border-none cursor-pointer hover:text-[#005f6c] text-sm font-semibold"
          >
            <span className="material-symbols-outlined text-base">arrow_back</span>
          </button>
          <span className="font-[Plus_Jakarta_Sans] font-black text-xl text-[#005f6c]">Borro</span>
          <h1 className="font-[Plus_Jakarta_Sans] font-bold text-xl text-[#1a1c1c] ml-4">Create a Listing</h1>
        </div>
      </header>

      <div className="max-w-screen-lg mx-auto px-8 py-10">
        {error && (
          <div className="bg-[#ffdad6] text-[#93000a] px-5 py-3 rounded-xl mb-6 text-sm font-semibold">{error}</div>
        )}

        <form onSubmit={handleSubmit}>
          {/* Two-column layout: form left, photo panel right */}
          <div className="grid lg:grid-cols-[1fr_340px] gap-8">

            {/* Left: form cards */}
            <div className="space-y-6">

              {/* Item Details card */}
              <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">Item Details</h2>
                <div className="space-y-4">

                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Category</label>
                    <select
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm font-semibold text-[#1a1c1c]"
                      value={form.category}
                      onChange={e => set('category', e.target.value)}
                    >
                      {CATEGORIES.map(c => <option key={c}>{c}</option>)}
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Title</label>
                    <input
                      required
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                      placeholder="e.g. DeWalt 20V Hammer Drill"
                      value={form.title}
                      onChange={e => set('title', e.target.value)}
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Description</label>
                    <textarea
                      required
                      rows={4}
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c] resize-none"
                      placeholder="Describe your item, condition, and any usage rules"
                      value={form.description}
                      onChange={e => set('description', e.target.value)}
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Location</label>
                    <input
                      required
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                      placeholder="City, State"
                      value={form.location}
                      onChange={e => set('location', e.target.value)}
                    />
                  </div>
                </div>
              </div>

              {/* Dynamic category specs card */}
              {dynamicFields.length > 0 && (
                <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
                  <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">{form.category} Specs</h2>
                  <div className="grid grid-cols-2 gap-4">
                    {dynamicFields.map(field => (
                      <div key={field.key}>
                        <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">{field.label}</label>
                        <input
                          type={field.type}
                          className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                          value={(form.attributes as ItemAttributes)[field.key] as string ?? ''}
                          onChange={e => setAttr(field.key, e.target.value)}
                        />
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Pricing card */}
              <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">Pricing</h2>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Daily Rate ($)</label>
                    <input
                      required
                      type="number"
                      min="1"
                      step="0.01"
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                      value={form.dailyPrice || ''}
                      onChange={e => set('dailyPrice', parseFloat(e.target.value))}
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-2">Minimum Rental (days)</label>
                    <input
                      type="number"
                      min="1"
                      defaultValue={1}
                      className="w-full bg-[#f3f3f3] rounded-xl px-4 py-3 text-sm text-[#1a1c1c]"
                    />
                  </div>
                </div>
              </div>

              {/* Handover & Options card */}
              <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)]">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-4">Handover & Options</h2>

                <div className="mb-4">
                  <label className="block text-xs font-bold uppercase tracking-wider text-[#3e494b] mb-3">Handover Options</label>
                  <div className="flex flex-wrap gap-3">
                    {HANDOVER_OPTIONS.map(opt => (
                      <label key={opt} className="flex items-center gap-2 cursor-pointer bg-[#f3f3f3] rounded-xl px-4 py-2.5">
                        <input
                          type="checkbox"
                          checked={form.handoverOptions.includes(opt)}
                          onChange={() => toggleHandover(opt)}
                          className="accent-[#005f6c]"
                        />
                        <span className="text-sm font-semibold text-[#1a1c1c]">{opt.replace(/([A-Z])/g, ' $1').trim()}</span>
                      </label>
                    ))}
                  </div>
                </div>

                <label className="flex items-center gap-3 cursor-pointer bg-[#f3f3f3] rounded-xl px-4 py-3">
                  <input
                    type="checkbox"
                    checked={form.instantBookEnabled}
                    onChange={e => set('instantBookEnabled', e.target.checked)}
                    className="accent-[#005f6c]"
                  />
                  <div>
                    <p className="font-bold text-sm text-[#1a1c1c]">Enable Instant Book</p>
                    <p className="text-xs text-[#3e494b]">Renters can book without waiting for your approval</p>
                  </div>
                </label>
              </div>
            </div>

            {/* Right: sticky photo upload panel */}
            <div>
              <div className="bg-white rounded-2xl p-6 shadow-[0_4px_24px_rgba(26,28,28,0.06)] lg:sticky lg:top-24">
                <h2 className="font-[Plus_Jakarta_Sans] font-bold text-lg mb-1">Photos</h2>
                <p className="text-xs text-[#3e494b] mb-4">{imageFiles.length} of {MAX_PHOTOS} uploaded</p>

                {/* Numbered photo slots */}
                <div className="grid grid-cols-3 gap-2 mb-4">
                  {Array.from({ length: MAX_PHOTOS }).map((_, i) => (
                    <div key={i} className="aspect-square rounded-xl bg-[#f3f3f3] overflow-hidden flex items-center justify-center">
                      {imagePreviewUrls[i] ? (
                        <img src={imagePreviewUrls[i]} alt="" className="w-full h-full object-cover" />
                      ) : (
                        <span className="text-[#bdc8cb] font-bold text-lg">{i + 1}</span>
                      )}
                    </div>
                  ))}
                </div>

                <label className="flex items-center justify-center gap-2 w-full border-2 border-dashed border-[#bdc8cb]/40 rounded-xl py-3 cursor-pointer hover:border-[#005f6c]/40 transition-colors">
                  <span className="material-symbols-outlined text-[#3e494b]">add_photo_alternate</span>
                  <span className="text-sm font-semibold text-[#3e494b]">Add Photos</span>
                  <input
                    type="file"
                    accept="image/*"
                    multiple
                    onChange={handlePhotoChange}
                    className="hidden"
                  />
                </label>

                <button
                  type="submit"
                  disabled={submitting}
                  className="w-full mt-6 bg-gradient-to-r from-[#005f6c] to-[#007a8a] text-white rounded-full py-4 font-bold hover:opacity-90 transition-opacity disabled:opacity-50 border-none"
                >
                  {submitting ? 'Creating...' : 'Create Listing'}
                </button>
              </div>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
