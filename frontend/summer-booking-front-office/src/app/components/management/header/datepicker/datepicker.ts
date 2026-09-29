import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  effect,
  inject,
  model,
} from '@angular/core';
import { DateAdapter } from '@angular/material/core';
import { provideDateFnsAdapter } from '@angular/material-date-fns-adapter';
import { MatCalendarCellClassFunction, MatDatepickerModule } from '@angular/material/datepicker';
import { TranslatePipe } from '@ngx-translate/core';
import { addDays, isAfter, isBefore, isWithinInterval } from 'date-fns';
import { enGB, it } from 'date-fns/locale';
import { LanguageBehaviour } from '../../../../behaviours/i18n/language.behaviour';

/** Dates shown and typed as dd/mm/yyyy in every language, like the reference. */
const DATE_FORMATS = {
  parse: { dateInput: ['dd/MM/yyyy', 'd/M/yyyy'] },
  display: {
    dateInput: 'dd/MM/yyyy',
    monthYearLabel: 'LLL yyyy',
    dateA11yLabel: 'PPP',
    monthYearA11yLabel: 'LLLL yyyy',
  },
};

/**
 * Start and end date of the management header (Angular Material datepickers):
 * `<app-datepicker [(start)]="startDate" [(end)]="endDate" />`.
 * Arrows move a date by one day; the start never goes after the end (the other date follows).
 */
@Component({
  selector: 'app-datepicker',
  imports: [MatDatepickerModule, TranslatePipe],
  providers: [provideDateFnsAdapter(DATE_FORMATS)],
  templateUrl: './datepicker.html',
  styleUrl: './datepicker.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The calendars open in an overlay outside the component: their styles must be global.
  encapsulation: ViewEncapsulation.None,
})
export class Datepicker {
  readonly start = model.required<Date>();
  readonly end = model.required<Date>();

  /** Highlights the chosen interval in both calendars. */
  protected readonly rangeClass: MatCalendarCellClassFunction<Date> = (date, view) =>
    view === 'month' && isWithinInterval(date, { start: this.start(), end: this.end() })
      ? 'datepicker__calendar__range'
      : '';

  constructor() {
    const dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
    const languageBehaviour = inject(LanguageBehaviour);
    effect(() => dateAdapter.setLocale(languageBehaviour.current() === 'en' ? enGB : it));
  }

  /** A typed text that is not a date (null) brings the field back to the current date. */
  protected setStart(date: Date | null): void {
    const start = date ?? new Date(this.start());
    this.start.set(start);
    if (isAfter(start, this.end())) {
      this.end.set(start);
    }
  }

  protected setEnd(date: Date | null): void {
    const end = date ?? new Date(this.end());
    this.end.set(end);
    if (isBefore(end, this.start())) {
      this.start.set(end);
    }
  }

  protected moveStart(days: number): void {
    this.setStart(addDays(this.start(), days));
  }

  protected moveEnd(days: number): void {
    this.setEnd(addDays(this.end(), days));
  }
}
