export type BookingStatus = 'pending' | 'confirmed' | 'cancelled';

export interface Booking {
  id: string;
  customerName: string;
  bookingDate: string;
  status: BookingStatus;
}
