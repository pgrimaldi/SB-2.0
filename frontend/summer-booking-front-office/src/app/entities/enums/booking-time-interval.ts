export enum BookingTimeInterval {
  FifteenMinutes = 'FifteenMinutes',
  ThirtyMinutes = 'ThirtyMinutes',
  FortyFiveMinutes = 'FortyFiveMinutes',
  OneHour = 'OneHour',
  TwoHours = 'TwoHours',
}

export const BOOKING_TIME_INTERVAL_MINUTES: Readonly<Record<BookingTimeInterval, number>> = {
  [BookingTimeInterval.FifteenMinutes]: 15,
  [BookingTimeInterval.ThirtyMinutes]: 30,
  [BookingTimeInterval.FortyFiveMinutes]: 45,
  [BookingTimeInterval.OneHour]: 60,
  [BookingTimeInterval.TwoHours]: 120,
};
