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
  AwaitingPayment = 'AwaitingPayment',
  FullyPaid = 'FullyPaid',
  PartiallyPaid = 'PartiallyPaid',
}

export class ExistingBookingFilters {
  bookingTypologies: BookingTypologyFilter[] = [];
  agreementBookings: AgreementBookingFilter[] = [];
  idAffiliatedHotels: string[] = [];
  paymentStatuses: PaymentStatusFilter[] = [];
}
