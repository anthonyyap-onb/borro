import { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { bookingApi, type BookingDto, type BookingStatus } from './bookingApi';
import { useChat } from './useChat';

const STATUS_LABELS: Record<BookingStatus, string> = {
  PendingApproval: 'Pending Approval',
  Approved: 'Approved',
  PaymentHeld: 'Awaiting Pickup',
  Active: 'Active',
  Completed: 'Completed',
  Disputed: 'Disputed',
  Cancelled: 'Cancelled',
};

const STATUS_BADGE: Record<BookingStatus, string> = {
  PendingApproval: 'bg-[#d5e0f7] text-[#3c475a]',
  Approved: 'bg-[#daf8ff] text-[#004e59]',
  PaymentHeld: 'bg-[#007a8a] text-white',
  Active: 'bg-[#007a8a] text-white',
  Completed: 'bg-[#e8e8e8] text-[#1a1c1c]',
  Disputed: 'bg-[#ffdad6] text-[#93000a]',
  Cancelled: 'bg-[#e8e8e8] text-[#6e797b]',
};

function formatDate(utc: string) {
  return new Date(utc).toLocaleDateString(undefined, {
    month: 'short', day: 'numeric', year: 'numeric',
  });
}

function formatTime(utc: string) {
  return new Date(utc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function diffDays(start: string, end: string) {
  return Math.max(1, Math.round(
    (new Date(end).getTime() - new Date(start).getTime()) / 86400000
  ));
}

function AvatarInitial({ name, size = 'md' }: { name: string; size?: 'sm' | 'md' }) {
  const cls = size === 'sm'
    ? 'w-7 h-7 text-xs'
    : 'w-10 h-10 text-sm';
  return (
    <div className={`${cls} rounded-full bg-[#005f6c] flex items-center justify-center text-white font-bold flex-shrink-0`}>
      {name.charAt(0).toUpperCase()}
    </div>
  );
}

export function BookingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [booking, setBooking] = useState<BookingDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [messageInput, setMessageInput] = useState('');
  const [transitioning, setTransitioning] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const chatActive = booking && !['PendingApproval', 'Cancelled'].includes(booking.status);
  const { messages, connected, sendMessage, setMessages } = useChat(chatActive ? (id ?? '') : '');

  useEffect(() => {
    if (!id) return;
    bookingApi.getById(id)
      .then(res => setBooking(res.data))
      .catch(() => navigate('/'))
      .finally(() => setLoading(false));

    bookingApi.getMessages(id)
      .then(res => setMessages(res.data))
      .catch(() => {});
  }, [id, navigate, setMessages]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleTransition = async (status: BookingStatus) => {
    if (!id || transitioning) return;
    setTransitioning(true);
    try {
      const res = await bookingApi.transition(id, status);
      setBooking(res.data);
    } finally {
      setTransitioning(false);
    }
  };

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!messageInput.trim() || !connected) return;
    await sendMessage(messageInput.trim());
    setMessageInput('');
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[#f9f9f9]">
        <span className="material-symbols-outlined animate-spin text-[#005f6c] text-4xl">progress_activity</span>
      </div>
    );
  }
  if (!booking) return null;

  const isLender = user?.userId === booking.lenderId;
  const isRenter = user?.userId === booking.renterId;
  const days = diffDays(booking.startDateUtc, booking.endDateUtc);
  const otherName = isLender ? booking.renterName : booking.lenderName;
  const serviceFee = booking.totalPrice * 0.05;
  const lenderPayout = booking.totalPrice - serviceFee;

  return (
    <div className="min-h-screen bg-[#f9f9f9] font-[Manrope]">
      {/* Top App Bar */}
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

      <main className="pt-24 pb-12 px-6 max-w-7xl mx-auto min-h-screen">
        {/* Breadcrumb */}
        <div className="mb-8 flex items-center gap-4">
          <button
            onClick={() => navigate(-1)}
            className="flex items-center text-[#3e494b] hover:text-[#005f6c] transition-colors bg-transparent border-none cursor-pointer font-medium"
          >
            <span className="material-symbols-outlined mr-1">arrow_back</span>
            My Bookings
          </button>
          <div className="h-4 w-px bg-[#bdc8cb]/30" />
          <span className="text-[#6e797b] font-medium">Booking ID: #{booking.id.slice(0, 8).toUpperCase()}</span>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* ── Left Column ─────────────────────────────── */}
          <div className="lg:col-span-7 space-y-6">
            {isRenter ? (
              <RenterView
                booking={booking}
                days={days}
                transitioning={transitioning}
                onTransition={handleTransition}
              />
            ) : (
              <LenderView
                booking={booking}
                days={days}
                serviceFee={serviceFee}
                lenderPayout={lenderPayout}
                transitioning={transitioning}
                onTransition={handleTransition}
              />
            )}
          </div>

          {/* ── Right Column: Chat ───────────────────────── */}
          <div className="lg:col-span-5 lg:sticky lg:top-24" style={{ height: 'calc(100vh - 140px)' }}>
            <ChatPanel
              booking={booking}
              otherName={otherName}
              isRenter={isRenter}
              chatActive={!!chatActive}
              connected={connected}
              messages={messages}
              messageInput={messageInput}
              onInputChange={setMessageInput}
              onSend={handleSend}
              currentUserId={user?.userId ?? ''}
              messagesEndRef={messagesEndRef}
            />
          </div>
        </div>
      </main>
    </div>
  );
}

/* ─── Renter View ─────────────────────────────────────────── */
function RenterView({
  booking, days, transitioning, onTransition,
}: {
  booking: BookingDto;
  days: number;
  transitioning: boolean;
  onTransition: (s: BookingStatus) => void;
}) {
  return (
    <>
      {/* Hero Card */}
      <div className="bg-white rounded-[32px] overflow-hidden shadow-[0px_20px_40px_rgba(26,28,28,0.04)]">
        <div className="relative h-[360px]">
          {booking.itemImageUrl ? (
            <img
              src={booking.itemImageUrl}
              alt={booking.itemTitle}
              className="w-full h-full object-cover"
            />
          ) : (
            <div className="w-full h-full bg-gradient-to-br from-[#007a8a] to-[#004e59] flex items-center justify-center">
              <span className="material-symbols-outlined text-white/30 text-8xl">inventory_2</span>
            </div>
          )}
          <div className="absolute top-5 left-5">
            <span className={`px-5 py-2.5 rounded-full font-bold text-sm flex items-center gap-2 shadow-lg ${STATUS_BADGE[booking.status]}`}>
              <span className="material-symbols-outlined text-[18px]" style={{ fontVariationSettings: "'FILL' 1" }}>schedule</span>
              {STATUS_LABELS[booking.status]}
            </span>
          </div>
        </div>

        <div className="p-8">
          <div className="flex justify-between items-start mb-5">
            <div>
              <h1 className="font-[Plus_Jakarta_Sans] text-3xl font-extrabold tracking-tight text-[#1a1c1c] mb-2">
                {booking.itemTitle}
              </h1>
              <div className="flex items-center gap-3">
                <div className="flex items-center bg-[#d5e0f7] px-3 py-1 rounded-full">
                  <span className="material-symbols-outlined text-[#005f6c] text-sm mr-1" style={{ fontVariationSettings: "'FILL' 1" }}>verified</span>
                  <span className="text-xs font-bold text-[#3c475a]">VERIFIED OWNER</span>
                </div>
                <span className="text-[#3e494b] text-sm font-medium">Hosted by {booking.lenderName}</span>
              </div>
            </div>
            <div className="text-right">
              <div className="text-2xl font-extrabold text-[#005f6c]">
                ${booking.dailyPrice.toFixed(2)}<span className="text-sm font-normal text-[#6e797b]">/day</span>
              </div>
            </div>
          </div>

          {/* Action Bar */}
          <div className="flex flex-wrap gap-3 pt-5 border-t border-[#e2e2e2]/30">
            {booking.status === 'PaymentHeld' && (
              <button
                onClick={() => onTransition('Active')}
                disabled={transitioning}
                className="bg-[#005f6c] hover:brightness-110 text-white px-7 py-3.5 rounded-full font-bold transition-all flex items-center gap-2 shadow-md active:scale-95 border-none cursor-pointer disabled:opacity-60"
              >
                Confirm Pickup
                <span className="material-symbols-outlined">check_circle</span>
              </button>
            )}
            {booking.status === 'Approved' && (
              <button
                onClick={() => onTransition('PaymentHeld')}
                disabled={transitioning}
                className="bg-[#005f6c] hover:brightness-110 text-white px-7 py-3.5 rounded-full font-bold transition-all flex items-center gap-2 shadow-md active:scale-95 border-none cursor-pointer disabled:opacity-60"
              >
                Confirm &amp; Pay
                <span className="material-symbols-outlined">payments</span>
              </button>
            )}
            {booking.status === 'Active' && (
              <button
                onClick={() => onTransition('Completed')}
                disabled={transitioning}
                className="bg-[#005f6c] hover:brightness-110 text-white px-7 py-3.5 rounded-full font-bold transition-all flex items-center gap-2 shadow-md active:scale-95 border-none cursor-pointer disabled:opacity-60"
              >
                Confirm Return
                <span className="material-symbols-outlined">task_alt</span>
              </button>
            )}
            {['PendingApproval', 'Approved'].includes(booking.status) && (
              <button
                onClick={() => onTransition('Cancelled')}
                disabled={transitioning}
                className="text-[#a91929] hover:bg-[#ffdad6]/30 px-7 py-3.5 rounded-full font-bold transition-all border-none bg-transparent cursor-pointer disabled:opacity-60"
              >
                Cancel Booking
              </button>
            )}
            {['PaymentHeld', 'Active', 'Completed'].includes(booking.status) && booking.status !== 'Disputed' && (
              <button
                onClick={() => onTransition('Disputed')}
                disabled={transitioning}
                className="bg-[#ffdad6] text-[#93000a] hover:bg-[#ffb3af] px-7 py-3.5 rounded-full font-bold transition-all flex items-center gap-2 active:scale-95 border-none cursor-pointer disabled:opacity-60"
              >
                <span className="material-symbols-outlined text-base">flag</span>
                Raise Dispute
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Bento: Rental Timeline + Cost Breakdown */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <div className="bg-[#f3f3f3] p-7 rounded-[32px] space-y-5">
          <h3 className="font-[Plus_Jakarta_Sans] font-bold text-base flex items-center gap-2">
            <span className="material-symbols-outlined text-[#005f6c]">calendar_today</span>
            Rental Timeline
          </h3>
          <div className="space-y-3 text-sm">
            <div className="flex justify-between items-center">
              <span className="text-[#3e494b]">Pickup Date</span>
              <span className="font-bold">{formatDate(booking.startDateUtc)}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-[#3e494b]">Return Date</span>
              <span className="font-bold">{formatDate(booking.endDateUtc)}</span>
            </div>
            <div className="h-px bg-[#bdc8cb]/30" />
            <div className="flex justify-between items-center text-[#005f6c]">
              <span className="font-medium">Total Duration</span>
              <span className="font-bold text-base">{days} {days === 1 ? 'Day' : 'Days'}</span>
            </div>
          </div>
        </div>

        <div className="bg-[#f3f3f3] p-7 rounded-[32px] space-y-5">
          <h3 className="font-[Plus_Jakarta_Sans] font-bold text-base flex items-center gap-2">
            <span className="material-symbols-outlined text-[#005f6c]">payments</span>
            Cost Breakdown
          </h3>
          <div className="space-y-3 text-sm">
            <div className="flex justify-between items-center">
              <span className="text-[#3e494b]">${booking.dailyPrice.toFixed(2)} × {days} {days === 1 ? 'day' : 'days'}</span>
              <span className="font-medium">${booking.totalPrice.toFixed(2)}</span>
            </div>
            <div className="h-px bg-[#bdc8cb]/30" />
            <div className="flex justify-between items-center">
              <span className="font-bold text-base">Total Paid</span>
              <span className="font-extrabold text-xl text-[#005f6c]">${booking.totalPrice.toFixed(2)}</span>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

/* ─── Lender View ─────────────────────────────────────────── */
function LenderView({
  booking, days, serviceFee, lenderPayout, transitioning, onTransition,
}: {
  booking: BookingDto;
  days: number;
  serviceFee: number;
  lenderPayout: number;
  transitioning: boolean;
  onTransition: (s: BookingStatus) => void;
}) {
  return (
    <>
      {/* Status Header */}
      <section className="flex flex-col gap-3">
        <div className="flex items-center gap-3 flex-wrap">
          <span className={`px-4 py-1.5 rounded-full text-xs font-bold uppercase tracking-wider ${STATUS_BADGE[booking.status]}`}>
            {STATUS_LABELS[booking.status]}
          </span>
          <span className="text-[#6e797b] text-sm font-medium">Booking ID: #{booking.id.slice(0, 8).toUpperCase()}</span>
        </div>
        <h1 className="font-[Plus_Jakarta_Sans] text-4xl font-extrabold tracking-tight text-[#1a1c1c]">
          {booking.itemTitle}
        </h1>
        <p className="text-[#3e494b] text-lg">
          Requested by <span className="font-bold text-[#1a1c1c]">{booking.renterName}</span>
        </p>
      </section>

      {/* Item Image */}
      <div className="relative h-80 w-full rounded-xl overflow-hidden bg-[#eeeeee]">
        {booking.itemImageUrl ? (
          <img
            src={booking.itemImageUrl}
            alt={booking.itemTitle}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full bg-gradient-to-br from-[#007a8a] to-[#004e59] flex items-center justify-center">
            <span className="material-symbols-outlined text-white/30 text-8xl">inventory_2</span>
          </div>
        )}
        <div className="absolute bottom-3 left-3 bg-white/80 backdrop-blur px-3 py-1.5 rounded-xl flex items-center gap-1.5">
          <span className="material-symbols-outlined text-[#005f6c] text-sm" style={{ fontVariationSettings: "'FILL' 1" }}>verified</span>
          <span className="text-sm font-bold text-[#005f6c]">Verified Item</span>
        </div>
      </div>

      {/* 2-col: Rental Period + Estimated Earnings */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="p-6 bg-[#f3f3f3] rounded-xl flex flex-col gap-2">
          <span className="text-[#3e494b] text-sm font-medium flex items-center gap-2">
            <span className="material-symbols-outlined text-sm">calendar_month</span>
            Rental Period
          </span>
          <p className="font-[Plus_Jakarta_Sans] text-xl font-bold">
            {formatDate(booking.startDateUtc)} — {formatDate(booking.endDateUtc)}
          </p>
          <p className="text-[#3e494b] text-sm">{days} {days === 1 ? 'Day' : 'Days'} Total</p>
        </div>
        <div className="p-6 bg-[#f3f3f3] rounded-xl flex flex-col gap-2">
          <span className="text-[#3e494b] text-sm font-medium flex items-center gap-2">
            <span className="material-symbols-outlined text-sm">payments</span>
            Estimated Earnings
          </span>
          <p className="font-[Plus_Jakarta_Sans] text-xl font-bold text-[#005f6c]">${lenderPayout.toFixed(2)}</p>
          <p className="text-[#3e494b] text-sm">After Borro service fee</p>
        </div>
      </div>

      {/* Financial Summary */}
      <section className="bg-white p-8 rounded-xl shadow-[0px_10px_30px_rgba(26,28,28,0.02)]">
        <h3 className="font-[Plus_Jakarta_Sans] text-xl font-bold mb-6">Financial Summary</h3>
        <div className="space-y-4 text-sm">
          <div className="flex justify-between items-center text-[#3e494b]">
            <span>${booking.dailyPrice.toFixed(2)} × {days} {days === 1 ? 'day' : 'days'}</span>
            <span>${booking.totalPrice.toFixed(2)}</span>
          </div>
          <div className="flex justify-between items-center text-[#3e494b]">
            <span>Insurance Coverage</span>
            <span className="text-[#005f6c] font-medium">Included</span>
          </div>
          <div className="flex justify-between items-center text-[#3e494b]">
            <span>Borro Service Fee (5%)</span>
            <span className="text-[#a91929]">-${serviceFee.toFixed(2)}</span>
          </div>
          <div className="pt-4 border-t border-[#e2e2e2]/30 flex justify-between items-center">
            <span className="font-[Plus_Jakarta_Sans] text-lg font-bold">Total Payout</span>
            <span className="font-[Plus_Jakarta_Sans] text-2xl font-extrabold text-[#005f6c]">${lenderPayout.toFixed(2)}</span>
          </div>
        </div>
      </section>

      {/* Lender Actions */}
      <div className="flex flex-col sm:flex-row gap-4">
        {booking.status === 'PendingApproval' && (
          <>
            <button
              onClick={() => onTransition('Approved')}
              disabled={transitioning}
              className="flex-1 bg-[#005f6c] text-white py-4 px-8 rounded-full font-[Plus_Jakarta_Sans] font-bold transition-all hover:brightness-110 active:scale-95 shadow-lg shadow-[#005f6c]/20 border-none cursor-pointer disabled:opacity-60"
            >
              Approve Request
            </button>
            <button
              onClick={() => onTransition('Cancelled')}
              disabled={transitioning}
              className="flex-1 bg-[#e2e2e2] text-[#1a1c1c] py-4 px-8 rounded-full font-[Plus_Jakarta_Sans] font-bold transition-all hover:bg-[#e8e8e8] active:scale-95 border-none cursor-pointer disabled:opacity-60"
            >
              Decline Request
            </button>
          </>
        )}
        {booking.status === 'Approved' && (
          <button
            onClick={() => onTransition('Cancelled')}
            disabled={transitioning}
            className="bg-[#ffdad6] text-[#93000a] py-4 px-8 rounded-full font-bold hover:bg-[#ffb3af] active:scale-95 transition-all border-none cursor-pointer disabled:opacity-60"
          >
            Cancel Booking
          </button>
        )}
        {['PaymentHeld', 'Active', 'Completed'].includes(booking.status) && booking.status !== 'Disputed' && (
          <button
            onClick={() => onTransition('Disputed')}
            disabled={transitioning}
            className="bg-[#ffdad6] text-[#93000a] py-4 px-8 rounded-full font-bold hover:bg-[#ffb3af] active:scale-95 transition-all flex items-center gap-2 border-none cursor-pointer disabled:opacity-60"
          >
            <span className="material-symbols-outlined text-base">flag</span>
            Raise Dispute
          </button>
        )}
      </div>
    </>
  );
}

/* ─── Chat Panel ──────────────────────────────────────────── */
function ChatPanel({
  otherName, chatActive, connected, messages, messageInput,
  onInputChange, onSend, currentUserId, messagesEndRef,
}: {
  booking: BookingDto;
  otherName: string;
  isRenter: boolean;
  chatActive: boolean;
  connected: boolean;
  messages: ReturnType<typeof useChat>['messages'];
  messageInput: string;
  onInputChange: (v: string) => void;
  onSend: (e: React.FormEvent) => void;
  currentUserId: string;
  messagesEndRef: React.RefObject<HTMLDivElement>;
}) {
  return (
    <div className="bg-white rounded-[32px] flex flex-col h-full shadow-[0px_20px_40px_rgba(26,28,28,0.06)] border border-[#e2e2e2]/50 overflow-hidden">
      {/* Chat Header */}
      <div className="p-5 flex items-center justify-between bg-white border-b border-[#e2e2e2]/30">
        <div className="flex items-center gap-3">
          <div className="relative">
            <AvatarInitial name={otherName} />
            {chatActive && connected && (
              <div className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full" />
            )}
          </div>
          <div>
            <h4 className="font-[Plus_Jakarta_Sans] font-bold text-base leading-tight">{otherName}</h4>
            <p className="text-xs text-[#6e797b] font-medium">
              {chatActive
                ? connected ? 'Online' : 'Connecting...'
                : 'Chat available after approval'}
            </p>
          </div>
        </div>
        <div className="flex gap-1">
          <button className="material-symbols-outlined p-2 hover:bg-[#f3f3f3] rounded-full transition-colors text-[#3e494b] border-none bg-transparent cursor-pointer">info</button>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-5 space-y-4" style={{ scrollbarWidth: 'none' }}>
        {!chatActive ? (
          <div className="h-full flex flex-col items-center justify-center gap-3 text-center px-8">
            <span className="material-symbols-outlined text-5xl text-[#bdc8cb]">chat_bubble</span>
            <p className="text-sm text-[#6e797b] font-medium">
              Chat opens once the lender approves this booking.
            </p>
          </div>
        ) : messages.length === 0 ? (
          <div className="h-full flex flex-col items-center justify-center gap-3 text-center px-8">
            <span className="material-symbols-outlined text-5xl text-[#bdc8cb]">forum</span>
            <p className="text-sm text-[#6e797b] font-medium">
              No messages yet. Say hi to coordinate handover!
            </p>
          </div>
        ) : (
          messages.map(msg => {
            const isMine = msg.senderId === currentUserId;
            return (
              <div key={msg.id} className={`flex gap-2 max-w-[85%] ${isMine ? 'ml-auto flex-row-reverse' : ''}`}>
                {!isMine && <AvatarInitial name={msg.senderName} size="sm" />}
                <div className={`p-3.5 text-sm leading-relaxed ${
                  isMine
                    ? 'bg-[#005f6c] text-white rounded-t-2xl rounded-bl-2xl'
                    : 'bg-[#f3f3f3] text-[#1a1c1c] rounded-t-2xl rounded-br-2xl'
                }`}>
                  <p>{msg.content}</p>
                  <span className={`text-[10px] block mt-1.5 ${isMine ? 'text-white/50 text-right' : 'text-[#6e797b]'}`}>
                    {formatTime(msg.createdAtUtc)}
                  </span>
                </div>
              </div>
            );
          })
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <div className="p-4 pt-2 border-t border-[#e2e2e2]/30">
        {chatActive ? (
          <form onSubmit={onSend}>
            <div className="flex items-center gap-2 bg-[#f3f3f3] rounded-full p-2">
              <button type="button" className="material-symbols-outlined p-1.5 text-[#3e494b] hover:text-[#005f6c] border-none bg-transparent cursor-pointer">add_circle</button>
              <input
                type="text"
                value={messageInput}
                onChange={e => onInputChange(e.target.value)}
                placeholder={connected ? 'Type a message...' : 'Connecting...'}
                disabled={!connected}
                className="flex-1 bg-transparent border-none focus:outline-none text-sm py-1.5 px-1 placeholder:text-[#6e797b] disabled:opacity-50"
              />
              <button
                type="submit"
                disabled={!connected || !messageInput.trim()}
                className="bg-[#005f6c] text-white p-2 rounded-full transition-transform active:scale-90 disabled:opacity-40 border-none cursor-pointer flex items-center justify-center"
              >
                <span className="material-symbols-outlined text-sm">send</span>
              </button>
            </div>
          </form>
        ) : (
          <div className="flex items-center justify-center gap-2 py-2 text-sm text-[#6e797b]">
            <span className="material-symbols-outlined text-base">lock</span>
            Chat locked until booking is approved
          </div>
        )}
      </div>
    </div>
  );
}

