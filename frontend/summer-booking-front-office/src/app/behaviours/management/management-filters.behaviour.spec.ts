import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { BookingDayType } from '../../entities/enums/booking-day-type';
import { AuthBehaviour } from '../auth/auth.behaviour';
import { DATES_DEBOUNCE, ManagementFiltersBehaviour } from './management-filters.behaviour';

describe('ManagementFiltersBehaviour', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => {
    vi.useRealTimers();
    sessionStorage.clear();
    localStorage.clear();
  });

  const signedIn = () => {
    TestBed.configureTestingModule({
      providers: [ManagementFiltersBehaviour, provideNativeDateAdapter()],
    });
    TestBed.inject(AuthBehaviour).start(
      {
        accessToken: 'token',
        expiresIn: 900,
        user: { email: 'u@e.it', idProperty: 'property-1', roles: [] },
      },
      false,
    );
    return TestBed.inject(ManagementFiltersBehaviour);
  };

  it('should send property, the chosen days as UTC days (no time zone shift) and part of the day', () => {
    const filters = signedIn();
    filters.request(); // the page starts reading it at once

    filters.startDate.set(new Date(2026, 6, 1, 15, 30));
    filters.endDate.set(new Date(2026, 6, 3));
    filters.period.set(BookingDayType.Morning);
    TestBed.tick();
    vi.advanceTimersByTime(DATES_DEBOUNCE);

    expect(filters.request()).toEqual({
      idProperty: 'property-1',
      datetimeFrom: '2026-07-01T00:00:00.000Z',
      datetimeTo: '2026-07-03T23:59:59.999Z',
      bookingDayType: BookingDayType.Morning,
    });
  });

  it('should follow the dates only once they settle, the part of the day at once', () => {
    const filters = signedIn();
    const first = filters.request();
    const end = new Date(filters.startDate().getFullYear() + 1, 0, 15);

    filters.endDate.set(end);
    TestBed.tick();
    vi.advanceTimersByTime(DATES_DEBOUNCE - 1);
    expect(filters.request()).toBe(first); // still moving: nothing asked yet

    vi.advanceTimersByTime(1);
    expect(filters.request()?.datetimeTo).toBe(`${end.getFullYear()}-01-15T23:59:59.999Z`);

    filters.period.set(BookingDayType.Afternoon);
    expect(filters.request()?.bookingDayType).toBe(BookingDayType.Afternoon);
  });

  it('should send nothing once the user has signed out', () => {
    TestBed.configureTestingModule({
      providers: [ManagementFiltersBehaviour, provideNativeDateAdapter()],
    });

    expect(TestBed.inject(ManagementFiltersBehaviour).request()).toBeNull();
  });
});
