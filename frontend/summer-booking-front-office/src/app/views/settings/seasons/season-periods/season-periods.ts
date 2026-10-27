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
import { ExpansionPanel } from '../../../../components/shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { SectionTitle } from '../../../../components/shared/ui/titles/section-title/section-title';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { SeasonPeriodIntervalItem } from '../../../../entities/settings/seasons/season-period-interval-item';
import { SeasonPeriodItem } from '../../../../entities/settings/seasons/season-period-item';
import { SeasonPeriodUpdateRequest } from '../../../../entities/settings/seasons/season-period-update-request';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../services/api/seasons/season.service';

interface SeasonIntervalRow {
  idInterval: string | null;
  dateFrom: string;
  dateTo: string;
  /** As the list gave it, for the edit popup. */
  item: SeasonPeriodIntervalItem;
}

interface SeasonPeriodRow {
  idPeriod: string | null;
  periodName: string;
  intervals: SeasonIntervalRow[];
  /** As the list gave it, for the popup that adds an interval. */
  item: SeasonPeriodItem;
}

/** No name when there are several: the popup then tells how many. */
interface IntervalsToDelete {
  idIntervals: string[];
  name: string | null;
}

@Component({
  selector: 'app-season-periods',
  imports: [
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
  protected readonly idPeriodsToDelete = computed(() => {
    const idPeriod = this.periodToDelete()?.idPeriod;
    return idPeriod ? [idPeriod] : [];
  });
  protected readonly isEditPeriodOpen = signal(false);
  protected readonly periodToEdit = signal<SeasonPeriodUpdateRequest | null>(null);
  protected readonly isAddIntervalOpen = signal(false);
  protected readonly periodToExtend = signal<SeasonPeriodItem | null>(null);
  protected readonly isEditIntervalOpen = signal(false);
  protected readonly intervalToEdit = signal<SeasonPeriodIntervalItem | null>(null);
  protected readonly isDeleteIntervalsAsked = signal(false);
  protected readonly intervalsToDelete = signal<IntervalsToDelete>({ idIntervals: [], name: null });
  private readonly reloads = signal(0);
  /** Chosen date intervals of all the periods of the tab. */
  private readonly selectedIntervals = signal<ReadonlySet<string>>(new Set());
  protected readonly selectedTotal = computed(() => this.selectedIntervals().size);
  private readonly periodItems = signal<SeasonPeriodItem[] | null>(null);
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
      idPeriod: item.idPeriod,
      periodName: item.periodName ?? '',
      intervals: item.intervals.map((interval) => ({
        idInterval: interval.idInterval,
        dateFrom: format(interval.dateFrom),
        dateTo: format(interval.dateTo),
        item: interval,
      })),
      item,
    }));
  });
  /** All the date intervals of the season of the tab: a new or changed one cannot overlap them. */
  protected readonly seasonIntervals = computed(() =>
    (this.periodItems() ?? []).flatMap(({ intervals }) => intervals),
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
      const idProperty = this.auth.user()?.propertyId;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() => {
        this.error.set(null);
        return this.seasons.periodList(idProperty, this.isNextSeason).subscribe({
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

  protected readonly deletePeriod = (idProperty: string, [idPeriod]: string[]) =>
    this.seasons.deletePeriod({ idProperty, idPeriod });

  protected readonly deleteIntervals = (idProperty: string, idIntervals: string[]) =>
    this.seasons.deleteIntervals({ idProperty, idIntervals });

  protected askDelete(period: SeasonPeriodRow): void {
    this.periodToDelete.set(period);
    this.isDeleteAsked.set(true);
  }

  protected askDeleteInterval(interval: SeasonIntervalRow): void {
    if (interval.idInterval) {
      this.intervalsToDelete.set({
        idIntervals: [interval.idInterval],
        name: `${interval.dateFrom} – ${interval.dateTo}`,
      });
      this.isDeleteIntervalsAsked.set(true);
    }
  }

  protected editPeriod(period: SeasonPeriodRow): void {
    this.periodToEdit.set({ idPeriod: period.idPeriod, periodName: period.periodName });
    this.isEditPeriodOpen.set(true);
  }

  protected addInterval(period: SeasonPeriodRow): void {
    this.periodToExtend.set(period.item);
    this.isAddIntervalOpen.set(true);
  }

  protected editInterval(interval: SeasonIntervalRow): void {
    this.intervalToEdit.set(interval.item);
    this.isEditIntervalOpen.set(true);
  }

  protected askDeleteSelected(): void {
    this.intervalsToDelete.set({ idIntervals: [...this.selectedIntervals()], name: null });
    this.isDeleteIntervalsAsked.set(true);
  }

  protected isIntervalSelected(idInterval: string | null): boolean {
    return !!idInterval && this.selectedIntervals().has(idInterval);
  }

  protected selectInterval(idInterval: string | null, isSelected: boolean): void {
    if (!idInterval) {
      return;
    }
    this.selectedIntervals.update((selected) => {
      const next = new Set(selected);
      if (isSelected) {
        next.add(idInterval);
      } else {
        next.delete(idInterval);
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
}
