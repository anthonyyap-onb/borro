import apiClient from '../../lib/apiClient';

export type BookingStatus =
  | 'PendingApproval' | 'Approved' | 'PaymentHeld'
  | 'Active' | 'Completed' | 'Disputed' | 'Cancelled';

export interface BookingDto {
  id: string;
  itemId: string;
  itemTitle: string;
  itemImageUrl: string | null;
  dailyPrice: number;
  renterId: string;
  renterName: string;
  lenderId: string;
  lenderName: string;
  startDateUtc: string;
  endDateUtc: string;
  totalPrice: number;
  status: BookingStatus;
  createdAtUtc: string;
}

export interface MessageDto {
  id: string;
  bookingId: string;
  senderId: string;
  senderName: string;
  content: string;
  createdAtUtc: string;
}

export const bookingApi = {
  create: (itemId: string, startDateUtc: string, endDateUtc: string) =>
    apiClient.post<BookingDto>('/api/bookings', { itemId, startDateUtc, endDateUtc }),

  list: () => apiClient.get<BookingDto[]>('/api/bookings'),

  getById: (id: string) => apiClient.get<BookingDto>(`/api/bookings/${id}`),

  transition: (id: string, status: BookingStatus) =>
    apiClient.patch<BookingDto>(`/api/bookings/${id}/status`, { status }),

  getMessages: (bookingId: string) =>
    apiClient.get<MessageDto[]>(`/api/bookings/${bookingId}/messages`),
};
