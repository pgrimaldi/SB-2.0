import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { BookingDayType } from '../../entities/enums/booking-day-type';
import { DATES_DEBOUNCE, ManagementFiltersBehaviour } from './management-filters.behaviour';

describe('ManagementFiltersBehaviour', () => {
  beforeEach(() => vi.useFakeTimers());

  const create = () => {
    TestBed.configureTestingModule({
      providers: [ManagementFiltersBehaviour, provideNativeDateAdapter()],
    });
    return TestBed.inject(ManagementFiltersBehaviour);
  };

  it('should give the chosen days as UTC days (no time zone shift) and part of the day as filters', () => {
    const filters = create();
    filters.filters(); // the page starts reading it at once

    filters.startDate.set(new Date(2026, 6, 1, 15, 30));
    filters.endDate.set(new Date(2026, 6, 3));
    filters.period.set(BookingDayType.Morning);
    TestBed.tick();
    vi.advanceTimersByTime(DATES_DEBOUNCE);

    expect(filters.filters()).toEqual([
      { field: 'datetimeFrom', value: '2026-07-01T00:00:00.000Z' },
      { field: 'datetimeTo', value: '2026-07-03T23:59:59.999Z' },
      { field: 'bookingDayType', value: BookingDayType.Morning },
    ]);
  });

  it('should follow the dates only once they settle, the part of the day at once', () => {
    const filters = create();
    const first = filters.filters();
    const end = new Date(filters.startDate().getFullYear() + 1, 0, 15);
    const value = (field: string) =>
      filters.filters().find((filter) => filter.field === field)?.value;

    filters.endDate.set(end);
    TestBed.tick();
    vi.advanceTimersByTime(DATES_DEBOUNCE - 1);
    expect(filters.filters()).toBe(first); // still moving: nothing asked yet

    vi.advanceTimersByTime(1);
    expect(value('datetimeTo')).toBe(`${end.getFullYear()}-01-15T23:59:59.999Z`);

    filters.period.set(BookingDayType.Afternoon);
    expect(value('bookingDayType')).toBe(BookingDayType.Afternoon);
  });
});
