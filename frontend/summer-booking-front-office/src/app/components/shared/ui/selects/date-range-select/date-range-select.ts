import { CdkConnectedOverlay, CdkOverlayOrigin, ConnectedPosition } from '@angular/cdk/overlay';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  ViewEncapsulation,
  computed,
  inject,
  input,
  model,
  signal,
  viewChild,
} from '@angular/core';
import { DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { DateRange, MatCalendar } from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { resolveIcons } from '../../icons/icons';
import { DATE_RANGE_SELECT_ICONS, DateRangeSelectHeader } from './date-range-select-header';

export interface DateRangeSelectRange {
  start: Date;
  end: Date;
}

/** A ready choice of the list: `range` null means every date. */
export interface DateRangeSelectOption {
  label: string;
  range: DateRangeSelectRange | null;
}

export interface DateRangeSelectTexts {
  confirm?: string;
}

const PANEL_POSITIONS: ConnectedPosition[] = [
  { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: 4 },
  { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -4 },
];

@Component({
  selector: 'app-date-range-select',
  imports: [CdkConnectedOverlay, CdkOverlayOrigin, MatCalendar, MatIconModule],
  templateUrl: './date-range-select.html',
  styleUrl: './date-range-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: DATE_RANGE_SELECT_ICONS, useFactory: () => inject(DateRangeSelect).icons },
  ],
  // The panel opens in an overlay outside the component: its styles must be global.
  encapsulation: ViewEncapsulation.None,
})
export class DateRangeSelect {
  readonly options = input<readonly DateRangeSelectOption[]>([]);
  /** The chosen dates; null means every date. */
  readonly range = model<DateRangeSelectRange | null>(null);
  readonly texts = input<DateRangeSelectTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [open, previous month, next month]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [open, previous month, next month]. */
  readonly pathIcon = input<readonly string[] | null>();

  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
  private readonly dateFormats = inject(MAT_DATE_FORMATS);

  protected readonly Header = DateRangeSelectHeader;
  protected readonly positions = PANEL_POSITIONS;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly isOpen = signal(false);
  protected readonly panelWidth = signal(0);
  /** The days picked in the calendar, applied only with the confirm button. */
  protected readonly draft = signal<DateRange<Date> | null>(null);
  protected readonly isDraftComplete = computed(() => !!this.draft()?.start && !!this.draft()?.end);
  protected readonly label = computed(() => {
    const range = this.range();
    const option = this.options().find((item) => this.isSameRange(item.range, range));
    if (option) {
      return option.label;
    }
    return range ? `${this.format(range.start)} - ${this.format(range.end)}` : '';
  });
  private readonly trigger = viewChild.required<ElementRef<HTMLElement>>('trigger');

  protected toggle(): void {
    if (this.isOpen()) {
      this.close();
      return;
    }
    const range = this.range();
    this.draft.set(range ? new DateRange(range.start, range.end) : null);
    this.panelWidth.set(this.trigger().nativeElement.getBoundingClientRect().width);
    this.isOpen.set(true);
  }

  protected close(): void {
    this.isOpen.set(false);
  }

  protected closeOutside(event: MouseEvent): void {
    if (!this.trigger().nativeElement.contains(event.target as Node)) {
      this.close();
    }
  }

  protected choose(option: DateRangeSelectOption): void {
    this.range.set(option.range);
    this.close();
  }

  /** First click: the first day; second click: the last one (or a new first day before it). */
  protected pick(date: Date | null): void {
    if (!date) {
      return;
    }
    const draft = this.draft();
    const isNewStart =
      !draft?.start || !!draft.end || this.dateAdapter.compareDate(date, draft.start) < 0;
    this.draft.set(isNewStart ? new DateRange(date, null) : new DateRange(draft.start, date));
  }

  protected confirm(): void {
    const draft = this.draft();
    if (draft?.start && draft.end) {
      this.range.set({ start: draft.start, end: draft.end });
      this.close();
    }
  }

  private isSameRange(
    first: DateRangeSelectRange | null,
    second: DateRangeSelectRange | null,
  ): boolean {
    if (!first || !second) {
      return first === second;
    }
    return (
      this.dateAdapter.sameDate(first.start, second.start) &&
      this.dateAdapter.sameDate(first.end, second.end)
    );
  }

  private format(date: Date): string {
    return this.dateAdapter.format(date, this.dateFormats.display.dateInput);
  }
}
