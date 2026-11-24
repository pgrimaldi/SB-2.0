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
  MAX_LENGTH,
  TreeValidationResult,
  applyEach,
  applyWhen,
  form,
  metadata,
  required,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
  EMPTY,
  Observable,
  Subscription,
  finalize,
  firstValueFrom,
  forkJoin,
  of,
  shareReplay,
  switchMap,
} from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { DropdownList } from '../../../../../behaviours/forms/dropdown-list';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import {
  apiFieldError,
  apiFieldErrorsOrGeneral,
} from '../../../../../behaviours/validation/api-field-errors';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  MAX_PACKAGE_INCLUDED_SERVICES_LENGTH,
  PackageAddItemRequest,
} from '../../../../../entities/settings/catalog/package-add-item-request';
import { PackageArticle } from '../../../../../entities/settings/catalog/package-article';
import { PackagePeriodPrice } from '../../../../../entities/settings/catalog/package-period-price';
import {
  MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH,
  PackageReducedSeasonalPrice,
} from '../../../../../entities/settings/catalog/package-reduced-seasonal-price';
import { PackageSectorPrice } from '../../../../../entities/settings/catalog/package-sector-price';
import {
  MAX_PACKAGE_NAME_LENGTH,
  PackageTranslation,
} from '../../../../../entities/settings/catalog/package-translation';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { noOverlap, requiredText } from '../../../../shared/forms/validators';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { SERVICE_LANGUAGES } from '../../services/shared/services-translations/services-translations';
import { orderedItems, withExistingItems } from '../package-items';
import { PackageGeneralInfoTab } from '../package-general-info-tab/package-general-info-tab';
import { PanelTab, panelTabOf } from '../package-panel-tabs';
import { PackageReducedPrice } from '../package-reduced-price/package-reduced-price';
import {
  intermediatePricesOf,
  maxPrices,
  maxPublicHolidays,
  periodPricesOf,
  reducedPricesOf,
  sectorPricesOf,
  vatRange,
} from '../price-limits';
import { PriceListTab } from '../price-list-tab/price-list-tab';
import { comparableJson } from '../../../../shared/forms/text-comparison';

interface PackageFormTexts {
  tabs: {
    general_info: string;
    price_list: string;
  };
}

@Component({
  selector: 'app-package-add-item-panel',
  imports: [
    PackageGeneralInfoTab,
    PackageReducedPrice,
    PriceListTab,
    SidePanel,
    TabGroup,
    TranslatePipe,
  ],
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
      metadata(translation.packageName, MAX_LENGTH, () => MAX_PACKAGE_NAME_LENGTH);
      applyWhen(
        translation,
        ({ valueOf }) => valueOf(translation.language) === this.mainLanguage(),
        (main) => requiredText(main.packageName),
      );
    });
    metadata(path.includedServices, MAX_LENGTH, () => MAX_PACKAGE_INCLUDED_SERVICES_LENGTH);
    vatRange(path.vatRate);
    maxPublicHolidays(path.publicHolidayDates);
    maxPrices(path.sectorPrices, sectorPricesOf);
    maxPrices(path.periodPrices, periodPricesOf);
    maxPrices(path.intermediatePrices, intermediatePricesOf);
    maxPrices(path.reducedSeasonalPrices, reducedPricesOf);
  });
  private readonly reducedSeasonalPrice = signal(new PackageReducedSeasonalPrice());
  protected readonly reducedSeasonalPriceForm = form(this.reducedSeasonalPrice, (path) => {
    required(path.startsOn);
    required(path.endsOn);
    requiredText(path.reducedSeasonalPriceName);
    metadata(
      path.reducedSeasonalPriceName,
      MAX_LENGTH,
      () => MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH,
    );
    noOverlap(path, () =>
      this.draft().reducedSeasonalPrices.filter(
        (_, index) => index !== this.reducedSeasonalPriceIndex(),
      ),
    );
  });
  protected readonly reducedSeasonalPriceIndex = signal<number | null>(null);
  /** The form with nothing typed: with an empty price for every row once the rows are read. */
  private readonly pristine = signal(comparableJson(newDraft()));
  protected readonly sectors = signal<readonly ComboboxItem[]>([]);
  /** The sectors of Info Generali and of the price rows, read once per opening. */
  private sectorRead: Observable<ComboboxItem[]> = EMPTY;
  protected readonly sectorList = new DropdownList(() => this.sectorRead, this.errorText);
  protected readonly sectorAreaList = new DropdownList(
    () => this.sectorService.sectorAreaCombobox(),
    this.errorText,
  );
  protected readonly articles = signal<readonly PackageArticle[]>([]);
  protected readonly periods = signal<readonly SeasonPeriodItem[]>([]);
  /** The rows of the price list are read once per opening, the first time its tab is shown. */
  protected readonly isPriceListLoaded = signal(false);
  protected readonly isFetching = signal(false);
  protected readonly isFetchingArticles = signal(false);
  private readonly areArticlesLoaded = signal(false);
  protected readonly packageName = computed(
    () =>
      this.draft()
        .translations.find(({ language }) => language === this.mainLanguage())
        ?.packageName?.trim() ?? '',
  );
  protected readonly isSaving = signal(false);
  /** Also while the reduced seasonal price box is open: its values are not in the package yet. */
  protected readonly isSaveDisabled = computed(
    () =>
      this.isFetching() ||
      this.isFetchingArticles() ||
      this.draftForm().invalid() ||
      this.reducedSeasonalPriceIndex() !== null,
  );
  private readonly error = signal<ApiProblem | null>(null);
  private readonly articlesError = signal<ApiProblem | null>(null);
  private readonly priceListError = signal<ApiProblem | null>(null);
  /** The error of the save, or else the one of what the shown tab reads. */
  private readonly shownError = computed(
    () =>
      this.error() ??
      (this.selectedTab() === PanelTab.GeneralInfo ? this.articlesError() : this.priceListError()),
  );
  protected readonly errorMessage = toSignal(
    toObservable(this.shownError).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private fetching?: Subscription;
  private fetchingArticles?: Subscription;

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
    effect(() => {
      if (this.open() && this.selectedTab() === PanelTab.GeneralInfo && !this.areArticlesLoaded()) {
        untracked(() => this.refreshArticles());
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
      (comparableJson(this.draft()) !== this.pristine() || this.isReducedSeasonalPriceTyped())
    );
  }

  /** The open box differs from what it showed when opened: empty, or the row being edited. */
  private isReducedSeasonalPriceTyped(): boolean {
    const index = this.reducedSeasonalPriceIndex();
    if (index === null) {
      return false;
    }
    const shown = this.draft().reducedSeasonalPrices[index] ?? new PackageReducedSeasonalPrice();
    return comparableJson(this.reducedSeasonalPrice()) !== comparableJson(shown);
  }

  protected save(): void {
    if (this.isSaving() || this.reducedSeasonalPriceIndex() !== null) {
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
        const problem = toApiProblem(error);
        this.showTabOf(problem);
        if (
          problem.code === ApiErrorCode.CatalogSectorNotFound ||
          problem.code === ApiErrorCode.MapSpotNotFound
        ) {
          this.loadSectors();
        }
        if (problem.code === ApiErrorCode.InventoryItemNotFound) {
          this.refreshArticles();
        }
        if (problem.code === ApiErrorCode.PricingCalculationMethodNotFound) {
          return apiFieldError(this.draftForm.calculationMethod, problem.code);
        }
        if (problem.code === ApiErrorCode.CatalogVatRateNotAllowed) {
          return apiFieldError(this.draftForm.vatRate, problem.code, problem.args);
        }
        return apiFieldErrorsOrGeneral(problem, this.draftForm, (general) =>
          this.error.set(general),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  private loadSectors(): void {
    if (!this.auth.user()?.propertyId) {
      return;
    }
    this.sectorRead = this.readSectors();
    this.sectorList.load();
    this.sectorAreaList.load();
  }

  private readSectors(): Observable<ComboboxItem[]> {
    return this.sectorService.sectorCombobox().pipe(shareReplay({ bufferSize: 1, refCount: true }));
  }

  /** The tab of the first wrong field, where its error shows. */
  private showTabOf(problem: ApiProblem): void {
    const tab = panelTabOf(problem);
    if (tab !== null) {
      this.selectedTab.set(tab);
    }
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
    this.articlesError.set(null);
    this.priceListError.set(null);
    this.draft.set(newDraft());
    this.draftForm().reset();
    this.pristine.set(comparableJson(newDraft()));
    this.reducedSeasonalPrice.set(new PackageReducedSeasonalPrice());
    this.reducedSeasonalPriceForm().reset();
    this.reducedSeasonalPriceIndex.set(null);
    this.fetchingArticles?.unsubscribe();
    this.isFetchingArticles.set(false);
    this.articles.set([]);
    this.areArticlesLoaded.set(false);
    this.sectors.set([]);
    this.periods.set([]);
    this.isPriceListLoaded.set(false);
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.sectorList.clear();
    this.sectorAreaList.clear();
    this.loadSectors();
  }

  /** What the warehouse has for the package; a chosen item that is no longer there goes away. */
  private refreshArticles(): void {
    if (!this.auth.user()?.propertyId || this.isFetchingArticles()) {
      return;
    }
    this.articlesError.set(null);
    this.isFetchingArticles.set(true);
    this.fetchingArticles = this.priceLists
      .packageArticles()
      .pipe(finalize(() => this.isFetchingArticles.set(false)))
      .subscribe({
        next: (articles) => {
          const items = this.draftForm.items().value;
          items.set(orderedItems(withExistingItems(items(), this.articles(), articles), articles));
          this.articles.set(articles);
          this.areArticlesLoaded.set(true);
        },
        error: (error: unknown) => this.articlesError.set(toApiProblem(error)),
      });
  }

  /** The sectors and the periods of the current season: one row of prices each. */
  private fetchPriceList(): void {
    if (!this.auth.user()?.propertyId || this.isFetching()) {
      return;
    }
    this.priceListError.set(null);
    this.isFetching.set(true);
    this.fetching = forkJoin([this.sectorRead, this.seasons.periodList(false)])
      .pipe(finalize(() => this.isFetching.set(false)))
      .subscribe({
        next: ([sectors, periods]) => {
          this.setPriceRows(sectors, periods);
          this.isPriceListLoaded.set(true);
        },
        error: (error: unknown) => this.priceListError.set(toApiProblem(error)),
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
      periods.map(({ key }) =>
        Object.assign(new PackagePeriodPrice(), { idSector: id, idPeriod: key }),
      ),
    );
    this.sectors.set(sectors);
    this.periods.set(periods);
    this.draftForm.sectorPrices().value.set(sectorPrices);
    this.draftForm.periodPrices().value.set(periodPrices);
    this.pristine.set(comparableJson(Object.assign(newDraft(), { sectorPrices, periodPrices })));
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.sectorList.destroy();
    this.sectorAreaList.destroy();
    this.fetching?.unsubscribe();
    this.fetchingArticles?.unsubscribe();
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
