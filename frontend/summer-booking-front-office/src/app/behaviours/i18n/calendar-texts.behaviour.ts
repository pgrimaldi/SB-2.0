import { Injectable, OnDestroy, inject } from '@angular/core';
import { MatDatepickerIntl } from '@angular/material/datepicker';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subscription } from 'rxjs';

interface CalendarTexts {
  label: string;
  open: string;
  close: string;
  previous_month: string;
  next_month: string;
  previous_year: string;
  next_year: string;
  previous_years: string;
  next_years: string;
  choose_date: string;
  choose_month_year: string;
  comparison_range: string;
}

/**
 * Translates the commands of Material's calendar read by screen readers ("Previous month"…), in
 * every language and on every language change: `DateAdapter` translates dates, not these commands.
 * Not started with the app: importing `MatDatepickerIntl` brings the whole calendar, which pages
 * without calendars (the home) must not download. Every page or component that shows a calendar
 * starts it with `inject(CalendarTextsBehaviour)` in its constructor (e.g. the management header);
 * it is one for the whole app, so starting it again does nothing more.
 */
@Injectable({ providedIn: 'root' })
export class CalendarTextsBehaviour implements OnDestroy {
  private readonly datepickerIntl = inject(MatDatepickerIntl);
  private readonly translate = inject(TranslateService);
  private readonly calendarTextsSubscription: Subscription;

  constructor() {
    // Notify even an open calendar.
    this.calendarTextsSubscription = (
      this.translate.stream('calendar') as Observable<CalendarTexts>
    ).subscribe((texts) => {
      if (!texts || typeof texts !== 'object') {
        return; // No translation group loaded yet: keep Material's defaults until it arrives.
      }
      this.datepickerIntl.calendarLabel = texts.label;
      this.datepickerIntl.openCalendarLabel = texts.open;
      this.datepickerIntl.closeCalendarLabel = texts.close;
      this.datepickerIntl.prevMonthLabel = texts.previous_month;
      this.datepickerIntl.nextMonthLabel = texts.next_month;
      this.datepickerIntl.prevYearLabel = texts.previous_year;
      this.datepickerIntl.nextYearLabel = texts.next_year;
      this.datepickerIntl.prevMultiYearLabel = texts.previous_years;
      this.datepickerIntl.nextMultiYearLabel = texts.next_years;
      this.datepickerIntl.switchToMonthViewLabel = texts.choose_date;
      this.datepickerIntl.switchToMultiYearViewLabel = texts.choose_month_year;
      this.datepickerIntl.comparisonDateLabel = texts.comparison_range;
      this.datepickerIntl.formatYearRangeLabel = (start, end) =>
        this.translate.instant('calendar.year_range', { start, end }) as string;
      this.datepickerIntl.changes.next();
    });
  }

  ngOnDestroy(): void {
    this.calendarTextsSubscription.unsubscribe();
  }
}
