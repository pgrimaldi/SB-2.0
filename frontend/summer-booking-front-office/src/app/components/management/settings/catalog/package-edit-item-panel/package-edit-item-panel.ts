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
  disabled,
  form,
  readonly,
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
import { PackageSelectedItem } from '../../../../../entities/settings/catalog/package-selected-item';
import { PackageTranslation } from '../../../../../entities/settings/catalog/package-translation';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { SidePanelButtons } from '../../../../shared/ui/dialogs/side-panel/side-panel-buttons';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { SERVICE_LANGUAGES } from '../../services/shared/services-translations/services-translations';
import { PackageGeneralInfoTab } from '../package-general-info-tab/package-general-info-tab';
import { PriceListTab } from '../price-list-tab/price-list-tab';

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
  selector: 'app-package-edit-item-panel',
  imports: [
    Button,
    PackageGeneralInfoTab,
    PriceListTab,
    SidePanel,
    SidePanelButtons,
    TabGroup,
    TranslatePipe,
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
  /** The values as the API gave them: the form is compared with them. */
  private readonly pristine = signal(newDraft());
  protected readonly sectors = signal<readonly ComboboxItem[]>([]);
  protected readonly periods = signal<readonly SeasonPeriodItem[]>([]);
  /** Saved prices of sectors or periods the panel does not show: sent back as they were. */
  private readonly hiddenSectorPrices = signal<readonly PackageSectorPrice[]>([]);
  private readonly hiddenPeriodPrices = signal<readonly PackagePeriodPrice[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly isEditDisabled = computed(() => this.isLoading() || !this.isLoaded());
  /** Also while the reduced seasonal price box is open: its values are not in the package yet. */
  protected readonly isSaveDisabled = computed(
    () => this.isLoading() || this.draftForm().invalid() || this.isReducedSeasonalPriceOpen(),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private loading?: Subscription;

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() &&
      (JSON.stringify(this.draft()) !== JSON.stringify(this.pristine()) ||
        this.isReducedSeasonalPriceTyped())
    );
  }

  protected startEdit(): void {
    this.error.set(null);
    this.isEditing.set(true);
  }

  /** Annulla: the package goes back to the values read, only to look at. */
  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancelEdit();
    }
  }

  protected save(): void {
    const idPackage = this.idPackage();
    if (this.isSaving() || this.isReducedSeasonalPriceOpen() || !idPackage) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.priceLists.updatePackage(idPackage, this.toRequest()));
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
    const shown = this.draft().reducedSeasonalPrice ?? new PackageReducedSeasonalPrice();
    return (
      this.isReducedSeasonalPriceOpen() &&
      JSON.stringify(this.reducedSeasonalPrice()) !== JSON.stringify(shown)
    );
  }

  private cancelEdit(): void {
    this.draft.set(structuredClone(this.pristine()));
    this.draftForm().reset();
    this.isReducedSeasonalPriceOpen.set(false);
    this.error.set(null);
    this.isEditing.set(false);
  }

  private reset(): void {
    this.loading?.unsubscribe();
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
    this.isReducedSeasonalPriceOpen.set(false);
    this.sectors.set([]);
    this.periods.set([]);
    this.hiddenSectorPrices.set([]);
    this.hiddenPeriodPrices.set([]);
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.load();
  }

  /** The package, with the sectors and the periods of the current season for its price rows. */
  private load(): void {
    const idPackage = this.idPackage();
    const idProperty = this.auth.user()?.propertyId;
    if (!idPackage || !idProperty) {
      return;
    }
    this.isLoading.set(true);
    this.loading = forkJoin({
      item: this.priceLists.selectedPackage(idPackage),
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
      umbrellaQuantity: item.umbrellaQuantity,
      sunbedQuantity: item.sunbedQuantity,
      deckchairQuantity: item.deckchairQuantity,
      directorChairQuantity: item.directorChairQuantity,
      cabinQuantity: item.cabinQuantity,
      tentQuantity: item.tentQuantity,
      includedServices: item.includedServices,
      idSectorAreas: [...item.idSectorAreas],
      idSectors: [...item.idSectors],
      isDefault: item.isDefault,
      isDefaultOnline: item.isDefaultOnline,
      isOnlineEnabled: item.isOnlineEnabled,
      isOnlineOnly: item.isOnlineOnly,
      reducedSeasonalPrice:
        item.reducedSeasonalPrice &&
        Object.assign(new PackageReducedSeasonalPrice(), item.reducedSeasonalPrice),
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
    this.loading?.unsubscribe();
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
