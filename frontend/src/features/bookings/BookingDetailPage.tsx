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

const STATUS_COLOR: Record<BookingStatus, string> = {
  PendingApproval: 'bg-[#d5e0f7] text-[#3c475a]',
  Approved:        'bg-[#d5e0f7] text-[#005f6c]',
  PaymentHeld:     'bg-[#daf8ff] text-[#004e59]',
  Active:          'bg-[#007a8a] text-white',
  Completed:       'bg-[#e8e8e8] text-[#1a1c1c]',
  Disputed:        'bg-[#ffdad6] text-[#93000a]',
  Cancelled:       'bg-[#e8e8e8] text-[#6e797b]',
};

function formatDate(utc: string) {
  return new Date(utc).toLocaleDateString(undefined, {
    month: 'short', day: 'numeric', year: 'numeric',
  });
}

function diffDays(start: string, end: string) {
  return Math.max(1, Math.round(
    (new Date(end).getTime() - new Date(start).getTime()) / 86400000
  ));
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
      <div className="min-h-screen flex items-center justify-center bg-[#f9f9f9] font-[Manrope]">
        <span className="material-symbols-outlined animate-spin text-[#005f6c] text-4xl">progress_activity</span>
      </div>
    );
  }
  if (!booking) return null;

  const isLender = user?.userId === booking.lenderId;
  const isRenter = user?.userId === booking.renterId;
  const days = diffDays(booking.startDateUtc, booking.endDateUtc);
  const otherName = isLender ? booking.renterName : booking.lenderName;

  return (
    <div className="min-h-screen bg-[#f9f9f9] font-[Manrope]">
      {/* Header */}
      <header className="bg-[#f9f9f9]/90 backdrop-blur-xl fixed top-0 z-50 w-full border-b border-[#e2e2e2]/50">
        <div className="flex justify-between items-center px-6 py-4 w-full max-w-7xl mx-auto">
          <div className="flex items-center gap-4">
            <button
              onClick={() => navigate(-1)}
              className="flex items-center gap-1 text-[#3e494b] hover:text-[#005f6c] transition-colors bg-transparent border-none cursor-pointer"
            >
              <span className="material-symbols-outlined">arrow_back</span>
            </button>
            <span className="font-[Plus_Jakarta_Sans] font-black text-2xl text-[#005f6c] tracking-tight">Borro</span>
          </div>
          <div className="flex items-center gap-2">
            <button className="p-2 hover:bg-[#f3f3f3] transition-colors rounded-full border-none bg-transparent cursor-pointer">
              <span className="material-symbols-outlined text-[#3e494b]">notifications</span>
            </button>
          </div>
        </div>
      </header>

      <main className="pt-24 pb-12 px-6 max-w-7xl mx-auto">
        {/* Breadcrumb */}
        <div className="mb-6 flex items-center gap-3">
          <button
            onClick={() => navigate(-1)}
            className="flex items-center gap-1 text-[#3e494b] hover:text-[#005f6c] transition-colors text-sm font-medium bg-transparent border-none cursor-pointer"
          >
            <span className="material-symbols-outlined text-base">arrow_back</span>
            My Bookings
          </button>
          <div className="h-4 w-px bg-[#bdc8cb]/50" />
          <span className="text-[#6e797b] text-sm font-medium">
            Booking #{booking.id.slice(0, 8).toUpperCase()}
          </span>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* ── Left Column ───────────────────────── */}
          <div className="lg:col-span-7 space-y-6">

            {/* Status + Title */}
            <section className="flex flex-col gap-3">
              <div className="flex items-center gap-3 flex-wrap">
                <span className={`px-4 py-1.5 rounded-full text-xs font-bold uppercase tracking-wider ${STATUS_COLOR[booking.status]}`}>
                  {STATUS_LABELS[booking.status]}
                </span>
                <span className="text-[#6e797b] text-sm font-medium">
                  {isLender ? `Requested by ${booking.renterName}` : `Hosted by ${booking.lenderName}`}
                </span>
              </div>
              <h1 className="font-[Plus_Jakarta_Sans] text-3xl font-extrabold tracking-tight text-[#1a1c1c]">
                {booking.itemTitle}
              </h1>
            </section>

            {/* Rental Timeline + Cost */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="bg-[#f3f3f3] p-6 rounded-2xl space-y-4">
                <h3 className="font-[Plus_Jakarta_Sans] font-bold text-base flex items-center gap-2">
                  <span className="material-symbols-outlined text-[#005f6c]">calendar_today</span>
                  Rental Period
                </h3>
                <div className="space-y-3 text-sm">
                  <div className="flex justify-between">
                    <span className="text-[#3e494b]">Pickup</span>
                    <span className="font-semibold">{formatDate(booking.startDateUtc)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-[#3e494b]">Return</span>
                    <span className="font-semibold">{formatDate(booking.endDateUtc)}</span>
                  </div>
                  <div className="border-t border-[#bdc8cb]/30 pt-3 flex justify-between text-[#005f6c]">
                    <span className="font-medium">Duration</span>
                    <span className="font-bold">{days} {days === 1 ? 'Day' : 'Days'}</span>
                  </div>
                </div>
              </div>

              <div className="bg-[#f3f3f3] p-6 rounded-2xl space-y-4">
                <h3 className="font-[Plus_Jakarta_Sans] font-bold text-base flex items-center gap-2">
                  <span className="material-symbols-outlined text-[#005f6c]">payments</span>
                  {isLender ? 'Estimated Earnings' : 'Cost Breakdown'}
                </h3>
                <div className="space-y-3 text-sm">
                  <div className="flex justify-between text-[#3e494b]">
                    <span>Rental ({days} {days === 1 ? 'day' : 'days'})</span>
                    <span>${booking.totalPrice.toFixed(2)}</span>
                  </div>
                  {isLender && (
                    <div className="flex justify-between text-[#3e494b]">
                      <span>Borro Service Fee</span>
                      <span className="text-[#a91929]">-${(booking.totalPrice * 0.05).toFixed(2)}</span>
                    </div>
                  )}
                  <div className="border-t border-[#bdc8cb]/30 pt-3 flex justify-between">
                    <span className="font-bold text-base">{isLender ? 'Total Payout' : 'Total Paid'}</span>
                    <span className="font-extrabold text-xl text-[#005f6c]">
                      ${isLender
                        ? (booking.totalPrice * 0.95).toFixed(2)
                        : booking.totalPrice.toFixed(2)}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex flex-wrap gap-3">
              {/* Lender actions */}
              {isLender && booking.status === 'PendingApproval' && (
                <>
                  <button
                    onClick={() => handleTransition('Approved')}
                    disabled={transitioning}
                    className="flex-1 bg-[#005f6c] text-white py-4 px-8 rounded-full font-bold hover:brightness-110 active:scale-95 transition-all disabled:opacity-60 border-none cursor-pointer shadow-lg shadow-[#005f6c]/20"
                  >
                    Approve Request
                  </button>
                  <button
                    onClick={() => handleTransition('Cancelled')}
                    disabled={transitioning}
                    className="flex-1 bg-[#e2e2e2] text-[#1a1c1c] py-4 px-8 rounded-full font-bold hover:bg-[#e8e8e8] active:scale-95 transition-all disabled:opacity-60 border-none cursor-pointer"
                  >
                    Decline Request
                  </button>
                </>
              )}
              {isLender && booking.status === 'Approved' && (
                <button
                  onClick={() => handleTransition('Cancelled')}
                  disabled={transitioning}
                  className="bg-[#ffdad6] text-[#93000a] py-4 px-8 rounded-full font-bold hover:bg-[#ffb3af] active:scale-95 transition-all disabled:opacity-60 border-none cursor-pointer"
                >
                  Cancel Booking
                </button>
              )}
              {isLender && ['Active', 'PaymentHeld', 'Completed'].includes(booking.status) && (
                <button
                  onClick={() => handleTransition('Disputed')}
                  disabled={transitioning || booking.status === 'Disputed'}
                  className="bg-[#ffdad6] text-[#93000a] py-4 px-8 rounded-full font-bold hover:bg-[#ffb3af] active:scale-95 transition-all disabled:opacity-60 border-none cursor-pointer"
                >
                  <span className="material-symbols-outlined text-base align-middle mr-1">flag</span>
                  Raise Dispute
                </button>
              )}

              {/* Renter actions */}
              {isRenter && booking.status === 'Approved' && (
                <button
                  onClick={() => handleTransition('PaymentHeld')}
                  disabled={transitioning}
                  className="flex-1 bg-[#005f6c] text-white py-4 px-8 rounded-full font-bold flex items-center justify-center gap-2 hover:brightness-110 active:scale-95 transition-all disabled:opacity-60 border-none cursor-pointer shadow-lg shadow-[#005f6c]/20"
                >
                  Confirm & Pay
                  <span className="material-symbols-outlined">payments</span>
                </button>
              )}
              {isRenter && booking.status === 'PaymentHeld' && (
                <button
                  onClick={() => handleTransition('Active')}
                  disabled={transitioning}
                  className="flex-1 bg-[#005f6c] text-white py-4 px-8 rounded-full font-bold flex items-center justify-center gap-2 hover:brightness-110 active:scale-95 transition-all disabled:opacity-60 border-none cursor-pointer shadow-lg shadow-[#005f6c]/20"
                >
                  Confirm Pickup
                  <span className="material-symbols-outlined">check_circle</span>
                </button>
              )}
              {isRenter && booking.status === 'Active' && (
                <button
                  onClick={() => handleTransition('Completed')}
                  disabled={transitioning}
                  className="flex-1 bg-[#005f6c] text-white py-4 px-8 rounded-full font-bold flex items-center justify-center gap-2 hover:brightness-110 active:scale-95 transition-all disabled:opacity-60 border-none cursor-pointer shadow-lg shadow-[#005f6c]/20"
                >
                  Confirm Return
                  <span className="material-symbols-outlined">task_alt</span>
                </button>
              )}
              {isRenter && ['PendingApproval', 'Approved'].includes(booking.status) && (
                <button
                  onClick={() => handleTransition('Cancelled')}
                  disabled={transitioning}
                  className="text-[#a91929] hover:bg-[#ffdad6]/50 py-4 px-8 rounded-full font-bold transition-all disabled:opacity-60 border-none bg-transparent cursor-pointer"
                >
                  Cancel Booking
                </button>
              )}
              {isRenter && ['Active', 'PaymentHeld', 'Completed'].includes(booking.status) && (
                <button
                  onClick={() => handleTransition('Disputed')}
                  disabled={transitioning || booking.status === 'Disputed'}
                  className="bg-[#ffdad6] text-[#93000a] py-4 px-8 rounded-full font-bold hover:bg-[#ffb3af] active:scale-95 transition-all disabled:opacity-60 border-none cursor-pointer"
                >
                  <span className="material-symbols-outlined text-base align-middle mr-1">flag</span>
                  Raise Dispute
                </button>
              )}
            </div>
          </div>

          {/* ── Right Column: Sticky Chat ──────────── */}
          <div className="lg:col-span-5 lg:sticky lg:top-24">
            <div className="bg-white rounded-3xl flex flex-col shadow-[0px_20px_40px_rgba(26,28,28,0.06)] border border-[#e2e2e2]/50 overflow-hidden" style={{ height: 'calc(100vh - 140px)' }}>

              {/* Chat Header */}
              <div className="p-5 flex items-center justify-between border-b border-[#e2e2e2]/60">
                <div className="flex items-center gap-3">
                  <div className="relative">
                    <div className="w-10 h-10 rounded-full bg-[#005f6c] flex items-center justify-center text-white font-bold text-sm">
                      {otherName.charAt(0).toUpperCase()}
                    </div>
                    {chatActive && connected && (
                      <div className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full" />
                    )}
                  </div>
                  <div>
                    <h4 className="font-[Plus_Jakarta_Sans] font-bold text-base leading-tight">{otherName}</h4>
                    <p className="text-xs text-[#6e797b]">
                      {chatActive
                        ? connected ? 'Connected' : 'Connecting...'
                        : 'Chat available after approval'}
                    </p>
                  </div>
                </div>
                <span className="material-symbols-outlined text-[#3e494b]">chat_bubble</span>
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
                    const isMine = msg.senderId === user?.userId;
                    return (
                      <div
                        key={msg.id}
                        className={`flex gap-2 max-w-[85%] ${isMine ? 'ml-auto flex-row-reverse' : ''}`}
                      >
                        {!isMine && (
                          <div className="w-7 h-7 rounded-full bg-[#d5e0f7] flex items-center justify-center text-[#3c475a] text-xs font-bold flex-shrink-0 mt-auto">
                            {msg.senderName.charAt(0).toUpperCase()}
                          </div>
                        )}
                        <div
                          className={`p-3 text-sm leading-relaxed ${
                            isMine
                              ? 'bg-[#005f6c] text-white rounded-t-2xl rounded-bl-2xl'
                              : 'bg-[#f3f3f3] text-[#1a1c1c] rounded-t-2xl rounded-br-2xl'
                          }`}
                        >
                          <p>{msg.content}</p>
                          <span className={`text-[10px] block mt-1 ${isMine ? 'text-white/60 text-right' : 'text-[#6e797b]'}`}>
                            {new Date(msg.createdAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          </span>
                        </div>
                      </div>
                    );
                  })
                )}
                <div ref={messagesEndRef} />
              </div>

              {/* Input */}
              <div className="p-4 pt-2 border-t border-[#e2e2e2]/60">
                {chatActive ? (
                  <form onSubmit={handleSend}>
                    <div className="flex items-center gap-2 bg-[#f3f3f3] rounded-full p-2">
                      <input
                        type="text"
                        value={messageInput}
                        onChange={e => setMessageInput(e.target.value)}
                        placeholder={connected ? 'Type a message...' : 'Connecting...'}
                        disabled={!connected}
                        className="flex-1 bg-transparent border-none focus:outline-none text-sm px-2 placeholder:text-[#6e797b] disabled:opacity-50"
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
          </div>
        </div>
      </main>
    </div>
  );
}
