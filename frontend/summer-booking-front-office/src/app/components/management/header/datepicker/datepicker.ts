import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  effect,
  inject,
  model,
} from '@angular/core';
import { DateAdapter } from '@angular/material/core';
import { MatCalendarCellClassFunction, MatDatepickerModule } from '@angular/material/datepicker';
import { TranslatePipe } from '@ngx-translate/core';
import { DateLanguageBehaviour } from '../../../../behaviours/i18n/date-language.behaviour';

/**
 * Start and end date of the management header (Angular Material datepickers, native date adapter):
 * `<app-datepicker [(start)]="startDate" [(end)]="endDate" />`.
 * Dates are chosen with the calendar or the arrows (one day), never typed; they are written as the
 * current language writes them. The start never goes after the end (the other date follows).
 */
@Component({
  selector: 'app-datepicker',
  imports: [MatDatepickerModule, TranslatePipe],
  templateUrl: './datepicker.html',
  styleUrl: './datepicker.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The calendars open in an overlay outside the component: their styles must be global.
  encapsulation: ViewEncapsulation.None,
})
export class Datepicker {
  readonly start = model.required<Date>();
  readonly end = model.required<Date>();

  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);

  /** Highlights the chosen interval in both calendars. */
  protected readonly rangeClass: MatCalendarCellClassFunction<Date> = (date, view) =>
    view === 'month' &&
    this.dateAdapter.compareDate(date, this.start()) >= 0 &&
    this.dateAdapter.compareDate(date, this.end()) <= 0
      ? 'datepicker__calendar__range'
      : '';

  constructor() {
    const dateLanguage = inject(DateLanguageBehaviour);
    effect(() => this.dateAdapter.setLocale(dateLanguage.locale()));
  }

  protected setStart(date: Date | null): void {
    if (!date) {
      return;
    }
    this.start.set(date);
    if (this.dateAdapter.compareDate(date, this.end()) > 0) {
      this.end.set(date);
    }
  }

  protected setEnd(date: Date | null): void {
    if (!date) {
      return;
    }
    this.end.set(date);
    if (this.dateAdapter.compareDate(date, this.start()) < 0) {
      this.start.set(date);
    }
  }

  protected moveStart(days: number): void {
    this.setStart(this.dateAdapter.addCalendarDays(this.start(), days));
  }

  protected moveEnd(days: number): void {
    this.setEnd(this.dateAdapter.addCalendarDays(this.end(), days));
  }
}
