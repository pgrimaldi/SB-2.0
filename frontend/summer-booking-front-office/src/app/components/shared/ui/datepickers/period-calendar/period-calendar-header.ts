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

/** The icons of the calendar: [previous month, next month, remove a period]. */
export const PERIOD_CALENDAR_ICONS = new InjectionToken<Signal<Icons>>('PERIOD_CALENDAR_ICONS');

/** Month and year in the middle, an arrow on each side. */
@Component({
  selector: 'app-period-calendar-header',
  imports: [MatIconModule],
  template: `
    <div class="period__calendar__header">
      <button class="period__calendar__month" type="button" (click)="move(-1)">
        @if (icons().list[0]; as icon) {
          @if (icons().material) {
            <mat-icon>{{ icon }}</mat-icon>
          } @else {
            <img [src]="icon" width="6" height="10" alt="" />
          }
        }
      </button>
      <span class="period__calendar__period">{{ period() }}</span>
      <button class="period__calendar__month" type="button" (click)="move(1)">
        @if (icons().list[1]; as icon) {
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
export class PeriodCalendarHeader implements OnDestroy {
  private readonly calendar = inject<MatCalendar<Date>>(MatCalendar);
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
  private readonly changeDetector = inject(ChangeDetectorRef);

  protected readonly icons = inject(PERIOD_CALENDAR_ICONS);
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
