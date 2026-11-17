import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
  viewChildren,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import { DateLanguageBehaviour } from '../../../../behaviours/i18n/date-language.behaviour';
import { SeasonDatesBehaviour } from '../../../../behaviours/settings/season-dates.behaviour';
import { DeleteItemsPopup } from '../../../../components/management/delete-items-popup/delete-items-popup';
import { SeasonIntervalAddPopup } from '../../../../components/management/settings/seasons/season-interval-add-popup/season-interval-add-popup';
import { SeasonIntervalEditPopup } from '../../../../components/management/settings/seasons/season-interval-edit-popup/season-interval-edit-popup';
import { SeasonPeriodAddPopup } from '../../../../components/management/settings/seasons/season-period-add-popup/season-period-add-popup';
import { SeasonPeriodEditPopup } from '../../../../components/management/settings/seasons/season-period-edit-popup/season-period-edit-popup';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { IconButton } from '../../../../components/shared/ui/buttons/icon-button/icon-button';
import { SelectionCheckbox } from '../../../../components/shared/ui/checkboxes/selection-checkbox/selection-checkbox';
import { DATA_RELOAD } from '../../../../components/shared/ui/data/data-reload';
import { AlertPopup } from '../../../../components/shared/ui/dialogs/alert-popup/alert-popup';
import { ExpansionPanel } from '../../../../components/shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { SectionTitle } from '../../../../components/shared/ui/titles/section-title/section-title';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { SeasonPeriodInterval } from '../../../../entities/settings/seasons/season-period-interval';
import { SeasonPeriodIntervalItem } from '../../../../entities/settings/seasons/season-period-interval-item';
import {
  MAX_SEASON_PERIOD_RANGES,
  SeasonPeriodItem,
} from '../../../../entities/settings/seasons/season-period-item';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../services/api/seasons/season.service';

interface SeasonIntervalRow {
  startsOn: string;
  endsOn: string;
  /** As the list gave it: the selection, the edit popup and the deletion know it by this. */
  item: SeasonPeriodInterval;
}

interface SeasonPeriodRow {
  key: number | null;
  name: string;
  intervals: SeasonIntervalRow[];
  /** As the list gave it, for the popups that change it. */
  item: SeasonPeriodItem;
}

/** No name when there are several: the popup then tells how many. */
interface IntervalsToDelete {
  intervals: SeasonPeriodIntervalItem[];
  name: string | null;
}

@Component({
  selector: 'app-season-periods',
  imports: [
    AlertPopup,
    Button,
    DeleteItemsPopup,
    ExpansionPanel,
    IconButton,
    SeasonIntervalAddPopup,
    SeasonIntervalEditPopup,
    SeasonPeriodAddPopup,
    SeasonPeriodEditPopup,
    SectionTitle,
    SelectionCheckbox,
    TranslatePipe,
  ],
  templateUrl: './season-periods.html',
  styleUrl: './season-periods.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeasonPeriods {
  private readonly auth = inject(AuthBehaviour);
  private readonly seasons = inject(SeasonService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly dateLanguage = inject(DateLanguageBehaviour);
  private readonly seasonDates = inject(SeasonDatesBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });

  protected readonly isNextSeason = inject(ActivatedRoute).snapshot.data['isNextSeason'] === true;
  protected readonly season = computed(() => this.seasonDates.rangeOf(this.isNextSeason));
  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly titleIcon = ['/assets/images/GeneralIcon_CalendarFilled.svg'] as const;
  protected readonly addIcon = ['/assets/images/GeneralIcon_Add.svg'] as const;
  /** [before the title, arrow]: no icon before the name of a period. */
  protected readonly periodIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly deleteIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;
  protected readonly editIcon = ['/assets/images/Manager_Table_Edit.svg'] as const;
  protected readonly addIntervalIcon = ['/assets/images/Seasons_AddInterval.svg'] as const;
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  protected readonly isAddOpen = signal(false);
  protected readonly isDeleteAsked = signal(false);
  protected readonly periodToDelete = signal<SeasonPeriodRow | null>(null);
  protected readonly keysToDelete = computed(() => {
    const key = this.periodToDelete()?.key;
    return key === null || key === undefined ? [] : [key];
  });
  protected readonly isEditPeriodOpen = signal(false);
  protected readonly periodToEdit = signal<SeasonPeriodItem | null>(null);
  protected readonly isAddIntervalOpen = signal(false);
  protected readonly periodToExtend = signal<SeasonPeriodItem | null>(null);
  protected readonly isEditIntervalOpen = signal(false);
  protected readonly periodOfInterval = signal<SeasonPeriodItem | null>(null);
  protected readonly intervalToEdit = signal<SeasonPeriodInterval | null>(null);
  protected readonly isDeleteIntervalsAsked = signal(false);
  protected readonly intervalsToDelete = signal<IntervalsToDelete>({ intervals: [], name: null });
  protected readonly isPeriodLeftEmpty = signal(false);
  protected readonly isPeriodFull = signal(false);
  protected readonly maxRanges = MAX_SEASON_PERIOD_RANGES;
  private readonly reloads = signal(0);
  /** Chosen date intervals of all the periods of the tab, as the list gave them. */
  private readonly selectedIntervals = signal<ReadonlySet<SeasonPeriodInterval>>(new Set());
  protected readonly selectedTotal = computed(() => this.selectedIntervals().size);
  protected readonly periodItems = signal<SeasonPeriodItem[] | null>(null);
  protected readonly periods = computed<SeasonPeriodRow[] | null>(() => {
    const items = this.periodItems();
    if (!items) {
      return null;
    }
    const day = new Intl.DateTimeFormat(this.dateLanguage.locale(), {
      day: '2-digit',
      month: 'long',
      year: 'numeric',
      timeZone: 'UTC',
    });
    const format = (date: string | null): string => (date ? day.format(new Date(date)) : '');
    return items.map((item) => ({
      key: item.key,
      name: item.name ?? '',
      intervals: item.ranges.map((range) => ({
        startsOn: format(range.startsOn),
        endsOn: format(range.endsOn),
        item: range,
      })),
      item,
    }));
  });
  /** All the date intervals of the season of the tab: a new or changed one cannot overlap them. */
  protected readonly seasonIntervals = computed(() =>
    (this.periodItems() ?? []).flatMap(({ ranges }) => ranges),
  );
  private readonly panels = viewChildren(ExpansionPanel);
  protected readonly isAllShown = computed(() => {
    const panels = this.panels();
    return panels.length > 0 && panels.every((panel) => panel.isExpanded());
  });
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    effect((onCleanup) => {
      this.dataReload?.();
      this.reloads();
      if (!this.auth.user()?.propertyId) {
        return;
      }
      const subscription = untracked(() => {
        this.error.set(null);
        return this.seasons.periodList(this.isNextSeason).subscribe({
          next: (items) => {
            this.selectedIntervals.set(new Set());
            this.periodItems.set(items);
          },
          error: (error: unknown) => this.error.set(toApiProblem(error)),
        });
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected readonly deletePeriod = ([key]: number[]) =>
    this.seasons.deletePeriod(this.isNextSeason, key);

  protected readonly deleteIntervals = (intervals: SeasonPeriodIntervalItem[]) =>
    this.seasons.deleteRanges(this.isNextSeason, intervals);

  protected askDelete(period: SeasonPeriodRow): void {
    this.periodToDelete.set(period);
    this.isDeleteAsked.set(true);
  }

  protected askDeleteInterval(interval: SeasonIntervalRow): void {
    this.askDeleteIntervals(new Set([interval.item]), `${interval.startsOn} – ${interval.endsOn}`);
  }

  protected editPeriod(period: SeasonPeriodRow): void {
    this.periodToEdit.set(period.item);
    this.isEditPeriodOpen.set(true);
  }

  protected addInterval(period: SeasonPeriodRow): void {
    if (period.item.ranges.length >= MAX_SEASON_PERIOD_RANGES) {
      this.isPeriodFull.set(true);
      return;
    }
    this.periodToExtend.set(period.item);
    this.isAddIntervalOpen.set(true);
  }

  protected editInterval(period: SeasonPeriodRow, interval: SeasonIntervalRow): void {
    this.periodOfInterval.set(period.item);
    this.intervalToEdit.set(interval.item);
    this.isEditIntervalOpen.set(true);
  }

  protected askDeleteSelected(): void {
    this.askDeleteIntervals(this.selectedIntervals(), null);
  }

  protected isIntervalSelected(interval: SeasonIntervalRow): boolean {
    return this.selectedIntervals().has(interval.item);
  }

  protected selectInterval(interval: SeasonIntervalRow, isSelected: boolean): void {
    this.selectedIntervals.update((selected) => {
      const next = new Set(selected);
      if (isSelected) {
        next.add(interval.item);
      } else {
        next.delete(interval.item);
      }
      return next;
    });
  }

  protected clearSelection(): void {
    this.selectedIntervals.set(new Set());
  }

  protected toggleAll(): void {
    const isExpanded = !this.isAllShown();
    this.panels().forEach((panel) => panel.isExpanded.set(isExpanded));
  }

  protected reload(): void {
    this.reloads.update((count) => count + 1);
  }

  /** A period keeps at least one interval: when all of its would go, nothing is asked nor deleted. */
  private askDeleteIntervals(chosen: ReadonlySet<SeasonPeriodInterval>, name: string | null): void {
    const periods = this.periodItems() ?? [];
    if (periods.some(({ ranges }) => ranges.length && ranges.every((range) => chosen.has(range)))) {
      this.isPeriodLeftEmpty.set(true);
      return;
    }
    const intervals = periods.flatMap(({ key, ranges }) =>
      ranges
        .filter((range) => chosen.has(range))
        .map(({ startsOn, endsOn }) => ({ key, startsOn, endsOn })),
    );
    if (intervals.length) {
      this.intervalsToDelete.set({ intervals, name });
      this.isDeleteIntervalsAsked.set(true);
    }
  }
}
