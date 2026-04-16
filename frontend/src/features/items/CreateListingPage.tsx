import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useToast } from '../../lib/toast';
import { CATEGORY_SCHEMAS, CATEGORY_KEYS, type FieldSchema } from './categorySchemas';
import { AvailabilityCalendar } from './AvailabilityCalendar';

// ── Types ─────────────────────────────────────────────────────────────────────

interface FormState {
  category: string;
  title: string;
  description: string;
  dailyPrice: string;
  instantBookEnabled: boolean;
  deliveryAvailable: boolean;
  imageUrls: string[];
  attributes: Record<string, string>;
  blockedDates: string[];
}

const INITIAL_STATE: FormState = {
  category: '',
  title: '',
  description: '',
  dailyPrice: '',
  instantBookEnabled: false,
  deliveryAvailable: false,
  imageUrls: [''],
  attributes: {},
  blockedDates: [],
};

const STEPS = ['Category', 'Details', 'Attributes', 'Publish'];

// ── Shared input styles ────────────────────────────────────────────────────────

const inputCls =
  'w-full px-4 py-3 rounded-xl border border-outline-variant/40 bg-surface-container-low text-on-surface font-medium text-sm focus:outline-none focus:ring-2 focus:ring-primary/25 focus:border-primary transition-all';
const labelCls = 'block text-xs font-bold uppercase tracking-wider text-on-surface-variant mb-1.5';

// ── Step Indicator ─────────────────────────────────────────────────────────────

function StepIndicator({ current }: { current: number }) {
  return (
    <div className="flex items-center gap-0 mb-10">
      {STEPS.map((label, i) => (
        <div key={label} className="flex items-center flex-1 last:flex-none">
          <div className="flex flex-col items-center">
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold border-2 transition-all ${
                i < current
                  ? 'bg-primary border-primary text-on-primary'
                  : i === current
                  ? 'border-primary text-primary bg-white'
                  : 'border-outline-variant/40 text-on-surface-variant bg-white'
              }`}
            >
              {i < current ? (
                <span className="material-symbols-outlined text-sm">check</span>
              ) : (
                i + 1
              )}
            </div>
            <span
              className={`mt-1.5 text-[10px] font-bold uppercase tracking-wider whitespace-nowrap ${
                i === current ? 'text-primary' : 'text-on-surface-variant'
              }`}
            >
              {label}
            </span>
          </div>
          {i < STEPS.length - 1 && (
            <div
              className={`flex-1 h-0.5 mx-2 mb-5 transition-colors ${
                i < current ? 'bg-primary' : 'bg-outline-variant/30'
              }`}
            />
          )}
        </div>
      ))}
    </div>
  );
}

// ── Step 1: Category ──────────────────────────────────────────────────────────

function CategoryStep({ onSelect }: { onSelect: (cat: string) => void }) {
  return (
    <div>
      <h2 className="font-headline text-2xl font-bold text-on-surface mb-1">What are you listing?</h2>
      <p className="text-sm text-on-surface-variant mb-6">
        Choose the category that best describes your item.
      </p>
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {CATEGORY_KEYS.map((cat) => {
          const schema = CATEGORY_SCHEMAS[cat];
          return (
            <button
              key={cat}
              type="button"
              onClick={() => onSelect(cat)}
              className="flex flex-col items-center gap-2 p-4 rounded-2xl border-2 border-outline-variant/30 bg-white hover:border-primary hover:bg-primary/5 transition-all active:scale-95 group"
            >
              <span className="material-symbols-outlined text-3xl text-on-surface-variant group-hover:text-primary transition-colors">
                {schema.icon}
              </span>
              <span className="text-xs font-bold text-on-surface-variant group-hover:text-primary transition-colors text-center leading-tight">
                {schema.label}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}

// ── Step 2: Details ───────────────────────────────────────────────────────────

interface DetailsStepProps {
  form: FormState;
  setField: <K extends keyof FormState>(key: K, value: FormState[K]) => void;
  error: string | null;
}

function DetailsStep({ form, setField, error }: DetailsStepProps) {
  const schema = CATEGORY_SCHEMAS[form.category];

  return (
    <div className="space-y-5">
      <div>
        <h2 className="font-headline text-2xl font-bold text-on-surface mb-1">
          Item details
        </h2>
        <p className="text-sm text-on-surface-variant">
          Listing as:{' '}
          <span className="inline-flex items-center gap-1 font-semibold text-primary">
            <span className="material-symbols-outlined text-base">{schema.icon}</span>
            {schema.label}
          </span>
        </p>
      </div>

      {error && (
        <div className="flex items-center gap-2 px-4 py-3 rounded-xl bg-error/10 border border-error/20 text-error text-sm font-medium">
          <span className="material-symbols-outlined text-base">error</span>
          {error}
        </div>
      )}

      <div>
        <label className={labelCls}>Title</label>
        <input
          className={inputCls}
          placeholder="e.g. Toyota Camry 2022 — Perfect for road trips"
          value={form.title}
          onChange={(e) => setField('title', e.target.value)}
          maxLength={200}
        />
      </div>

      <div>
        <label className={labelCls}>Description</label>
        <textarea
          className={`${inputCls} resize-none`}
          rows={4}
          placeholder="Describe your item, its condition, what's included, and any rules for renters..."
          value={form.description}
          onChange={(e) => setField('description', e.target.value)}
          maxLength={2000}
        />
      </div>

      <div>
        <label className={labelCls}>Daily Price</label>
        <div className="relative">
          <span className="absolute left-4 top-1/2 -translate-y-1/2 text-on-surface-variant font-semibold text-sm">
            $
          </span>
          <input
            className={`${inputCls} pl-8`}
            type="number"
            min="0"
            step="0.01"
            placeholder="0.00"
            value={form.dailyPrice}
            onChange={(e) => setField('dailyPrice', e.target.value)}
          />
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-1">
        <ToggleField
          icon="bolt"
          label="Instant Book"
          description="Renters can book without waiting for your approval"
          checked={form.instantBookEnabled}
          onChange={(v) => setField('instantBookEnabled', v)}
        />
        <ToggleField
          icon="local_shipping"
          label="Delivery Available"
          description="You can deliver this item to the renter"
          checked={form.deliveryAvailable}
          onChange={(v) => setField('deliveryAvailable', v)}
        />
      </div>
    </div>
  );
}

// ── Step 3: Attributes ────────────────────────────────────────────────────────

interface AttributesStepProps {
  category: string;
  attributes: Record<string, string>;
  setAttr: (key: string, value: string) => void;
}

function AttributesStep({ category, attributes, setAttr }: AttributesStepProps) {
  const schema = CATEGORY_SCHEMAS[category];

  return (
    <div className="space-y-5">
      <div>
        <h2 className="font-headline text-2xl font-bold text-on-surface mb-1">
          {schema.label} details
        </h2>
        <p className="text-sm text-on-surface-variant">
          {schema.fields.length > 0
            ? 'These details help renters find your listing.'
            : 'No extra details required for this category.'}
        </p>
      </div>

      {schema.fields.length === 0 && (
        <div className="flex flex-col items-center justify-center py-12 text-on-surface-variant">
          <span className="material-symbols-outlined text-5xl mb-3 text-outline">check_circle</span>
          <p className="font-semibold">You're all set — click Continue.</p>
        </div>
      )}

      {schema.fields.map((field) => (
        <AttributeField
          key={field.key}
          field={field}
          value={attributes[field.key] ?? ''}
          onChange={(v) => setAttr(field.key, v)}
        />
      ))}
    </div>
  );
}

function AttributeField({
  field,
  value,
  onChange,
}: {
  field: FieldSchema;
  value: string;
  onChange: (v: string) => void;
}) {
  if (field.type === 'boolean') {
    const checked = value === 'true';
    return (
      <div className="flex items-center justify-between p-4 rounded-xl border border-outline-variant/30 bg-surface-container-low">
        <label className="text-sm font-semibold text-on-surface">{field.label}</label>
        <button
          type="button"
          role="switch"
          aria-checked={checked}
          onClick={() => onChange(checked ? 'false' : 'true')}
          className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary/30 ${
            checked ? 'bg-primary' : 'bg-outline-variant/50'
          }`}
        >
          <span
            className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
              checked ? 'translate-x-6' : 'translate-x-1'
            }`}
          />
        </button>
      </div>
    );
  }

  if (field.type === 'select') {
    return (
      <div>
        <label className={labelCls}>{field.label}</label>
        <select
          className={inputCls}
          value={value}
          onChange={(e) => onChange(e.target.value)}
        >
          <option value="">Select…</option>
          {field.options?.map((opt) => (
            <option key={opt} value={opt}>
              {opt}
            </option>
          ))}
        </select>
      </div>
    );
  }

  return (
    <div>
      <label className={labelCls}>{field.label}</label>
      <input
        className={inputCls}
        type={field.type === 'number' ? 'number' : 'text'}
        placeholder={field.placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        min={field.type === 'number' ? 0 : undefined}
        step={field.type === 'number' ? 'any' : undefined}
      />
    </div>
  );
}

// ── Step 4: Publish ───────────────────────────────────────────────────────────

interface PublishStepProps {
  form: FormState;
  setField: <K extends keyof FormState>(key: K, value: FormState[K]) => void;
  error: string | null;
  loading: boolean;
}

function PublishStep({ form, setField, error, loading }: PublishStepProps) {
  const schema = CATEGORY_SCHEMAS[form.category];

  function updateImageUrl(index: number, value: string) {
    const updated = [...form.imageUrls];
    updated[index] = value;
    setField('imageUrls', updated);
  }

  function addImageUrl() {
    setField('imageUrls', [...form.imageUrls, '']);
  }

  function removeImageUrl(index: number) {
    setField('imageUrls', form.imageUrls.filter((_, i) => i !== index));
  }

  function toggleBlockedDate(date: string) {
    const current = form.blockedDates;
    setField(
      'blockedDates',
      current.includes(date) ? current.filter((d) => d !== date) : [...current, date],
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="font-headline text-2xl font-bold text-on-surface mb-1">
          Add photos & publish
        </h2>
        <p className="text-sm text-on-surface-variant">
          Listings with photos get significantly more bookings.
        </p>
      </div>

      {/* Image URLs */}
      <div className="space-y-3">
        <label className={labelCls}>Image URLs</label>
        {form.imageUrls.map((url, i) => (
          <div key={i} className="flex gap-2 items-center">
            <input
              className={inputCls}
              type="url"
              placeholder="https://example.com/photo.jpg"
              value={url}
              onChange={(e) => updateImageUrl(i, e.target.value)}
            />
            {form.imageUrls.length > 1 && (
              <button
                type="button"
                onClick={() => removeImageUrl(i)}
                className="shrink-0 p-2 rounded-lg text-on-surface-variant hover:text-error hover:bg-error/10 transition-colors"
                aria-label="Remove image"
              >
                <span className="material-symbols-outlined text-base">close</span>
              </button>
            )}
          </div>
        ))}
        {/* Preview thumbnails */}
        {form.imageUrls.some((u) => u.trim().length > 0) && (
          <div className="flex gap-2 flex-wrap mt-2">
            {form.imageUrls
              .filter((u) => u.trim().length > 0)
              .map((url, i) => (
                <img
                  key={i}
                  src={url}
                  alt={`Preview ${i + 1}`}
                  className="h-16 w-16 object-cover rounded-lg border border-outline-variant/30"
                  onError={(e) => {
                    (e.target as HTMLImageElement).style.display = 'none';
                  }}
                />
              ))}
          </div>
        )}
        <button
          type="button"
          onClick={addImageUrl}
          className="flex items-center gap-1.5 text-sm font-semibold text-primary hover:text-primary/80 transition-colors"
        >
          <span className="material-symbols-outlined text-base">add</span>
          Add another image
        </button>
      </div>

      {/* Availability calendar */}
      <div className="space-y-2">
        <div>
          <label className={labelCls}>Block unavailable dates</label>
          <p className="text-xs text-on-surface-variant mb-3">
            Click any date to mark it as unavailable for renters. Click again to unblock.
          </p>
        </div>
        <AvailabilityCalendar
          blockedDates={form.blockedDates}
          onToggle={toggleBlockedDate}
        />
      </div>

      {/* Review summary */}
      <div className="rounded-2xl border border-outline-variant/30 bg-surface-container-low p-5 space-y-3">
        <h3 className="font-headline font-bold text-on-surface text-sm uppercase tracking-wider">
          Listing Summary
        </h3>
        <div className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
          <SummaryRow
            icon={schema.icon}
            label="Category"
            value={schema.label}
          />
          <SummaryRow icon="title" label="Title" value={form.title} />
          <SummaryRow
            icon="payments"
            label="Daily Price"
            value={`$${parseFloat(form.dailyPrice || '0').toFixed(2)}`}
          />
          <SummaryRow
            icon="bolt"
            label="Instant Book"
            value={form.instantBookEnabled ? 'Yes' : 'No'}
          />
          <SummaryRow
            icon="local_shipping"
            label="Delivery"
            value={form.deliveryAvailable ? 'Available' : 'Pickup only'}
          />
          {form.blockedDates.length > 0 && (
            <SummaryRow
              icon="event_busy"
              label="Blocked Dates"
              value={`${form.blockedDates.length} day${form.blockedDates.length !== 1 ? 's' : ''}`}
            />
          )}
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-2 px-4 py-3 rounded-xl bg-error/10 border border-error/20 text-error text-sm font-medium">
          <span className="material-symbols-outlined text-base">error</span>
          {error}
        </div>
      )}

      <button
        type="submit"
        disabled={loading}
        className="w-full bg-primary text-on-primary font-bold rounded-full py-4 flex items-center justify-center gap-2 hover:bg-primary/90 transition-all active:scale-[0.98] disabled:opacity-60 disabled:cursor-not-allowed"
      >
        {loading ? (
          <>
            <span className="material-symbols-outlined text-base animate-spin">progress_activity</span>
            Publishing…
          </>
        ) : (
          <>
            <span className="material-symbols-outlined text-base">rocket_launch</span>
            Publish Listing
          </>
        )}
      </button>
    </div>
  );
}

// ── Shared sub-components ─────────────────────────────────────────────────────

function ToggleField({
  icon,
  label,
  description,
  checked,
  onChange,
}: {
  icon: string;
  label: string;
  description: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <button
      type="button"
      onClick={() => onChange(!checked)}
      className={`text-left p-4 rounded-2xl border-2 transition-all w-full ${
        checked
          ? 'border-primary bg-primary/5'
          : 'border-outline-variant/30 bg-surface-container-low hover:border-primary/40'
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2">
          <span
            className={`material-symbols-outlined text-xl ${
              checked ? 'text-primary' : 'text-on-surface-variant'
            }`}
          >
            {icon}
          </span>
          <span
            className={`font-bold text-sm ${checked ? 'text-primary' : 'text-on-surface'}`}
          >
            {label}
          </span>
        </div>
        <div
          className={`shrink-0 w-10 h-6 rounded-full relative transition-colors ${
            checked ? 'bg-primary' : 'bg-outline-variant/50'
          }`}
        >
          <span
            className={`absolute top-1 h-4 w-4 rounded-full bg-white shadow transition-transform ${
              checked ? 'translate-x-5' : 'translate-x-1'
            }`}
          />
        </div>
      </div>
      <p className="text-xs text-on-surface-variant mt-2 leading-relaxed">{description}</p>
    </button>
  );
}

function SummaryRow({ icon, label, value }: { icon: string; label: string; value: string }) {
  return (
    <div className="flex items-center gap-2 min-w-0">
      <span className="material-symbols-outlined text-base text-on-surface-variant shrink-0">
        {icon}
      </span>
      <span className="text-on-surface-variant shrink-0">{label}:</span>
      <span className="font-semibold text-on-surface truncate">{value}</span>
    </div>
  );
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export function CreateListingPage() {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [step, setStep] = useState(0);
  const [form, setForm] = useState<FormState>(INITIAL_STATE);
  const [stepError, setStepError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const selectedSchema = form.category ? CATEGORY_SCHEMAS[form.category] : null;

  function setField<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function setAttr(key: string, value: string) {
    setForm((prev) => ({ ...prev, attributes: { ...prev.attributes, [key]: value } }));
  }

  function selectCategory(cat: string) {
    const schema = CATEGORY_SCHEMAS[cat];
    const initialAttrs: Record<string, string> = {};
    for (const field of schema?.fields ?? []) {
      if (field.type === 'boolean') initialAttrs[field.key] = 'false';
    }
    setForm((prev) => ({ ...prev, category: cat, attributes: initialAttrs }));
    setStepError(null);
    setStep(1);
  }

  function validateStep(): string | null {
    if (step === 1) {
      if (!form.title.trim()) return 'Title is required.';
      if (!form.description.trim()) return 'Description is required.';
      const price = parseFloat(form.dailyPrice);
      if (!form.dailyPrice || isNaN(price) || price <= 0) return 'A valid daily price is required.';
    }
    return null;
  }

  function goNext() {
    const err = validateStep();
    if (err) { setStepError(err); return; }
    setStepError(null);
    setStep((s) => Math.min(s + 1, STEPS.length - 1));
  }

  function goBack() {
    setStepError(null);
    if (step === 0) { navigate('/'); return; }
    setStep((s) => s - 1);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setStepError(null);
    setLoading(true);

    const schema = selectedSchema?.fields ?? [];
    const typedAttributes: Record<string, unknown> = {};
    for (const field of schema) {
      const raw = form.attributes[field.key];
      if (raw === undefined || raw === '') continue;
      if (field.type === 'number') typedAttributes[field.key] = Number(raw);
      else if (field.type === 'boolean') typedAttributes[field.key] = raw === 'true';
      else typedAttributes[field.key] = raw;
    }

    try {
      const { data: createdItem } = await apiClient.post<{ id: string }>('/api/items', {
        title: form.title.trim(),
        description: form.description.trim(),
        dailyPrice: parseFloat(form.dailyPrice),
        category: form.category,
        instantBookEnabled: form.instantBookEnabled,
        deliveryAvailable: form.deliveryAvailable,
        imageUrls: form.imageUrls.filter((u) => u.trim().length > 0),
        attributes: typedAttributes,
      });

      if (form.blockedDates.length > 0) {
        await apiClient.post(`/api/items/${createdItem.id}/blocked-dates`, {
          dates: form.blockedDates,
        });
      }

      showToast('Listing published successfully!');
      navigate('/');
    } catch {
      setStepError('Failed to create listing. Please check your details and try again.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen bg-surface font-body text-on-surface antialiased">
      {/* Top bar */}
      <header className="sticky top-0 z-50 bg-white/80 backdrop-blur-xl border-b border-outline-variant/15">
        <div className="max-w-2xl mx-auto px-6 h-16 flex items-center justify-between">
          <button
            type="button"
            onClick={goBack}
            className="flex items-center gap-1 text-sm font-semibold text-on-surface-variant hover:text-primary transition-colors"
          >
            <span className="material-symbols-outlined text-base">arrow_back</span>
            {step === 0 ? 'Home' : 'Back'}
          </button>
          <Link to="/" className="font-headline text-xl font-black text-primary tracking-tight">
            Borro
          </Link>
          <span className="text-xs text-on-surface-variant font-medium">
            Step {step + 1} of {STEPS.length}
          </span>
        </div>
      </header>

      <main className="max-w-2xl mx-auto px-6 py-10">
        <StepIndicator current={step} />

        <form onSubmit={handleSubmit} noValidate>
          {step === 0 && <CategoryStep onSelect={selectCategory} />}

          {step === 1 && (
            <>
              <DetailsStep form={form} setField={setField} error={stepError} />
              <div className="mt-8 flex justify-end">
                <button
                  type="button"
                  onClick={goNext}
                  className="bg-primary text-on-primary font-bold rounded-full px-8 py-3 flex items-center gap-2 hover:bg-primary/90 transition-all active:scale-95"
                >
                  Continue
                  <span className="material-symbols-outlined text-base">arrow_forward</span>
                </button>
              </div>
            </>
          )}

          {step === 2 && (
            <>
              <AttributesStep
                category={form.category}
                attributes={form.attributes}
                setAttr={setAttr}
              />
              <div className="mt-8 flex justify-end">
                <button
                  type="button"
                  onClick={goNext}
                  className="bg-primary text-on-primary font-bold rounded-full px-8 py-3 flex items-center gap-2 hover:bg-primary/90 transition-all active:scale-95"
                >
                  Continue
                  <span className="material-symbols-outlined text-base">arrow_forward</span>
                </button>
              </div>
            </>
          )}

          {step === 3 && (
            <PublishStep
              form={form}
              setField={setField}
              error={stepError}
              loading={loading}
            />
          )}
        </form>
      </main>
    </div>
  );
}
