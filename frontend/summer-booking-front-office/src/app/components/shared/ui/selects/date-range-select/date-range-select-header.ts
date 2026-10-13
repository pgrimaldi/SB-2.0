import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  InjectionToken,
  OnDestroy,
  Signal,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DateAdapter } from '@angular/material/core';
import { MatCalendar } from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { Subscription } from 'rxjs';
import { Icons } from '../../icons/icons';

/** The icons of the select: [open, previous month, next month]. */
export const DATE_RANGE_SELECT_ICONS = new InjectionToken<Signal<Icons>>('DATE_RANGE_SELECT_ICONS');

/** Month and year in the middle, an arrow on each side. */
@Component({
  selector: 'app-date-range-select-header',
  imports: [MatIconModule],
  template: `
    <div class="date__range__select__header">
      <button class="date__range__select__month" type="button" (click)="move(-1)">
        @if (icons().list[1]; as icon) {
          @if (icons().material) {
            <mat-icon>{{ icon }}</mat-icon>
          } @else {
            <img [src]="icon" width="6" height="10" alt="" />
          }
        }
      </button>
      <span class="date__range__select__period">{{ period() }}</span>
      <button class="date__range__select__month" type="button" (click)="move(1)">
        @if (icons().list[2]; as icon) {
          @if (icons().material) {
            <mat-icon>{{ icon }}</mat-icon>
          } @else {
            <img [src]="icon" width="6" height="10" alt="" />
          }
        }
      </button>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DateRangeSelectHeader implements OnDestroy {
  private readonly calendar = inject<MatCalendar<Date>>(MatCalendar);
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
  private readonly changeDetector = inject(ChangeDetectorRef);

  protected readonly icons = inject(DATE_RANGE_SELECT_ICONS);
  private readonly activeDate = signal(this.calendar.activeDate);
  protected readonly period = computed(() =>
    this.dateAdapter.format(this.activeDate(), { month: 'long', year: 'numeric' }),
  );
  private readonly stateChanges: Subscription;

  constructor() {
    // Material changes the month from outside too (e.g. with the keyboard).
    this.stateChanges = this.calendar.stateChanges.subscribe(() => {
      this.activeDate.set(this.calendar.activeDate);
      this.changeDetector.markForCheck();
    });
  }

  protected move(months: number): void {
    this.calendar.activeDate = this.dateAdapter.addCalendarMonths(this.calendar.activeDate, months);
  }

  ngOnDestroy(): void {
    this.stateChanges.unsubscribe();
  }
}
