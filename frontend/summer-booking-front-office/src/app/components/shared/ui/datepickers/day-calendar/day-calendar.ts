import {
  ChangeDetectionStrategy,
  Component,
  ViewEncapsulation,
  afterRenderEffect,
  computed,
  inject,
  input,
  model,
  output,
  untracked,
  viewChild,
} from '@angular/core';
import { MatCalendar, MatCalendarCellClassFunction } from '@angular/material/datepicker';
import { resolveIcons } from '../../icons/icons';
import {
  PERIOD_CALENDAR_ICONS,
  PeriodCalendarHeader,
} from '../period-calendar/period-calendar-header';

export interface DayCalendarTexts {
  hint?: string;
}

/**
 * A calendar always open where several days are chosen: a click on a day chooses it, another click
 * removes it; the chosen days stay coloured.
 */
@Component({
  selector: 'app-day-calendar',
  imports: [MatCalendar],
  templateUrl: './day-calendar.html',
  styleUrl: './day-calendar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: PERIOD_CALENDAR_ICONS, useFactory: () => inject(DayCalendar).icons }],
  // The days are drawn by Material's calendar: their classes need global styles.
  encapsulation: ViewEncapsulation.None,
})
export class DayCalendar {
  /** The chosen days, at midnight UTC as ISO strings, in order. */
  readonly value = model<string[]>([]);
  readonly readonly = input(false);
  readonly disabled = input(false);
  /** Days that cannot be chosen, greyed (e.g. chosen already), at midnight UTC as ISO strings. */
  readonly disabledDates = input<readonly string[]>([]);
  readonly texts = input<DayCalendarTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [previous month, next month]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: same order. */
  readonly pathIcon = input<readonly string[] | null>();
  /** A day was chosen or removed: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  protected readonly Header = PeriodCalendarHeader;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly isLocked = computed(() => this.readonly() || this.disabled());
  private readonly chosenDays = computed(() => new Set(this.value().map(dayOf)));
  private readonly disabledDays = computed(() => new Set(this.disabledDates().map(dayOf)));
  protected readonly dateFilter = computed(() => {
    const disabled = this.disabledDays();
    return (date: Date): boolean => !disabled.has(dayOf(utcDay(date)));
  });
  protected readonly dateClass = computed<MatCalendarCellClassFunction<Date>>(() => {
    const chosen = this.chosenDays();
    return (date, view) => ({
      day__calendar__chosen: view === 'month' && chosen.has(dayOf(utcDay(date))),
    });
  });
  private readonly calendar = viewChild(MatCalendar);

  constructor() {
    // Material reads the classes of the days only when it draws the month: it draws it again once
    // the new classes have reached it.
    afterRenderEffect(() => {
      this.dateClass();
      untracked(() => this.calendar()?.updateTodaysDate());
    });
  }

  protected pick(date: Date | null): void {
    if (!date || this.isLocked()) {
      return;
    }
    const day = utcDay(date);
    this.value.update((days) =>
      this.chosenDays().has(dayOf(day))
        ? days.filter((chosen) => dayOf(chosen) !== dayOf(day))
        : [...days, day].sort(),
    );
    this.touch.emit();
  }
}

function utcDay(date: Date): string {
  return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate())).toISOString();
}

/** The day of an ISO string, without its time. */
function dayOf(date: string): string {
  return date.slice(0, 10);
}
