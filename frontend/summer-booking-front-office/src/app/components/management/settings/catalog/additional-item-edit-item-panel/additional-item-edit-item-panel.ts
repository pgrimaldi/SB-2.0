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
  FormField,
  TreeValidationResult,
  disabled,
  form,
  readonly,
  required,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription, finalize, firstValueFrom, forkJoin, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
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
import { PriceListTab } from '../price-list-tab/price-list-tab';
import { byIntermediatePrice } from '../intermediate-prices/intermediate-prices';

@Component({
  selector: 'app-additional-item-edit-item-panel',
  imports: [
    Button,
    Checkbox,
    FilledMultiSelect,
    FilledSelect,
    FormField,
    PriceListTab,
    SectionTitle,
    SidePanel,
    SidePanelButtons,
    TranslatePipe,
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

  private readonly auth = inject(AuthBehaviour);
  private readonly priceLists = inject(PriceListService);
  private readonly warehouse = inject(WarehouseService);
  private readonly sectorService = inject(SectorService);
  private readonly seasons = inject(SeasonService);
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
  });
  /** The values as the API gave them: the form is compared with them. */
  private readonly pristine = signal(new AdditionalItemAddItemRequest());
  private readonly articles = signal<readonly ComboboxItem[]>([]);
  protected readonly articlesLoadError = signal<string | null>(null);
  /** The name of the chosen article, in the rows of the intermediate prices. */
  protected readonly articleName = computed(
    () => this.articles().find(({ id }) => id === this.draft().idArticle)?.value ?? '',
  );
  protected readonly articleOptions = computed<readonly SelectOption[]>(() =>
    this.articles().map(({ id, value }) => ({ value: id, label: value })),
  );
  private readonly packages = signal<readonly ComboboxItem[]>([]);
  protected readonly packagesLoadError = signal<string | null>(null);
  protected readonly packageOptions = computed<readonly SelectOption[]>(() =>
    this.packages().map(({ id, value }) => ({ value: id, label: value })),
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
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private loadings = new Subscription();

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && JSON.stringify(this.draft()) !== JSON.stringify(this.pristine());
  }

  protected startEdit(): void {
    this.error.set(null);
    this.isEditing.set(true);
  }

  /** Annulla: the item goes back to the values read, only to look at. */
  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.draft.set(structuredClone(this.pristine()));
      this.draftForm().reset();
      this.error.set(null);
      this.isEditing.set(false);
    }
  }

  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    const idAdditionalItem = this.idAdditionalItem();
    if (this.isSaving() || !idProperty || !idAdditionalItem) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(
          this.priceLists.updateAdditionalItem(idProperty, idAdditionalItem, this.toRequest()),
        );
        this.isSaving.set(false);
        this.open.set(false);
        this.updated.emit();
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

  /** The form, with the hidden saved prices after the shown ones. */
  private toRequest(): AdditionalItemAddItemRequest {
    const draft = this.draft();
    return {
      ...draft,
      sectorPrices: [...draft.sectorPrices, ...this.hiddenSectorPrices()],
      periodPrices: [...draft.periodPrices, ...this.hiddenPeriodPrices()],
    };
  }

  private reset(): void {
    this.loadings.unsubscribe();
    this.loadings = new Subscription();
    this.isLoading.set(false);
    this.isLoaded.set(false);
    this.error.set(null);
    this.isEditing.set(false);
    this.isSaving.set(false);
    this.draft.set(new AdditionalItemAddItemRequest());
    this.pristine.set(new AdditionalItemAddItemRequest());
    this.draftForm().reset();
    this.articles.set([]);
    this.articlesLoadError.set(null);
    this.packages.set([]);
    this.packagesLoadError.set(null);
    this.sectors.set([]);
    this.periods.set([]);
    this.hiddenSectorPrices.set([]);
    this.hiddenPeriodPrices.set([]);
    this.load();
  }

  /** The item with the sectors and the periods of its price rows, and the choices of the dropdowns. */
  private load(): void {
    const idAdditionalItem = this.idAdditionalItem();
    const idProperty = this.auth.user()?.propertyId;
    if (!idAdditionalItem || !idProperty) {
      return;
    }
    this.loadings.add(
      this.warehouse.articleCombobox(idProperty).subscribe({
        next: (articles) => this.articles.set(articles),
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.articles.set([]);
          this.articlesLoadError.set(this.errorText.instant(problem.code, problem.args));
        },
      }),
    );
    this.loadings.add(
      this.priceLists.packageCombobox(idProperty).subscribe({
        next: (packages) => this.packages.set(packages),
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.packages.set([]);
          this.packagesLoadError.set(this.errorText.instant(problem.code, problem.args));
        },
      }),
    );
    this.isLoading.set(true);
    this.loadings.add(
      forkJoin({
        item: this.priceLists.selectedAdditionalItem(idAdditionalItem),
        sectors: this.sectorService.sectorCombobox(idProperty),
        periods: this.seasons.periodList(idProperty, false),
      })
        .pipe(finalize(() => this.isLoading.set(false)))
        .subscribe({
          next: ({ item, sectors, periods }) => {
            this.setLoaded(item, sectors, periods);
            this.isLoaded.set(true);
          },
          error: (error: unknown) => this.error.set(toApiProblem(error)),
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
        periods.map(({ idPeriod }) =>
          Object.assign(
            new PackagePeriodPrice(),
            item.periodPrices.find((price) => price.idSector === id && price.idPeriod === idPeriod),
            { idSector: id, idPeriod },
          ),
        ),
      ),
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
    const idPeriods = new Set(periods.map(({ idPeriod }) => idPeriod));
    this.hiddenSectorPrices.set(
      item.sectorPrices.filter(({ idSector }) => !idSector || !idSectors.has(idSector)),
    );
    this.hiddenPeriodPrices.set(
      item.periodPrices.filter(
        ({ idSector, idPeriod }) =>
          !idSector || !idSectors.has(idSector) || !idPeriod || !idPeriods.has(idPeriod),
      ),
    );
    this.sectors.set(sectors);
    this.periods.set(periods);
    this.draft.set(draft);
    this.pristine.set(structuredClone(draft));
    this.draftForm().reset();
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.loadings.unsubscribe();
  }
}
