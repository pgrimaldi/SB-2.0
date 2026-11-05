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
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { BarServiceIcon } from '../../../../../entities/enums/bar-service-icon';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { BarServiceAddItemRequest } from '../../../../../entities/settings/services/bar-service-add-item-request';
import { BarServiceSelectedGeneralInfo } from '../../../../../entities/settings/services/bar-service-selected-general-info';
import { BarServiceSelectedPrices } from '../../../../../entities/settings/services/bar-service-selected-prices';
import { BarServiceUpdateBookingRequest } from '../../../../../entities/settings/services/bar-service-update-booking-request';
import { BarServiceUpdateGeneralRequest } from '../../../../../entities/settings/services/bar-service-update-general-request';
import { BarServiceUpdatePricesRequest } from '../../../../../entities/settings/services/bar-service-update-prices-request';
import { ServiceBookingOption } from '../../../../../entities/settings/services/service-booking-option';
import { ServicePeriodPrice } from '../../../../../entities/settings/services/service-period-price';
import { ServiceTranslation } from '../../../../../entities/settings/services/service-translation';
import { ServiceVariant } from '../../../../../entities/settings/services/service-variant';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { SidePanelButtons } from '../../../../shared/ui/dialogs/side-panel/side-panel-buttons';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { ServiceBookingsTab } from '../service-bookings-tab/service-bookings-tab';
import { ServicePricesTab } from '../service-prices-tab/service-prices-tab';
import { ServiceGeneralInfoTab } from '../service-general-info-tab/service-general-info-tab';
import { SERVICE_LANGUAGES } from '../shared/services-translations/services-translations';

interface ServiceFormTexts {
  tabs: {
    general_info: string;
    prices: string;
    bookings: string;
  };
}

/** The tabs of the panel, in their order. */
enum PanelTab {
  GeneralInfo,
  Prices,
  Bookings,
}

/** The fields of the form each tab shows, reads and saves. */
const TAB_FIELDS: Readonly<Record<PanelTab, readonly (keyof BarServiceAddItemRequest)[]>> = {
  [PanelTab.GeneralInfo]: ['translations', 'category', 'icon', 'isHighlighted'],
  [PanelTab.Prices]: [
    'price',
    'vatRate',
    'variants',
    'isPeriodPricingEnabled',
    'periodPrices',
    'idSectors',
    'isOnlinePurchaseEnabled',
  ],
  [PanelTab.Bookings]: ['bookingOptions'],
};

@Component({
  selector: 'app-bar-service-edit-item-panel',
  imports: [
    Button,
    ServiceBookingsTab,
    ServiceGeneralInfoTab,
    ServicePricesTab,
    SidePanel,
    SidePanelButtons,
    TabGroup,
    TranslatePipe,
  ],
  templateUrl: './bar-service-edit-item-panel.html',
  styleUrl: './bar-service-edit-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarServiceEditItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  readonly idBarService = input<string | null>(null);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly updated = output<void>();

  private readonly auth = inject(AuthBehaviour);
  private readonly catalog = inject(CatalogService);
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
  /** The tab marked in the bar: back to `selectedTab` when the user stays on the tab being edited. */
  protected readonly tabIndex = signal<number>(PanelTab.GeneralInfo);
  /** Modifica pressed: the fields of the tab can be changed, until Annulla or Salva. */
  protected readonly isEditing = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isLocked = computed(() => !this.isEditing() || this.isSaving());
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.services.form',
    ) as Observable<ServiceFormTexts>,
    { initialValue: null },
  );
  protected readonly tabs = computed<readonly TabGroupTab[]>(() => [
    { label: this.texts()?.tabs.general_info ?? '', widthPercent: 34 },
    { label: this.texts()?.tabs.prices ?? '', widthPercent: 33 },
    { label: this.texts()?.tabs.bookings ?? '', widthPercent: 33 },
  ]);
  private readonly draft = signal(newDraft());
  protected readonly draftForm = form(this.draft, (path) => {
    readonly(path, () => this.isLocked());
    disabled(path.isHighlighted, () => this.isLocked());
    disabled(path.isPeriodPricingEnabled, () => this.isLocked());
    disabled(path.idSectors, () => this.isLocked());
    disabled(path.isOnlinePurchaseEnabled, () => this.isLocked());
    applyEach(path.translations, (translation) => {
      applyWhen(
        translation,
        ({ valueOf }) =>
          this.selectedTab() === PanelTab.GeneralInfo &&
          valueOf(translation.language) === this.mainLanguage(),
        (main) => {
          requiredText(main.serviceName);
          requiredText(main.description);
        },
      );
    });
  });
  /** The values as the APIs gave them, tab by tab: the form is compared with them. */
  private readonly pristine = signal(newDraft());
  protected readonly periods = signal<readonly SeasonPeriodItem[]>([]);
  /** The tabs read (or being read) since the panel opened: changing tab keeps their data. */
  private readonly loadedTabs = signal<ReadonlySet<PanelTab>>(new Set());
  /** The tabs whose data are being read: their fields cannot be used meanwhile. */
  private readonly loadingTabs = signal<ReadonlySet<PanelTab>>(new Set());
  protected readonly isEditDisabled = computed(() => {
    const tab = this.selectedTab() as PanelTab;
    return this.loadingTabs().has(tab) || !this.loadedTabs().has(tab);
  });
  protected readonly isSaveDisabled = computed(
    () => this.loadingTabs().has(this.selectedTab()) || this.draftForm().invalid(),
  );
  /** The image chosen from the computer, instead of an icon of the list. */
  private readonly image = signal<File | null>(null);
  protected readonly imagePreviewUrl = signal<string | null>(null);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private readonly loadings = new Map<PanelTab, Subscription>();

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
      (this.image() !== null || JSON.stringify(this.draft()) !== JSON.stringify(this.pristine()))
    );
  }

  protected isLoading(tab: PanelTab): boolean {
    return this.loadingTabs().has(tab);
  }

  protected chooseIcon(icon: BarServiceIcon): void {
    this.draftForm.icon().value.set(icon);
    this.setImage(null);
  }

  protected chooseImage(image: File): void {
    this.draftForm.icon().value.set(null);
    this.setImage(image);
  }

  protected startEdit(): void {
    this.error.set(null);
    this.isEditing.set(true);
  }

  /** Annulla: the tab goes back to the values read, only to look at. */
  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancelEdit();
    }
  }

  protected cancelEdit(): void {
    const tab = this.selectedTab() as PanelTab;
    const pristine = structuredClone(this.pristine());
    this.draft.update((draft) => ({ ...draft, ...pick(pristine, tab) }));
    if (tab === PanelTab.GeneralInfo) {
      this.setImage(null);
    }
    this.draftForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  /**
   * Another tab while editing: with changes, first the question about losing them; staying, the bar
   * marks the tab being edited again.
   */
  protected async requestTab(index: number): Promise<void> {
    const current = this.selectedTab();
    if (index === current) {
      return;
    }
    if (this.isEditing()) {
      if (!(await this.unsaved.confirmDiscard(this))) {
        this.tabIndex.set(current);
        return;
      }
      this.cancelEdit();
    }
    this.selectedTab.set(index);
    this.tabIndex.set(index);
    this.loadTab(index);
  }

  /** Salva: only the tab being edited, with its API. */
  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    const idBarService = this.idBarService();
    if (this.isSaving() || !idProperty || !idBarService) {
      return;
    }
    const tab = this.selectedTab() as PanelTab;
    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.update(tab, idProperty, idBarService));
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

  /** The data of a tab, read from its API the first time the tab is opened. */
  private loadTab(tab: PanelTab): void {
    const idBarService = this.idBarService();
    const idProperty = this.auth.user()?.propertyId;
    if (!idBarService || !idProperty || this.loadedTabs().has(tab)) {
      return;
    }
    this.loadedTabs.update((tabs) => new Set(tabs).add(tab));
    this.error.set(null);
    this.setLoading(tab, true);
    if (tab === PanelTab.GeneralInfo) {
      this.loadings.set(
        tab,
        this.catalog
          .barServiceGeneralInfo(idBarService)
          .pipe(finalize(() => this.setLoading(tab, false)))
          .subscribe({
            next: (info) => this.setGeneralInfo(info),
            error: (error: unknown) => this.loadFailed(tab, error),
          }),
      );
    } else if (tab === PanelTab.Prices) {
      this.loadings.set(
        tab,
        forkJoin({
          prices: this.catalog.barServicePrices(idBarService),
          periods: this.seasons.periodList(idProperty, false),
        })
          .pipe(finalize(() => this.setLoading(tab, false)))
          .subscribe({
            next: ({ prices, periods }) => this.setPrices(prices, periods),
            error: (error: unknown) => this.loadFailed(tab, error),
          }),
      );
    } else {
      this.loadings.set(
        tab,
        this.catalog
          .barServiceBookingOptions(idBarService)
          .pipe(finalize(() => this.setLoading(tab, false)))
          .subscribe({
            next: (options) => this.setBookingOptions(options),
            error: (error: unknown) => this.loadFailed(tab, error),
          }),
      );
    }
  }

  /** The tab is read again the next time it is opened. */
  private loadFailed(tab: PanelTab, error: unknown): void {
    this.loadedTabs.update((tabs) => {
      const next = new Set(tabs);
      next.delete(tab);
      return next;
    });
    this.error.set(toApiProblem(error));
  }

  /** The update API of the tab, with the fields of that tab only. */
  private update(tab: PanelTab, idProperty: string, idBarService: string): Observable<void> {
    const draft = this.draft();
    if (tab === PanelTab.GeneralInfo) {
      return this.catalog.updateBarServiceGeneral(
        idProperty,
        Object.assign(new BarServiceUpdateGeneralRequest(), { idBarService, ...pick(draft, tab) }),
      );
    }
    if (tab === PanelTab.Prices) {
      return this.catalog.updateBarServicePrices(
        idProperty,
        Object.assign(new BarServiceUpdatePricesRequest(), { idBarService, ...pick(draft, tab) }),
      );
    }
    return this.catalog.updateBarServiceBooking(
      idProperty,
      Object.assign(new BarServiceUpdateBookingRequest(), { idBarService, ...pick(draft, tab) }),
    );
  }

  private reset(): void {
    this.loadings.forEach((loading) => loading.unsubscribe());
    this.loadings.clear();
    this.loadedTabs.set(new Set());
    this.loadingTabs.set(new Set());
    this.error.set(null);
    this.isEditing.set(false);
    this.isSaving.set(false);
    this.draft.set(newDraft());
    this.pristine.set(newDraft());
    this.draftForm().reset();
    this.periods.set([]);
    this.setImage(null);
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.tabIndex.set(PanelTab.GeneralInfo);
    this.loadTab(PanelTab.GeneralInfo);
  }

  /** Name and description in every language, also those the API leaves out (empty). */
  private setGeneralInfo(info: BarServiceSelectedGeneralInfo): void {
    this.applyLoaded({
      translations: SERVICE_LANGUAGES.map((language) => {
        const translation = info.translations.find((item) => item.language === language);
        return Object.assign(new ServiceTranslation(), {
          language,
          serviceName: translation?.serviceName ?? null,
          description: translation?.description ?? null,
        });
      }),
      category: info.category,
      icon: info.icon,
      isHighlighted: info.isHighlighted ?? false,
    });
  }

  /** A price for each period of the current season: the saved one, or empty. */
  private setPrices(prices: BarServiceSelectedPrices, periods: readonly SeasonPeriodItem[]): void {
    this.periods.set(periods);
    this.applyLoaded({
      price: prices.price,
      vatRate: prices.vatRate,
      variants: prices.variants.map(({ variantName, price }) =>
        Object.assign(new ServiceVariant(), { variantName, price }),
      ),
      isPeriodPricingEnabled: prices.isPeriodPricingEnabled ?? false,
      periodPrices: periods.map(({ idPeriod }) =>
        Object.assign(new ServicePeriodPrice(), {
          idPeriod,
          price: prices.periodPrices.find((item) => item.idPeriod === idPeriod)?.price ?? null,
        }),
      ),
      idSectors: [...prices.idSectors],
      isOnlinePurchaseEnabled: prices.isOnlinePurchaseEnabled ?? false,
    });
  }

  private setBookingOptions(options: readonly ServiceBookingOption[]): void {
    this.applyLoaded({
      bookingOptions: options.map((option) => Object.assign(new ServiceBookingOption(), option)),
    });
  }

  /** Values read from an API: into the form, and into what the form is compared with. */
  private applyLoaded(values: Partial<BarServiceAddItemRequest>): void {
    this.draft.update((draft) => ({ ...draft, ...values }));
    this.pristine.update((pristine) => ({ ...pristine, ...structuredClone(values) }));
  }

  private setLoading(tab: PanelTab, isLoading: boolean): void {
    this.loadingTabs.update((tabs) => {
      const next = new Set(tabs);
      if (isLoading) {
        next.add(tab);
      } else {
        next.delete(tab);
      }
      return next;
    });
  }

  private setImage(image: File | null): void {
    const previewUrl = this.imagePreviewUrl();
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }
    this.image.set(image);
    this.imagePreviewUrl.set(image ? URL.createObjectURL(image) : null);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.loadings.forEach((loading) => loading.unsubscribe());
    this.setImage(null);
  }
}

/** Empty, with a name and a description for every language: the user's one is the main. */
function newDraft(): BarServiceAddItemRequest {
  return Object.assign(new BarServiceAddItemRequest(), {
    translations: SERVICE_LANGUAGES.map((language) =>
      Object.assign(new ServiceTranslation(), { language }),
    ),
  });
}

/** The values of the fields of one tab. */
function pick(values: BarServiceAddItemRequest, tab: PanelTab): Partial<BarServiceAddItemRequest> {
  return Object.fromEntries(TAB_FIELDS[tab].map((field) => [field, values[field]]));
}
