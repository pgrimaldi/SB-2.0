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
  FormField,
  TreeValidationResult,
  disabled,
  form,
  readonly,
  required,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, finalize, firstValueFrom, forkJoin } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { DropdownList } from '../../../../../behaviours/forms/dropdown-list';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import {
  apiFieldError,
  apiFieldErrorsOrGeneral,
  clearApiFieldErrors,
} from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { AdditionalItemAddItemRequest } from '../../../../../entities/settings/catalog/additional-item-add-item-request';
import { AdditionalItemSelectedItem } from '../../../../../entities/settings/catalog/additional-item-selected-item';
import { PackagePeriodPrice } from '../../../../../entities/settings/catalog/package-period-price';
import { PackageSectorPrice } from '../../../../../entities/settings/catalog/package-sector-price';
import { PackageIntermediatePrice } from '../../../../../entities/settings/catalog/package-intermediate-price';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { SidePanelButtons } from '../../../../shared/ui/dialogs/side-panel/side-panel-buttons';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import {
  intermediatePricesOf,
  maxPrices,
  maxPublicHolidays,
  periodPricesOf,
  sectorPricesOf,
  vatRange,
} from '../price-limits';
import { optionsWithMissing } from '../options-with-missing';
import { PriceListTab } from '../price-list-tab/price-list-tab';
import { byIntermediatePrice } from '../intermediate-prices/intermediate-prices';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { ReadErrorPopup } from '../../../read-error-popup/read-error-popup';
import { VersionConflictPopup } from '../../../version-conflict-popup/version-conflict-popup';

interface AdditionalItemFormTexts {
  missing_article: string;
  missing_package: string;
}

@Component({
  selector: 'app-additional-item-edit-item-panel',
  imports: [
    Button,
    Checkbox,
    FilledMultiSelect,
    FilledSelect,
    FormField,
    PriceListTab,
    ReadErrorPopup,
    SectionTitle,
    SidePanel,
    SidePanelButtons,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './additional-item-edit-item-panel.html',
  styleUrl: './additional-item-edit-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdditionalItemEditItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  readonly idAdditionalItem = input<string | null>(null);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly updated = output<void>();
  readonly failed = output<ApiProblem>();

  private readonly auth = inject(AuthBehaviour);
  private readonly priceLists = inject(PriceListService);
  private readonly warehouse = inject(WarehouseService);
  private readonly sectorService = inject(SectorService);
  private readonly seasons = inject(SeasonService);
  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  /** Modifica pressed: the fields can be changed, until Annulla or Salva. */
  protected readonly isEditing = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isLocked = computed(() => !this.isEditing() || this.isSaving());
  private readonly draft = signal(new AdditionalItemAddItemRequest());
  protected readonly draftForm = form(this.draft, (path) => {
    readonly(path, () => this.isLocked());
    disabled(path.idPackages, () => this.isLocked());
    disabled(path.isOnlineEnabled, () => this.isLocked());
    disabled(path.isOnlineOnly, () => this.isLocked());
    disabled(path.isMapAssignable, () => this.isLocked());
    disabled(path.isCumulative, () => this.isLocked());
    disabled(path.isPublicHolidaysIncluded, () => this.isLocked());
    disabled(path.isIntermediatePricesIncluded, () => this.isLocked());
    required(path.idArticle);
    vatRange(path.vatRate);
    maxPublicHolidays(path.publicHolidayDates);
    maxPrices(path.sectorPrices, sectorPricesOf);
    maxPrices(path.periodPrices, periodPricesOf);
    maxPrices(path.intermediatePrices, intermediatePricesOf);
  });
  /** The values as the API gave them: the form is compared with them. */
  private readonly pristine = signal(new AdditionalItemAddItemRequest());
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.catalog.additional_items.form',
    ) as Observable<AdditionalItemFormTexts>,
    { initialValue: null },
  );
  protected readonly articles = new DropdownList(
    () => this.warehouse.articleCombobox(),
    this.errorText,
  );
  /** The name of the chosen article, in the rows of the intermediate prices. */
  protected readonly articleName = computed(
    () => this.articles.items().find(({ id }) => id === this.draft().idArticle)?.value ?? '',
  );
  /** Also the chosen one when it is no longer there, for the user to choose another. */
  protected readonly articleOptions = computed<readonly SelectOption[]>(() => {
    const { idArticle } = this.draft();
    return optionsWithMissing(
      this.articles,
      idArticle === null ? [] : [idArticle],
      this.texts()?.missing_article ?? '',
    );
  });
  protected readonly packages = new DropdownList(
    () => this.priceLists.packageCombobox(),
    this.errorText,
  );
  /** Also the chosen ones that are no longer there, for the user to take away. */
  protected readonly packageOptions = computed<readonly SelectOption[]>(() =>
    optionsWithMissing(this.packages, this.draft().idPackages, this.texts()?.missing_package ?? ''),
  );
  protected readonly sectors = signal<readonly ComboboxItem[]>([]);
  protected readonly periods = signal<readonly SeasonPeriodItem[]>([]);
  /** Saved prices of sectors or periods the panel does not show: sent back as they were. */
  private readonly hiddenSectorPrices = signal<readonly PackageSectorPrice[]>([]);
  private readonly hiddenPeriodPrices = signal<readonly PackagePeriodPrice[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly isEditDisabled = computed(() => this.isLoading() || !this.isLoaded());
  protected readonly isSaveDisabled = computed(
    () => this.isLoading() || this.draftForm().invalid(),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly readError = signal<ApiProblem | null>(null);
  protected readonly isConflictShown = signal(false);
  private version: string | null = null;
  /** The save found a newer version: the popup shows once the item has been read again. */
  private isConflictPending = false;
  protected readonly errorMessage = this.errorText.message(this.error);
  private loadings = new Subscription();

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      } else {
        untracked(() => this.stopLoadings());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && comparable(this.draft()) !== comparable(this.pristine());
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

  /** Annulla: the item goes back to the values read, only to look at. */
  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.draft.set(structuredClone(this.pristine()));
      clearApiFieldErrors(this.draftForm);
      this.draftForm().reset();
      this.error.set(null);
      this.isEditing.set(false);
    }
  }

  protected save(): void {
    const idAdditionalItem = this.idAdditionalItem();
    if (this.isSaving() || !this.isLoaded() || !this.auth.user()?.propertyId || !idAdditionalItem) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(
          this.priceLists.updateAdditionalItem(idAdditionalItem, this.toRequest(), this.version),
        );
        this.isSaving.set(false);
        this.open.set(false);
        this.updated.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.failed.emit(problem);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.reset(true);
          return undefined;
        }
        if (problem.code === ApiErrorCode.InventoryItemNotFound) {
          this.articles.load();
          return apiFieldError(this.draftForm.idArticle, problem.code);
        }
        if (problem.code === ApiErrorCode.PricingPackageNotFound) {
          this.packages.load();
          return apiFieldError(this.draftForm.idPackages, problem.code);
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

  protected async leave(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.open.set(false);
    }
  }

  /** The form, with the hidden saved prices after the shown ones. */
  private toRequest(): AdditionalItemAddItemRequest {
    const draft = this.draft();
    return {
      ...draft,
      sectorPrices: [...draft.sectorPrices, ...this.hiddenSectorPrices()],
      periodPrices: [...draft.periodPrices, ...this.hiddenPeriodPrices()],
    };
  }

  protected retryRead(): void {
    this.open.set(true);
  }

  /** Closed while the item is being read: its answer, or its error, is not wanted any more. */
  private stopLoadings(): void {
    this.loadings.unsubscribe();
    this.loadings = new Subscription();
    this.isConflictPending = false;
  }

  private reset(isConflictPending = false): void {
    this.stopLoadings();
    this.isConflictPending = isConflictPending;
    this.isLoading.set(false);
    this.isLoaded.set(false);
    this.error.set(null);
    this.isEditing.set(false);
    this.isSaving.set(false);
    this.draft.set(new AdditionalItemAddItemRequest());
    this.pristine.set(new AdditionalItemAddItemRequest());
    this.draftForm().reset();
    this.articles.clear();
    this.packages.clear();
    this.sectors.set([]);
    this.periods.set([]);
    this.hiddenSectorPrices.set([]);
    this.hiddenPeriodPrices.set([]);
    this.version = null;
    this.load();
  }

  /** The item with the sectors and the periods of its price rows, and the choices of the dropdowns. */
  private load(): void {
    const idAdditionalItem = this.idAdditionalItem();
    if (!idAdditionalItem || !this.auth.user()?.propertyId) {
      return;
    }
    this.articles.load();
    this.packages.load();
    this.isLoading.set(true);
    this.loadings.add(
      forkJoin({
        item: this.priceLists.selectedAdditionalItem(idAdditionalItem),
        sectors: this.sectorService.sectorCombobox(),
        periods: this.seasons.periodList(false),
      })
        .pipe(finalize(() => this.isLoading.set(false)))
        .subscribe({
          next: ({ item, sectors, periods }) => {
            this.setLoaded(item, sectors, periods);
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
        }),
    );
  }

  /**
   * The values read, with a price for every sector and for every sector in every period (the saved
   * one, or empty). The saved prices of other sectors or periods (e.g. of another season) are kept
   * aside.
   */
  private setLoaded(
    item: AdditionalItemSelectedItem,
    sectors: readonly ComboboxItem[],
    periods: readonly SeasonPeriodItem[],
  ): void {
    const draft = Object.assign(new AdditionalItemAddItemRequest(), {
      idArticle: item.idArticle,
      idPackages: [...item.idPackages],
      isOnlineEnabled: item.isOnlineEnabled,
      isOnlineOnly: item.isOnlineOnly,
      isMapAssignable: item.isMapAssignable,
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
    this.version = item.version;
    this.draft.set(draft);
    this.pristine.set(structuredClone(draft));
    this.draftForm().reset();
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.loadings.unsubscribe();
    this.articles.destroy();
    this.packages.destroy();
  }
}

/** The order of the packages does not count as a change. */
function comparable(values: AdditionalItemAddItemRequest): string {
  return comparableJson({ ...values, idPackages: [...values.idPackages].sort() });
}
