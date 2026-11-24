import { ComboboxItem } from '../combobox/combobox-item';

export enum BookingTypologyFilter {
  Daily = 'Daily',
  MultiDay = 'MultiDay',
  SevenDays = 'SevenDays',
  FourteenDays = 'FourteenDays',
  Monthly = 'Monthly',
  Seasonal = 'Seasonal',
}

export enum AgreementBookingFilter {
  Yes = 'Yes',
  No = 'No',
}

export enum PaymentStatusFilter {
  Unpaid = 'unpaid',
  Paid = 'paid',
  Partial = 'partial',
}

export class ExistingBookingFilters {
  bookingTypologies: BookingTypologyFilter[] = [];
  agreementBookings: AgreementBookingFilter[] = [];
  affiliatedHotels: ComboboxItem[] = [];
  paymentStatuses: PaymentStatusFilter[] = [];
}
