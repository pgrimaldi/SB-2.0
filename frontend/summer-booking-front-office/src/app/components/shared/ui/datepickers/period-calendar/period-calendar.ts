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
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { DateRange, MatCalendar, MatCalendarCellClassFunction } from '@angular/material/datepicker';
import { FilterChip, FilterChips } from '../../chips/filter-chips/filter-chips';
import { resolveIcons } from '../../icons/icons';
import { PERIOD_CALENDAR_ICONS, PeriodCalendarHeader } from './period-calendar-header';

/** First and last day of a period, each at midnight UTC as an ISO string. */
export interface PeriodCalendarPeriod {
  dateFrom: string | null;
  dateTo: string | null;
}

export interface PeriodCalendarTexts {
  hint?: string;
  selected?: string;
}

interface PeriodChip extends FilterChip {
  index: number;
}

interface Days {
  start: Date;
  end: Date;
}

/**
 * A calendar always open where several periods are chosen: two clicks (first and last day) add a
 * period, the chosen periods stay coloured and are listed under it, each with an X to remove it.
 */
@Component({
  selector: 'app-period-calendar',
  imports: [FilterChips, MatCalendar],
  templateUrl: './period-calendar.html',
  styleUrl: './period-calendar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: PERIOD_CALENDAR_ICONS, useFactory: () => inject(PeriodCalendar).icons }],
  // The days are drawn by Material's calendar: their classes need global styles.
  encapsulation: ViewEncapsulation.None,
})
export class PeriodCalendar {
  readonly value = model<PeriodCalendarPeriod[]>([]);
  readonly readonly = input(false);
  readonly disabled = input(false);
  readonly texts = input<PeriodCalendarTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [previous month, next month, remove a period]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: same order. */
  readonly pathIcon = input<readonly string[] | null>();
  /** A period was added or removed: a Signal Forms `[formField]` marks it as touched. */
  readonly touch = output<void>();

  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
  private readonly dateFormats = inject(MAT_DATE_FORMATS);

  protected readonly Header = PeriodCalendarHeader;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly removeIcon = computed(() => this.icons().list.slice(2, 3));
  protected readonly isLocked = computed(() => this.readonly() || this.disabled());
  /** The first day clicked, waiting for the last one. */
  protected readonly draft = signal<DateRange<Date> | null>(null);
  private readonly periods = computed(() =>
    this.value().flatMap((period): Days[] => {
      const start = this.day(period.dateFrom);
      const end = this.day(period.dateTo);
      return start && end ? [{ start, end }] : [];
    }),
  );
  protected readonly chips = computed<readonly PeriodChip[]>(() =>
    this.periods().map(({ start, end }, index) => ({
      label: `${this.format(start)} - ${this.format(end)}`,
      index,
    })),
  );
  protected readonly dateClass = computed<MatCalendarCellClassFunction<Date>>(() => {
    const periods = this.periods();
    return (date, view) => {
      if (view !== 'month') {
        return '';
      }
      const period = periods.find(
        ({ start, end }) =>
          this.dateAdapter.compareDate(date, start) >= 0 &&
          this.dateAdapter.compareDate(date, end) <= 0,
      );
      return {
        period__calendar__chosen: !!period,
        period__calendar__chosen__start: !!period && this.dateAdapter.sameDate(date, period.start),
        period__calendar__chosen__end: !!period && this.dateAdapter.sameDate(date, period.end),
      };
    };
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

  /** First click: the first day; second click: the last one (or a new first day before it). */
  protected pick(date: Date | null): void {
    if (!date || this.isLocked()) {
      return;
    }
    const start = this.draft()?.start;
    if (!start || this.dateAdapter.compareDate(date, start) < 0) {
      this.draft.set(new DateRange(date, null));
      return;
    }
    this.draft.set(null);
    const period = { dateFrom: this.utcDay(start), dateTo: this.utcDay(date) };
    if (
      !this.value().some(
        ({ dateFrom, dateTo }) => dateFrom === period.dateFrom && dateTo === period.dateTo,
      )
    ) {
      this.value.update((periods) => [...periods, period]);
    }
    this.touch.emit();
  }

  protected remove(chip: PeriodChip): void {
    if (this.isLocked()) {
      return;
    }
    this.value.update((periods) => periods.filter((_, index) => index !== chip.index));
    this.touch.emit();
  }

  private day(value: string | null): Date | null {
    if (!value) {
      return null;
    }
    const utc = new Date(value);
    return new Date(utc.getUTCFullYear(), utc.getUTCMonth(), utc.getUTCDate());
  }

  private utcDay(day: Date): string {
    return new Date(Date.UTC(day.getFullYear(), day.getMonth(), day.getDate())).toISOString();
  }

  private format(date: Date): string {
    return this.dateAdapter.format(date, this.dateFormats.display.dateInput);
  }
}
