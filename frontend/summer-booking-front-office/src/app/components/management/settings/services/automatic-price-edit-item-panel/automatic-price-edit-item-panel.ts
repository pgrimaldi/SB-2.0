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
import { StandardServiceIcon } from '../../../../../entities/enums/standard-service-icon';
import { AutomaticPriceAddItemRequest } from '../../../../../entities/settings/services/automatic-price-add-item-request';
import { AutomaticPriceSelectedGeneralInfo } from '../../../../../entities/settings/services/automatic-price-selected-general-info';
import { AutomaticPriceSelectedPricesValidity } from '../../../../../entities/settings/services/automatic-price-selected-prices-validity';
import { AutomaticPriceServices } from '../../../../../entities/settings/services/automatic-price-services';
import { AutomaticPriceTimeSlot } from '../../../../../entities/settings/services/automatic-price-time-slot';
import { AutomaticPriceUpdateGeneralRequest } from '../../../../../entities/settings/services/automatic-price-update-general-request';
import { AutomaticPriceUpdatePricesValidityRequest } from '../../../../../entities/settings/services/automatic-price-update-prices-validity-request';
import { AutomaticPriceValidityPeriod } from '../../../../../entities/settings/services/automatic-price-validity-period';
import { ServiceTranslation } from '../../../../../entities/settings/services/service-translation';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { SidePanelButtons } from '../../../../shared/ui/dialogs/side-panel/side-panel-buttons';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { AutomaticPriceGeneralInfoTab } from '../automatic-price-general-info-tab/automatic-price-general-info-tab';
import { AutomaticPricePricesValidityTab } from '../automatic-price-prices-validity-tab/automatic-price-prices-validity-tab';
import { SERVICE_LANGUAGES } from '../shared/services-translations/services-translations';

interface ServiceFormTexts {
  tabs: {
    general_info: string;
    prices_validity: string;
  };
}

/** The tabs of the panel, in their order. */
enum PanelTab {
  GeneralInfo,
  PricesValidity,
}

/** The fields of the form each tab shows, reads and saves. */
const TAB_FIELDS: Readonly<Record<PanelTab, readonly (keyof AutomaticPriceAddItemRequest)[]>> = {
  [PanelTab.GeneralInfo]: ['translations', 'icon'],
  [PanelTab.PricesValidity]: [
    'price',
    'vatRate',
    'isOnlinePurchaseEnabled',
    'validityPeriods',
    'idBarServices',
    'idStandardServices',
    'idHourlyServices',
    'isPricePerServiceEnabled',
    'idSectors',
  ],
};

@Component({
  selector: 'app-automatic-price-edit-item-panel',
  imports: [
    AutomaticPriceGeneralInfoTab,
    AutomaticPricePricesValidityTab,
    Button,
    SidePanel,
    SidePanelButtons,
    TabGroup,
    TranslatePipe,
  ],
  templateUrl: './automatic-price-edit-item-panel.html',
  styleUrl: './automatic-price-edit-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomaticPriceEditItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  readonly idAutomaticPrice = input<string | null>(null);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly updated = output<void>();

  private readonly auth = inject(AuthBehaviour);
  private readonly catalog = inject(CatalogService);
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
    { label: this.texts()?.tabs.general_info ?? '', widthPercent: 50 },
    { label: this.texts()?.tabs.prices_validity ?? '', widthPercent: 50 },
  ]);
  private readonly draft = signal(newDraft());
  protected readonly draftForm = form(this.draft, (path) => {
    disabled(path, () => this.isLocked());
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
  /** The services of the property for "Validità sui servizi"; null until they are read. */
  protected readonly services = signal<AutomaticPriceServices | null>(null);
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

  protected chooseIcon(icon: BarServiceIcon | StandardServiceIcon): void {
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
    const idAutomaticPrice = this.idAutomaticPrice();
    if (this.isSaving() || !idProperty || !idAutomaticPrice) {
      return;
    }
    const tab = this.selectedTab() as PanelTab;
    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.update(tab, idProperty, idAutomaticPrice));
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
    const idAutomaticPrice = this.idAutomaticPrice();
    const idProperty = this.auth.user()?.propertyId;
    if (!idAutomaticPrice || !idProperty || this.loadedTabs().has(tab)) {
      return;
    }
    this.loadedTabs.update((tabs) => new Set(tabs).add(tab));
    this.error.set(null);
    this.setLoading(tab, true);
    if (tab === PanelTab.GeneralInfo) {
      this.loadings.set(
        tab,
        this.catalog
          .automaticPriceGeneralInfo(idAutomaticPrice)
          .pipe(finalize(() => this.setLoading(tab, false)))
          .subscribe({
            next: (info) => this.setGeneralInfo(info),
            error: (error: unknown) => this.loadFailed(tab, error),
          }),
      );
    } else {
      this.loadings.set(
        tab,
        forkJoin({
          values: this.catalog.automaticPricePricesValidity(idAutomaticPrice),
          services: this.catalog.automaticPriceServices(idProperty),
        })
          .pipe(finalize(() => this.setLoading(tab, false)))
          .subscribe({
            next: ({ values, services }) => this.setPricesValidity(values, services),
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
  private update(tab: PanelTab, idProperty: string, idAutomaticPrice: string): Observable<void> {
    const draft = this.draft();
    if (tab === PanelTab.GeneralInfo) {
      return this.catalog.updateAutomaticPriceGeneral(
        idProperty,
        Object.assign(new AutomaticPriceUpdateGeneralRequest(), {
          idAutomaticPrice,
          ...pick(draft, tab),
        }),
      );
    }
    return this.catalog.updateAutomaticPricePricesValidity(
      idProperty,
      Object.assign(new AutomaticPriceUpdatePricesValidityRequest(), {
        idAutomaticPrice,
        ...pick(draft, tab),
      }),
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
    this.services.set(null);
    this.setImage(null);
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.tabIndex.set(PanelTab.GeneralInfo);
    this.loadTab(PanelTab.GeneralInfo);
  }

  /** Name and description in every language, also those the API leaves out (empty). */
  private setGeneralInfo(info: AutomaticPriceSelectedGeneralInfo): void {
    this.applyLoaded({
      translations: SERVICE_LANGUAGES.map((language) => {
        const translation = info.translations.find((item) => item.language === language);
        return Object.assign(new ServiceTranslation(), {
          language,
          serviceName: translation?.serviceName ?? null,
          description: translation?.description ?? null,
        });
      }),
      icon: info.icon,
    });
  }

  private setPricesValidity(
    values: AutomaticPriceSelectedPricesValidity,
    services: AutomaticPriceServices,
  ): void {
    this.services.set(services);
    this.applyLoaded({
      price: values.price,
      vatRate: values.vatRate,
      isOnlinePurchaseEnabled: values.isOnlinePurchaseEnabled ?? false,
      validityPeriods: values.validityPeriods.map((period) =>
        Object.assign(new AutomaticPriceValidityPeriod(), period, {
          daysOfWeek: [...period.daysOfWeek],
          timeSlots: period.timeSlots.map((timeSlot) =>
            Object.assign(new AutomaticPriceTimeSlot(), timeSlot),
          ),
        }),
      ),
      idBarServices: [...values.idBarServices],
      idStandardServices: [...values.idStandardServices],
      idHourlyServices: [...values.idHourlyServices],
      isPricePerServiceEnabled: values.isPricePerServiceEnabled ?? false,
      idSectors: [...values.idSectors],
    });
  }

  /** Values read from an API: into the form, and into what the form is compared with. */
  private applyLoaded(values: Partial<AutomaticPriceAddItemRequest>): void {
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
function newDraft(): AutomaticPriceAddItemRequest {
  return Object.assign(new AutomaticPriceAddItemRequest(), {
    translations: SERVICE_LANGUAGES.map((language) =>
      Object.assign(new ServiceTranslation(), { language }),
    ),
  });
}

/** The values of the fields of one tab. */
function pick(
  values: AutomaticPriceAddItemRequest,
  tab: PanelTab,
): Partial<AutomaticPriceAddItemRequest> {
  return Object.fromEntries(TAB_FIELDS[tab].map((field) => [field, values[field]]));
}
