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
import { toSignal } from '@angular/core/rxjs-interop';
import {
  MAX_LENGTH,
  TreeValidationResult,
  applyEach,
  applyWhen,
  disabled,
  form,
  metadata,
  readonly,
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
  shareReplay,
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
  clearApiFieldErrors,
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
import { PackageSelectedItem } from '../../../../../entities/settings/catalog/package-selected-item';
import {
  MAX_PACKAGE_NAME_LENGTH,
  PackageTranslation,
} from '../../../../../entities/settings/catalog/package-translation';
import { PackageIntermediatePrice } from '../../../../../entities/settings/catalog/package-intermediate-price';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { noOverlap, requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { SidePanelButtons } from '../../../../shared/ui/dialogs/side-panel/side-panel-buttons';
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
import { byIntermediatePrice } from '../intermediate-prices/intermediate-prices';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { ReadErrorPopup } from '../../../read-error-popup/read-error-popup';
import { VersionConflictPopup } from '../../../version-conflict-popup/version-conflict-popup';

interface PackageFormTexts {
  tabs: {
    general_info: string;
    price_list: string;
  };
}

@Component({
  selector: 'app-package-edit-item-panel',
  imports: [
    Button,
    PackageGeneralInfoTab,
    PackageReducedPrice,
    PriceListTab,
    ReadErrorPopup,
    SidePanel,
    SidePanelButtons,
    TabGroup,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './package-edit-item-panel.html',
  styleUrl: './package-edit-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageEditItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  readonly idPackage = input<string | null>(null);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly updated = output<void>();
  readonly failed = output<ApiProblem>();

  private readonly auth = inject(AuthBehaviour);
  private readonly priceLists = inject(PriceListService);
  private readonly sectorService = inject(SectorService);
  private readonly seasons = inject(SeasonService);
  private readonly language = inject(LanguageBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly PanelTab = PanelTab;
  protected readonly mainLanguage = this.language.current;
  protected readonly selectedTab = signal<number>(PanelTab.GeneralInfo);
  /** Modifica pressed: the fields of both tabs can be changed, until Annulla or Salva. */
  protected readonly isEditing = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isLocked = computed(() => !this.isEditing() || this.isSaving());
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
    readonly(path, () => this.isLocked());
    disabled(path.idSectorAreas, () => this.isLocked());
    disabled(path.idSectors, () => this.isLocked());
    disabled(path.isDefault, () => this.isLocked());
    disabled(path.isDefaultOnline, () => this.isLocked());
    disabled(path.isOnlineEnabled, () => this.isLocked());
    disabled(path.isOnlineOnly, () => this.isLocked());
    disabled(path.isCumulative, () => this.isLocked());
    disabled(path.isPublicHolidaysIncluded, () => this.isLocked());
    disabled(path.isIntermediatePricesIncluded, () => this.isLocked());
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
  /** The values as the API gave them: the form is compared with them. */
  private readonly pristine = signal(newDraft());
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
  /** Saved prices of sectors or periods the panel does not show: sent back as they were. */
  private readonly hiddenSectorPrices = signal<readonly PackageSectorPrice[]>([]);
  private readonly hiddenPeriodPrices = signal<readonly PackagePeriodPrice[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly isEditDisabled = computed(() => this.isLoading() || !this.isLoaded());
  protected readonly packageName = computed(
    () =>
      this.draft()
        .translations.find(({ language }) => language === this.mainLanguage())
        ?.packageName?.trim() ?? '',
  );
  /** Also while the reduced seasonal price box is open: its values are not in the package yet. */
  protected readonly isSaveDisabled = computed(
    () =>
      this.isLoading() || this.draftForm().invalid() || this.reducedSeasonalPriceIndex() !== null,
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly readError = signal<ApiProblem | null>(null);
  protected readonly isConflictShown = signal(false);
  private version: string | null = null;
  /** The save found a newer version: the popup shows once the package has been read again. */
  private isConflictPending = false;
  protected readonly errorMessage = this.errorText.message(this.error);
  private loading?: Subscription;

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      } else {
        untracked(() => this.stopLoading());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() &&
      (comparable(this.draft()) !== comparable(this.pristine()) ||
        this.isReducedSeasonalPriceTyped())
    );
  }

  protected startEdit(): void {
    if (!this.isLoaded()) {
      return;
    }
    this.error.set(null);
    this.isEditing.set(true);
    // What was read already wrong shows at once: it keeps Salva off.
    this.draftForm().markAsTouched();
  }

  /** Annulla: the package goes back to the values read, only to look at. */
  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancelEdit();
    }
  }

  protected save(): void {
    const idPackage = this.idPackage();
    if (
      this.isSaving() ||
      !this.isLoaded() ||
      this.reducedSeasonalPriceIndex() !== null ||
      !idPackage
    ) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(
          this.priceLists.updatePackage(idPackage, this.toRequest(), this.version),
        );
        this.isSaving.set(false);
        this.open.set(false);
        this.updated.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.failed.emit(problem);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          const tab = this.selectedTab();
          this.reset(true);
          this.selectedTab.set(tab);
          return undefined;
        }
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

  /** The form, with the hidden saved prices after the shown ones. */
  private toRequest(): PackageAddItemRequest {
    const draft = this.draft();
    return {
      ...draft,
      sectorPrices: [...draft.sectorPrices, ...this.hiddenSectorPrices()],
      periodPrices: [...draft.periodPrices, ...this.hiddenPeriodPrices()],
    };
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

  private cancelEdit(): void {
    this.draft.set(structuredClone(this.pristine()));
    clearApiFieldErrors(this.draftForm);
    this.draftForm().reset();
    this.reducedSeasonalPriceIndex.set(null);
    this.error.set(null);
    this.isEditing.set(false);
  }

  protected retryRead(): void {
    this.open.set(true);
  }

  /** Closed while the package is being read: its answer, or its error, is not wanted any more. */
  private stopLoading(): void {
    this.loading?.unsubscribe();
    this.isConflictPending = false;
  }

  private reset(isConflictPending = false): void {
    this.stopLoading();
    this.isConflictPending = isConflictPending;
    this.isLoading.set(false);
    this.isLoaded.set(false);
    this.error.set(null);
    this.isEditing.set(false);
    this.isSaving.set(false);
    this.draft.set(newDraft());
    this.pristine.set(newDraft());
    this.draftForm().reset();
    this.reducedSeasonalPrice.set(new PackageReducedSeasonalPrice());
    this.reducedSeasonalPriceForm().reset();
    this.reducedSeasonalPriceIndex.set(null);
    this.sectors.set([]);
    this.periods.set([]);
    this.articles.set([]);
    this.hiddenSectorPrices.set([]);
    this.hiddenPeriodPrices.set([]);
    this.version = null;
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.sectorList.clear();
    this.sectorAreaList.clear();
    this.load();
  }

  /** An item chosen is no longer in the warehouse: the articles are read again, and it goes away. */
  private refreshArticles(): void {
    this.isLoading.set(true);
    this.loading = this.priceLists
      .packageArticles()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (articles) => {
          const items = this.draftForm.items().value;
          items.set(orderedItems(withExistingItems(items(), this.articles(), articles), articles));
          this.articles.set(articles);
        },
        error: (error: unknown) => this.error.set(toApiProblem(error)),
      });
  }

  /** The package, with the sectors and the periods of the current season for its price rows. */
  private load(): void {
    const idPackage = this.idPackage();
    if (!idPackage || !this.auth.user()?.propertyId) {
      return;
    }
    this.loadSectors();
    this.isLoading.set(true);
    this.loading = forkJoin({
      item: this.priceLists.selectedPackage(idPackage),
      sectors: this.sectorRead,
      periods: this.seasons.periodList(false),
      articles: this.priceLists.packageArticles(),
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ item, sectors, periods, articles }) => {
          this.setLoaded(item, sectors, periods, articles);
          this.isLoaded.set(true);
          if (this.isConflictPending) {
            this.isConflictPending = false;
            this.isConflictShown.set(true);
          }
        },
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.isConflictPending = false;
          this.failed.emit(problem);
          this.readError.set(problem);
          this.open.set(false);
        },
      });
  }

  /**
   * The values read, with a name for every language (empty where missing) and a price for every
   * sector and for every sector in every period (the saved one, or empty). The saved prices of
   * other sectors or periods (e.g. of another season) are kept aside.
   */
  private setLoaded(
    item: PackageSelectedItem,
    sectors: readonly ComboboxItem[],
    periods: readonly SeasonPeriodItem[],
    articles: readonly PackageArticle[],
  ): void {
    const draft = Object.assign(newDraft(), {
      translations: SERVICE_LANGUAGES.map((language) =>
        Object.assign(new PackageTranslation(), {
          language,
          packageName:
            item.translations.find((translation) => translation.language === language)
              ?.packageName ?? null,
        }),
      ),
      maxAdditionalItemQuantity: item.maxAdditionalItemQuantity,
      maxPeopleQuantity: item.maxPeopleQuantity,
      items: orderedItems(item.items, articles),
      includedServices: item.includedServices,
      idSectorAreas: [...item.idSectorAreas],
      idSectors: [...item.idSectors],
      isDefault: item.isDefault,
      isDefaultOnline: item.isDefaultOnline,
      isOnlineEnabled: item.isOnlineEnabled,
      isOnlineOnly: item.isOnlineOnly,
      reducedSeasonalPrices: item.reducedSeasonalPrices
        .map((price) =>
          Object.assign(new PackageReducedSeasonalPrice(), price, {
            sectorPrices: [
              ...sectors.map(({ id }) =>
                Object.assign(
                  new PackageSectorPrice(),
                  price.sectorPrices.find((sectorPrice) => sectorPrice.idSector === id),
                  { idSector: id },
                ),
              ),
              ...price.sectorPrices.filter(
                ({ idSector }) => !idSector || !sectors.some(({ id }) => id === idSector),
              ),
            ],
          }),
        )
        .sort((first, second) => (first.startsOn ?? '').localeCompare(second.startsOn ?? '')),
      sectorPrices: sectors.map(({ id }) =>
        Object.assign(
          new PackageSectorPrice(),
          item.sectorPrices.find((price) => price.idSector === id),
          { idSector: id },
        ),
      ),
      periodPrices: sectors.flatMap(({ id }) =>
        periods.map(({ key: idPeriod }) =>
          Object.assign(
            new PackagePeriodPrice(),
            item.periodPrices.find((price) => price.idSector === id && price.idPeriod === idPeriod),
            { idSector: id, idPeriod },
          ),
        ),
      ),
      vatRate: item.vatRate,
      calculationMethod: item.calculationMethod,
      isCumulative: item.isCumulative,
      monthlySetting: item.monthlySetting,
      weekendSetting: item.weekendSetting,
      isPublicHolidaysIncluded: item.isPublicHolidaysIncluded,
      publicHolidayDates: [...item.publicHolidayDates].sort(),
      isIntermediatePricesIncluded: item.isIntermediatePricesIncluded,
      intermediatePrices: item.intermediatePrices
        .map((price) => Object.assign(new PackageIntermediatePrice(), price))
        .sort(byIntermediatePrice),
    });
    const idSectors = new Set(sectors.map(({ id }) => id));
    const idPeriods = new Set(periods.map(({ key }) => key));
    this.hiddenSectorPrices.set(
      item.sectorPrices.filter(({ idSector }) => !idSector || !idSectors.has(idSector)),
    );
    this.hiddenPeriodPrices.set(
      item.periodPrices.filter(
        ({ idSector, idPeriod }) =>
          !idSector || !idSectors.has(idSector) || idPeriod === null || !idPeriods.has(idPeriod),
      ),
    );
    this.sectors.set(sectors);
    this.periods.set(periods);
    this.articles.set(articles);
    this.version = item.version;
    this.draft.set(draft);
    this.pristine.set(structuredClone(draft));
    this.draftForm().reset();
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.loading?.unsubscribe();
    this.sectorList.destroy();
    this.sectorAreaList.destroy();
  }
}

/** The order of the sectors and of the areas does not count as a change. */
function comparable(values: PackageAddItemRequest): string {
  return comparableJson({
    ...values,
    idSectors: [...values.idSectors].sort(),
    idSectorAreas: [...values.idSectorAreas].sort(),
  });
}

/** Empty, with a name for every language: the user's one is the main. */
function newDraft(): PackageAddItemRequest {
  return Object.assign(new PackageAddItemRequest(), {
    translations: SERVICE_LANGUAGES.map((language) =>
      Object.assign(new PackageTranslation(), { language }),
    ),
  });
}
