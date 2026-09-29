import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { startOfToday } from 'date-fns';
import { debounceTime } from 'rxjs';
import { BookingDayType } from '../../entities/enums/booking-day-type';
import { ManagementRequest } from '../../entities/management/management-request';
import { AuthBehaviour } from '../auth/auth.behaviour';

/** Pause after the last date change before the pages ask the API (arrows can be clicked quickly). */
export const DATES_DEBOUNCE = 500;

interface Dates {
  start: Date;
  end: Date;
}

/**
 * Filters chosen in the management header and read by the management pages.
 * Provided by the management layout: it lives, and starts again from today, with it.
 */
@Injectable()
export class ManagementFiltersBehaviour {
  private readonly auth = inject(AuthBehaviour);

  /** Days the management pages refer to (calendar days); today by default. */
  readonly startDate = signal(startOfToday());
  readonly endDate = signal(startOfToday());
  /** Part of the day the management pages refer to; full day by default. */
  readonly period = signal(BookingDayType.FullDay);
  /** Text searched in the page's table (empty: no filter). */
  readonly search = signal('');

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

/** The chosen calendar day as a UTC day, without time zone conversion: 29/09 → `2026-09-29T00:00:00.000Z`. */
function utcDayStart(day: Date): string {
  return new Date(Date.UTC(day.getFullYear(), day.getMonth(), day.getDate())).toISOString();
}

/** Last instant of the chosen calendar day as a UTC day: 29/09 → `2026-09-29T23:59:59.999Z`. */
function utcDayEnd(day: Date): string {
  return new Date(
    Date.UTC(day.getFullYear(), day.getMonth(), day.getDate(), 23, 59, 59, 999),
  ).toISOString();
}
