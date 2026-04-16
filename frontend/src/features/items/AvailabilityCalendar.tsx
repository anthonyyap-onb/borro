import { useState } from 'react';

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Format a local Date as "YYYY-MM-DD" without timezone shift. */
function toDateString(d: Date): string {
  return [
    d.getFullYear(),
    String(d.getMonth() + 1).padStart(2, '0'),
    String(d.getDate()).padStart(2, '0'),
  ].join('-');
}

/** Today at midnight local time — used to disable past dates. */
function todayMidnight(): Date {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  return d;
}

const WEEKDAYS = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];
const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

// ── Calendar Grid Builder ─────────────────────────────────────────────────────

interface DayCell {
  date: Date;
  dateStr: string;
  isCurrentMonth: boolean;
  isPast: boolean;
  isToday: boolean;
}

function buildCalendarGrid(year: number, month: number): DayCell[] {
  const today = todayMidnight();
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);

  // Leading empty slots (0 = Sunday start)
  const leadingDays = firstDay.getDay();
  const cells: DayCell[] = [];

  // Pad with previous-month days (just filler, not interactive)
  for (let i = leadingDays - 1; i >= 0; i--) {
    const d = new Date(year, month, -i);
    cells.push({ date: d, dateStr: toDateString(d), isCurrentMonth: false, isPast: d < today, isToday: false });
  }

  // Current month days
  for (let day = 1; day <= lastDay.getDate(); day++) {
    const d = new Date(year, month, day);
    cells.push({
      date: d,
      dateStr: toDateString(d),
      isCurrentMonth: true,
      isPast: d < today,
      isToday: toDateString(d) === toDateString(today),
    });
  }

  // Trailing days to complete the last row
  const trailing = 7 - (cells.length % 7);
  if (trailing < 7) {
    for (let i = 1; i <= trailing; i++) {
      const d = new Date(year, month + 1, i);
      cells.push({ date: d, dateStr: toDateString(d), isCurrentMonth: false, isPast: d < today, isToday: false });
    }
  }

  return cells;
}

// ── Component ─────────────────────────────────────────────────────────────────

interface AvailabilityCalendarProps {
  /** ISO date strings ("YYYY-MM-DD") that are blocked. */
  blockedDates: string[];
  /** Called when a date is clicked — parent toggles the date. */
  onToggle: (date: string) => void;
}

export function AvailabilityCalendar({ blockedDates, onToggle }: AvailabilityCalendarProps) {
  const today = new Date();
  const [viewYear, setViewYear] = useState(today.getFullYear());
  const [viewMonth, setViewMonth] = useState(today.getMonth());

  const blockedSet = new Set(blockedDates);
  const cells = buildCalendarGrid(viewYear, viewMonth);

  function prevMonth() {
    if (viewMonth === 0) { setViewYear(y => y - 1); setViewMonth(11); }
    else setViewMonth(m => m - 1);
  }

  function nextMonth() {
    if (viewMonth === 11) { setViewYear(y => y + 1); setViewMonth(0); }
    else setViewMonth(m => m + 1);
  }

  // Prevent navigating before current month
  const isAtMinMonth = viewYear === today.getFullYear() && viewMonth === today.getMonth();

  return (
    <div className="rounded-2xl border border-outline-variant/30 bg-white p-4 select-none">
      {/* Month navigation */}
      <div className="flex items-center justify-between mb-4">
        <button
          type="button"
          onClick={prevMonth}
          disabled={isAtMinMonth}
          className="p-1.5 rounded-lg hover:bg-surface-container-low transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
          aria-label="Previous month"
        >
          <span className="material-symbols-outlined text-lg text-on-surface-variant">chevron_left</span>
        </button>

        <span className="font-headline font-bold text-on-surface text-sm">
          {MONTH_NAMES[viewMonth]} {viewYear}
        </span>

        <button
          type="button"
          onClick={nextMonth}
          className="p-1.5 rounded-lg hover:bg-surface-container-low transition-colors"
          aria-label="Next month"
        >
          <span className="material-symbols-outlined text-lg text-on-surface-variant">chevron_right</span>
        </button>
      </div>

      {/* Weekday headers */}
      <div className="grid grid-cols-7 mb-1">
        {WEEKDAYS.map((d) => (
          <div key={d} className="text-center text-[10px] font-bold uppercase tracking-wider text-on-surface-variant py-1">
            {d}
          </div>
        ))}
      </div>

      {/* Day grid */}
      <div className="grid grid-cols-7 gap-y-0.5">
        {cells.map((cell) => {
          const isBlocked = blockedSet.has(cell.dateStr);
          const disabled = cell.isPast || !cell.isCurrentMonth;

          return (
            <button
              key={cell.dateStr}
              type="button"
              disabled={disabled}
              onClick={() => !disabled && onToggle(cell.dateStr)}
              aria-label={`${cell.dateStr}${isBlocked ? ' (blocked)' : ''}`}
              aria-pressed={isBlocked}
              className={[
                'relative mx-auto flex h-9 w-9 items-center justify-center rounded-full text-sm font-medium transition-all',
                disabled
                  ? 'text-on-surface-variant/30 cursor-default'
                  : isBlocked
                  ? 'bg-error text-white hover:bg-error/85 cursor-pointer'
                  : cell.isToday
                  ? 'border-2 border-primary text-primary hover:bg-primary/10 cursor-pointer'
                  : 'text-on-surface hover:bg-surface-container-low cursor-pointer',
              ].join(' ')}
            >
              {cell.date.getDate()}
            </button>
          );
        })}
      </div>

      {/* Legend */}
      <div className="flex items-center gap-4 mt-4 pt-3 border-t border-outline-variant/20 text-xs text-on-surface-variant">
        <span className="flex items-center gap-1.5">
          <span className="inline-block w-3 h-3 rounded-full bg-error" />
          Blocked
        </span>
        <span className="flex items-center gap-1.5">
          <span className="inline-block w-3 h-3 rounded-full border-2 border-primary" />
          Today
        </span>
        <span className="ml-auto text-[11px]">
          {blockedDates.length} date{blockedDates.length !== 1 ? 's' : ''} blocked
        </span>
      </div>
    </div>
  );
}
