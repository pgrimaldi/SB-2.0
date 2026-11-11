import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  TreeValidationResult,
  applyEach,
  applyWhen,
  form,
  required,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, finalize, firstValueFrom, forkJoin, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { PackageAddItemRequest } from '../../../../../entities/settings/catalog/package-add-item-request';
import { PackagePeriodPrice } from '../../../../../entities/settings/catalog/package-period-price';
import { PackageReducedSeasonalPrice } from '../../../../../entities/settings/catalog/package-reduced-seasonal-price';
import { PackageSectorPrice } from '../../../../../entities/settings/catalog/package-sector-price';
import { PackageTranslation } from '../../../../../entities/settings/catalog/package-translation';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { requiredText } from '../../../../shared/forms/validators';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { SERVICE_LANGUAGES } from '../../services/shared/services-translations/services-translations';
import { PackageGeneralInfoTab } from '../package-general-info-tab/package-general-info-tab';
import { PackagePriceListTab } from '../package-price-list-tab/package-price-list-tab';

interface PackageFormTexts {
  tabs: {
    general_info: string;
    price_list: string;
  };
}

/** The tabs of the panel, in their order. */
enum PanelTab {
  GeneralInfo,
  PriceList,
}

@Component({
  selector: 'app-package-add-item-panel',
  imports: [PackageGeneralInfoTab, PackagePriceListTab, SidePanel, TabGroup, TranslatePipe],
  templateUrl: './package-add-item-panel.html',
  styleUrl: './package-add-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageAddItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly created = output<void>();

  private readonly auth = inject(AuthBehaviour);
  private readonly sectorService = inject(SectorService);
  private readonly priceLists = inject(PriceListService);
  private readonly seasons = inject(SeasonService);
  private readonly language = inject(LanguageBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly PanelTab = PanelTab;
  protected readonly mainLanguage = this.language.current;
  protected readonly selectedTab = signal<number>(PanelTab.GeneralInfo);
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.catalog.packages.form',
    ) as Observable<PackageFormTexts>,
    { initialValue: null },
  );
  protected readonly tabs = computed<readonly TabGroupTab[]>(() => [
    { label: this.texts()?.tabs.general_info ?? '', widthPercent: 50 },
    { label: this.texts()?.tabs.price_list ?? '', widthPercent: 50 },
  ]);
  private readonly draft = signal(newDraft());
  protected readonly draftForm = form(this.draft, (path) => {
    applyEach(path.translations, (translation) => {
      applyWhen(
        translation,
        ({ valueOf }) => valueOf(translation.language) === this.mainLanguage(),
        (main) => requiredText(main.packageName),
      );
    });
  });
  private readonly reducedSeasonalPrice = signal(new PackageReducedSeasonalPrice());
  protected readonly reducedSeasonalPriceForm = form(this.reducedSeasonalPrice, (path) => {
    required(path.dateFrom);
    required(path.dateTo);
    requiredText(path.reducedSeasonalPriceName);
  });
  protected readonly isReducedSeasonalPriceOpen = signal(false);
  /** The form with nothing typed: with an empty price for every row once the rows are read. */
  private readonly pristine = signal(JSON.stringify(newDraft()));
  protected readonly sectors = signal<readonly ComboboxItem[]>([]);
  protected readonly periods = signal<readonly SeasonPeriodItem[]>([]);
  /** The rows of the price list are read once per opening, the first time its tab is shown. */
  protected readonly isPriceListLoaded = signal(false);
  protected readonly isFetching = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isSaveDisabled = computed(
    () => this.isFetching() || this.draftForm().invalid(),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private fetching?: Subscription;

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
    effect(() => {
      if (this.open() && this.selectedTab() === PanelTab.PriceList && !this.isPriceListLoaded()) {
        untracked(() => this.fetchPriceList());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() &&
      (JSON.stringify(this.draft()) !== this.pristine() || this.isReducedSeasonalPriceTyped())
    );
  }

  /** The open box differs from what it showed when opened: empty, or the row being edited. */
  private isReducedSeasonalPriceTyped(): boolean {
    const shown = this.draft().reducedSeasonalPrice ?? new PackageReducedSeasonalPrice();
    return (
      this.isReducedSeasonalPriceOpen() &&
      JSON.stringify(this.reducedSeasonalPrice()) !== JSON.stringify(shown)
    );
  }

  protected save(): void {
    if (this.isSaving()) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.priceLists.createPackage(this.draft()));
        this.isSaving.set(false);
        this.open.set(false);
        this.created.emit();
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.draftForm, (problem) =>
          this.error.set(problem),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  protected async leave(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.open.set(false);
    }
  }

  private reset(): void {
    this.fetching?.unsubscribe();
    this.isFetching.set(false);
    this.error.set(null);
    this.draft.set(newDraft());
    this.draftForm().reset();
    this.pristine.set(JSON.stringify(newDraft()));
    this.reducedSeasonalPrice.set(new PackageReducedSeasonalPrice());
    this.reducedSeasonalPriceForm().reset();
    this.isReducedSeasonalPriceOpen.set(false);
    this.sectors.set([]);
    this.periods.set([]);
    this.isPriceListLoaded.set(false);
    this.selectedTab.set(PanelTab.GeneralInfo);
  }

  /** The sectors and the periods of the current season: one row of prices each. */
  private fetchPriceList(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (!idProperty || this.isFetching()) {
      return;
    }
    this.error.set(null);
    this.isFetching.set(true);
    this.fetching = forkJoin([
      this.sectorService.sectorCombobox(idProperty),
      this.seasons.periodList(idProperty, false),
    ])
      .pipe(finalize(() => this.isFetching.set(false)))
      .subscribe({
        next: ([sectors, periods]) => {
          this.setPriceRows(sectors, periods);
          this.isPriceListLoaded.set(true);
        },
        error: (error: unknown) => this.error.set(toApiProblem(error)),
      });
  }

  /** An empty price for every sector, and for every sector in every period. */
  private setPriceRows(
    sectors: readonly ComboboxItem[],
    periods: readonly SeasonPeriodItem[],
  ): void {
    const sectorPrices = sectors.map(({ id }) =>
      Object.assign(new PackageSectorPrice(), { idSector: id }),
    );
    const periodPrices = sectors.flatMap(({ id }) =>
      periods.map(({ idPeriod }) =>
        Object.assign(new PackagePeriodPrice(), { idSector: id, idPeriod }),
      ),
    );
    this.sectors.set(sectors);
    this.periods.set(periods);
    this.draftForm.sectorPrices().value.set(sectorPrices);
    this.draftForm.periodPrices().value.set(periodPrices);
    this.pristine.set(JSON.stringify(Object.assign(newDraft(), { sectorPrices, periodPrices })));
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.fetching?.unsubscribe();
  }
}

/** Empty, with a name for every language: the user's one is the main. */
function newDraft(): PackageAddItemRequest {
  return Object.assign(new PackageAddItemRequest(), {
    translations: SERVICE_LANGUAGES.map((language) =>
      Object.assign(new PackageTranslation(), { language }),
    ),
  });
}
