import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  computed,
  inject,
  input,
  model,
} from '@angular/core';
import { DateAdapter } from '@angular/material/core';
import {
  MatCalendarCellClassFunction,
  MatDatepicker,
  MatDatepickerInput,
} from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { resolveIcons } from '../../icons/icons';

/**
 * Dates are written by the app's date adapter, whose locale and formats the app sets (e.g.
 * `provideNativeDateAdapter`). The arrow icons are the same for both dates.
 */
@Component({
  selector: 'app-datepicker',
  imports: [MatDatepicker, MatDatepickerInput, MatIconModule],
  templateUrl: './datepicker.html',
  styleUrl: './datepicker.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The calendars open in an overlay outside the component: their styles must be global.
  encapsulation: ViewEncapsulation.None,
})
export class Datepicker {
  readonly start = model.required<Date>();
  readonly end = model.required<Date>();
  /** Icons as Material icon names (Material Symbols font): [calendar, previous day, next day]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [calendar, previous day, next day]. */
  readonly pathIcon = input<readonly string[] | null>();

  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));

  protected readonly rangeClass: MatCalendarCellClassFunction<Date> = (date, view) =>
    view === 'month' &&
    this.dateAdapter.compareDate(date, this.start()) >= 0 &&
    this.dateAdapter.compareDate(date, this.end()) <= 0
      ? 'datepicker__calendar__range'
      : '';

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
