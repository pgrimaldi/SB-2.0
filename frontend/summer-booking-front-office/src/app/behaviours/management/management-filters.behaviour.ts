import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { DateAdapter } from '@angular/material/core';
import { debounceTime } from 'rxjs';
import { BookingDayType } from '../../entities/enums/booking-day-type';
import { ManagementRequest } from '../../entities/management/management-request';
import { AuthBehaviour } from '../auth/auth.behaviour';

/** The date arrows can be clicked quickly: the pages ask the API only after this pause. */
export const DATES_DEBOUNCE = 500;

interface Dates {
  start: Date;
  end: Date;
}

/** Provided by the management layout: it lives, and starts again from today, with it. */
@Injectable()
export class ManagementFiltersBehaviour {
  private readonly auth = inject(AuthBehaviour);
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);

  readonly startDate = signal(this.dateAdapter.today());
  readonly endDate = signal(this.dateAdapter.today());
  readonly period = signal(BookingDayType.FullDay);

  /** The dates the user stopped on: they follow the header after `DATES_DEBOUNCE` ms of quiet. */
  private readonly settledDates = toSignal(
    toObservable(computed<Dates>(() => ({ start: this.startDate(), end: this.endDate() }))).pipe(
      debounceTime(DATES_DEBOUNCE),
    ),
    {
      initialValue: { start: this.startDate(), end: this.endDate() },
      equal: (a, b) =>
        a.start.getTime() === b.start.getTime() && a.end.getTime() === b.end.getTime(),
    },
  );

  /**
   * What every management API request sends; it changes (and the pages ask the API again) when the
   * dates settle or the part of the day changes. Null once the user has signed out.
   */
  readonly request = computed<ManagementRequest | null>(() => {
    const user = this.auth.user();
    const { start, end } = this.settledDates();
    return user
      ? {
          idProperty: user.idProperty,
          datetimeFrom: utcDayStart(start),
          datetimeTo: utcDayEnd(end),
          bookingDayType: this.period(),
        }
      : null;
  });
}

/** The calendar day as a UTC day, no time zone conversion: 29/09 → `2026-09-29T00:00:00.000Z`. */
function utcDayStart(day: Date): string {
  return new Date(Date.UTC(day.getFullYear(), day.getMonth(), day.getDate())).toISOString();
}

function utcDayEnd(day: Date): string {
  return new Date(
    Date.UTC(day.getFullYear(), day.getMonth(), day.getDate(), 23, 59, 59, 999),
  ).toISOString();
}
