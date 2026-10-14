import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { DateAdapter } from '@angular/material/core';
import { debounceTime } from 'rxjs';
import { BookingDayType } from '../../entities/enums/booking-day-type';
import { PageFilter } from '../../entities/pagination/page';
import { DateRange } from '../../entities/shared/date-range';
import { utcDayEnd, utcDayStart } from '../dates/date-periods';

/** The date arrows can be clicked quickly: the pages ask the API only after this pause. */
export const DATES_DEBOUNCE = 500;

/** Provided by the management layout: it lives, and starts again from today, with it. */
@Injectable()
export class ManagementFiltersBehaviour {
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);

  readonly startDate = signal(this.dateAdapter.today());
  readonly endDate = signal(this.dateAdapter.today());
  readonly period = signal(BookingDayType.FullDay);

  /** The dates the user stopped on: they follow the header after `DATES_DEBOUNCE` ms of quiet. */
  private readonly settledDates = toSignal(
    toObservable(
      computed<DateRange>(() => ({ start: this.startDate(), end: this.endDate() })),
    ).pipe(debounceTime(DATES_DEBOUNCE)),
    {
      initialValue: { start: this.startDate(), end: this.endDate() },
      equal: (a, b) =>
        a.start.getTime() === b.start.getTime() && a.end.getTime() === b.end.getTime(),
    },
  );

  /**
   * The header choices as table filters (`[filters]` of `app-table`); they change when the dates
   * settle or the part of the day changes.
   */
  readonly filters = computed<PageFilter[]>(() => {
    const { start, end } = this.settledDates();
    return [
      { field: 'datetimeFrom', value: [utcDayStart(start)] },
      { field: 'datetimeTo', value: [utcDayEnd(end)] },
      { field: 'bookingDayType', value: [this.period()] },
    ];
  });
}
