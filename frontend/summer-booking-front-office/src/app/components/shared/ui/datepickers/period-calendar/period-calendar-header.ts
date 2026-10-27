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
import { MatCalendar, MatCalendarView, yearsPerPage } from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { Subscription } from 'rxjs';
import { Icons } from '../../icons/icons';

/** The icons of the calendar: [previous month, next month, remove a period]. */
export const PERIOD_CALENDAR_ICONS = new InjectionToken<Signal<Icons>>('PERIOD_CALENDAR_ICONS');

/**
 * Month and year in the middle, an arrow on each side. As in Material's own header, a click on the
 * month and year shows the years, then the months of the chosen year, then its days; the arrows move
 * by a month, a year or a page of years, never past `minDate` / `maxDate` of the calendar.
 */
@Component({
  selector: 'app-period-calendar-header',
  imports: [MatIconModule],
  template: `
    <div class="period__calendar__header">
      <button
        class="period__calendar__month"
        type="button"
        [disabled]="!canMove(-1)"
        (click)="move(-1)"
      >
        @if (icons().list[0]; as icon) {
          @if (icons().material) {
            <mat-icon>{{ icon }}</mat-icon>
          } @else {
            <img [src]="icon" width="6" height="10" alt="" />
          }
        }
      </button>
      <button class="period__calendar__period" type="button" (click)="switchView()">
        {{ period() }}
      </button>
      <button
        class="period__calendar__month"
        type="button"
        [disabled]="!canMove(1)"
        (click)="move(1)"
      >
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
  styleUrl: './period-calendar-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PeriodCalendarHeader implements OnDestroy {
  private readonly calendar = inject<MatCalendar<Date>>(MatCalendar);
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
  private readonly changeDetector = inject(ChangeDetectorRef);

  protected readonly icons = inject(PERIOD_CALENDAR_ICONS);
  private readonly activeDate = signal(this.calendar.activeDate);
  private readonly view = signal<MatCalendarView>(this.calendar.currentView);
  protected readonly period = computed(() => {
    const date = this.activeDate();
    switch (this.view()) {
      case 'month':
        return this.dateAdapter.format(date, { month: 'long', year: 'numeric' });
      case 'year':
        return this.dateAdapter.getYearName(date);
      default: {
        const first = this.firstYearOfPage(date);
        return `${first} – ${first + yearsPerPage - 1}`;
      }
    }
  });
  private readonly subscriptions = new Subscription();

  constructor() {
    // Material changes the month and the view from outside too (e.g. with the keyboard, or a click
    // on a year).
    this.subscriptions.add(
      this.calendar.stateChanges.subscribe(() => {
        this.activeDate.set(this.calendar.activeDate);
        this.view.set(this.calendar.currentView);
        this.changeDetector.markForCheck();
      }),
    );
    this.subscriptions.add(
      this.calendar.viewChanged.subscribe((view) => {
        this.view.set(view);
        this.changeDetector.markForCheck();
      }),
    );
  }

  /** Not to a month, year or page of years without days that can be chosen. */
  protected canMove(direction: number): boolean {
    const limit = direction < 0 ? this.calendar.minDate : this.calendar.maxDate;
    return !limit || !this.isSameView(this.activeDate(), limit);
  }

  protected move(direction: number): void {
    const date = this.calendar.activeDate;
    this.calendar.activeDate =
      this.view() === 'month'
        ? this.dateAdapter.addCalendarMonths(date, direction)
        : this.dateAdapter.addCalendarYears(
            date,
            this.view() === 'year' ? direction : direction * yearsPerPage,
          );
  }

  protected switchView(): void {
    this.calendar.currentView = this.calendar.currentView === 'month' ? 'multi-year' : 'month';
  }

  private isSameView(first: Date, second: Date): boolean {
    const firstYear = this.dateAdapter.getYear(first);
    const secondYear = this.dateAdapter.getYear(second);
    switch (this.view()) {
      case 'month':
        return (
          firstYear === secondYear &&
          this.dateAdapter.getMonth(first) === this.dateAdapter.getMonth(second)
        );
      case 'year':
        return firstYear === secondYear;
      default:
        return this.firstYearOfPage(first) === this.firstYearOfPage(second);
    }
  }

  /** The pages of years are laid out as Material's year view does, from the limits of the calendar. */
  private firstYearOfPage(date: Date): number {
    const { minDate, maxDate } = this.calendar;
    const start = maxDate
      ? this.dateAdapter.getYear(maxDate) - yearsPerPage + 1
      : minDate
        ? this.dateAdapter.getYear(minDate)
        : 0;
    const year = this.dateAdapter.getYear(date);
    return year - ((((year - start) % yearsPerPage) + yearsPerPage) % yearsPerPage);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
