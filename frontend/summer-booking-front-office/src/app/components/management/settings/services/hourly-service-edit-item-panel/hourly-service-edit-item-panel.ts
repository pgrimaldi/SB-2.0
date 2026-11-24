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
  submit,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, finalize, firstValueFrom, forkJoin } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import {
  apiFieldErrorsOrGeneral,
  clearApiFieldErrors,
} from '../../../../../behaviours/validation/api-field-errors';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { StandardServiceIcon } from '../../../../../entities/enums/standard-service-icon';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { ServiceTimeSlot } from '../../../../../entities/enums/service-time-slot';
import { HourlyServiceAddItemRequest } from '../../../../../entities/settings/services/hourly-service-add-item-request';
import { HourlyServiceBookingOptionDraft } from '../../../../../entities/settings/services/hourly-service-booking-option-draft';
import { HourlyServiceDraft } from '../../../../../entities/settings/services/hourly-service-draft';
import { HourlyServiceSelectedBookingOptions } from '../../../../../entities/settings/services/hourly-service-selected-booking-options';
import { HourlyServiceSelectedGeneralInfo } from '../../../../../entities/settings/services/hourly-service-selected-general-info';
import { HourlyServiceSelectedPrices } from '../../../../../entities/settings/services/hourly-service-selected-prices';
import { HourlyServiceUpdateBookingRequest } from '../../../../../entities/settings/services/hourly-service-update-booking-request';
import { HourlyServiceUpdateGeneralRequest } from '../../../../../entities/settings/services/hourly-service-update-general-request';
import { HourlyServiceUpdatePricesRequest } from '../../../../../entities/settings/services/hourly-service-update-prices-request';
import { ServiceAvailabilityOption } from '../../../../../entities/settings/services/service-availability-option';
import { ServiceAvailabilityOptionDraft } from '../../../../../entities/settings/services/service-availability-option-draft';
import { ServicePeriodAvailabilityDraft } from '../../../../../entities/settings/services/service-period-availability-draft';
import { ServiceTimeSlotPrice } from '../../../../../entities/settings/services/service-time-slot-price';
import {
  MAX_SERVICE_DESCRIPTION_LENGTH,
  MAX_SERVICE_NAME_LENGTH,
  ServiceTranslation,
} from '../../../../../entities/settings/services/service-translation';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { ConfirmPopup } from '../../../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { SidePanelButtons } from '../../../../shared/ui/dialogs/side-panel/side-panel-buttons';
import { PageSpinner } from '../../../../shared/ui/spinners/page-spinner/page-spinner';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { HourlyServiceBookingsTab } from '../hourly-service-bookings-tab/hourly-service-bookings-tab';
import { HourlyServiceGeneralInfoTab } from '../hourly-service-general-info-tab/hourly-service-general-info-tab';
import { HourlyServicePricesTab } from '../hourly-service-prices-tab/hourly-service-prices-tab';
import { hiddenHourlyServiceFields, serviceFieldError } from '../service-field-errors';
import {
  hourlyServicePriceRules,
  otherTranslationRules,
  serviceAvailabilityRules,
  withAvailabilitiesInForce,
  withoutEmptyAvailabilities,
} from '../service-form-rules';
import { ServiceImageErrorPopup } from '../service-image-error-popup/service-image-error-popup';
import { SERVICE_LANGUAGES } from '../shared/services-translations/services-translations';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { ReadErrorPopup } from '../../../read-error-popup/read-error-popup';
import { VersionConflictPopup } from '../../../version-conflict-popup/version-conflict-popup';

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
const TAB_FIELDS: Readonly<Record<PanelTab, readonly (keyof HourlyServiceAddItemRequest)[]>> = {
  [PanelTab.GeneralInfo]: ['translations', 'icon', 'isHighlighted'],
  [PanelTab.Prices]: [
    'price',
    'vatRate',
    'isPeriodPricingEnabled',
    'availabilityOptions',
    'periodAvailabilities',
    'idSectors',
    'isOnlinePurchaseEnabled',
  ],
  [PanelTab.Bookings]: ['bookingOptions'],
};

@Component({
  selector: 'app-hourly-service-edit-item-panel',
  imports: [
    Button,
    ConfirmPopup,
    HourlyServiceBookingsTab,
    HourlyServiceGeneralInfoTab,
    HourlyServicePricesTab,
    PageSpinner,
    ReadErrorPopup,
    ServiceImageErrorPopup,
    SidePanel,
    SidePanelButtons,
    TabGroup,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './hourly-service-edit-item-panel.html',
  styleUrl: './hourly-service-edit-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HourlyServiceEditItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  readonly idHourlyService = input<string | null>(null);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly updated = output<void>();
  readonly failed = output<ApiProblem>();

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
  protected readonly isRemovingImage = signal(false);
  protected readonly isImageDeletionAsked = signal(false);
  protected readonly isLocked = computed(
    () => !this.isEditing() || this.isSaving() || this.isRemovingImage(),
  );
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
    disabled(path, () => this.isLocked());
    applyEach(path.translations, (translation) => {
      metadata(translation.serviceName, MAX_LENGTH, () => MAX_SERVICE_NAME_LENGTH);
      metadata(translation.description, MAX_LENGTH, () => MAX_SERVICE_DESCRIPTION_LENGTH);
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
      applyWhen(
        translation,
        ({ valueOf }) =>
          this.selectedTab() === PanelTab.GeneralInfo &&
          valueOf(translation.language) !== this.mainLanguage(),
        otherTranslationRules,
      );
    });
    applyWhen(
      path,
      () => this.selectedTab() === PanelTab.Prices,
      (prices) =>
        hourlyServicePriceRules(prices, () => ({
          canLackVat: this.pristine().vatRate === null,
          hiddenAvailabilities: this.hiddenPeriodAvailabilities().flatMap(
            (period) => period.availabilityOptions,
          ),
          areBookingOptionsRead:
            this.loadedTabs().has(PanelTab.Bookings) && !this.loadingTabs().has(PanelTab.Bookings),
        })),
    );
    applyWhen(
      path.bookingOptions,
      () => this.selectedTab() === PanelTab.Bookings,
      (options) =>
        applyEach(options, (option) => serviceAvailabilityRules(option.availabilityOption)),
    );
  });
  /** The values as the APIs gave them, tab by tab: the form is compared with them. */
  private readonly pristine = signal(newDraft());
  protected readonly periods = signal<readonly SeasonPeriodItem[]>([]);
  /** Availabilities read for periods the tab does not show (e.g. of no current period): sent back as read. */
  private readonly hiddenPeriodAvailabilities = signal<readonly ServicePeriodAvailabilityDraft[]>(
    [],
  );
  protected readonly sectorsVersion = signal(0);
  /** The tabs read (or being read) since the panel opened: changing tab keeps their data. */
  private readonly loadedTabs = signal<ReadonlySet<PanelTab>>(new Set());
  /** The tabs whose data are being read: their fields cannot be used meanwhile. */
  private readonly loadingTabs = signal<ReadonlySet<PanelTab>>(new Set());
  /** The version of each tab read: its save sends it back. */
  private readonly versions = new Map<PanelTab, string | null>();
  protected readonly isEditDisabled = computed(() => {
    const tab = this.selectedTab() as PanelTab;
    return this.loadingTabs().has(tab) || !this.loadedTabs().has(tab);
  });
  protected readonly isSaveDisabled = computed(
    () =>
      this.loadingTabs().has(this.selectedTab()) ||
      !this.loadedTabs().has(this.selectedTab()) ||
      this.draftForm().invalid(),
  );
  /** The image chosen from the computer, instead of an icon of the list. */
  private readonly image = signal<File | null>(null);
  private readonly chosenImageUrl = signal<string | null>(null);
  /** The icon read when it is not one of the list: sent back as read until another is chosen. */
  private readonly otherIcon = signal<string | null>(null);
  /** The image the service has, until an icon or another image is chosen. */
  private readonly savedImageUrl = signal<string | null>(null);
  protected readonly imagePreviewUrl = computed(
    () => this.chosenImageUrl() ?? (this.draft().icon === null ? this.savedImageUrl() : null),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly imageError = signal<ApiProblem | null>(null);
  private readonly imageRemovalError = signal<ApiProblem | null>(null);
  protected readonly imageRemovalMessage = this.errorText.message(this.imageRemovalError);
  protected readonly readError = signal<ApiProblem | null>(null);
  protected readonly isConflictShown = signal(false);
  private readonly tabErrors = signal<ReadonlyMap<PanelTab, ApiProblem>>(new Map());
  protected readonly tabErrorMessage = this.errorText.message(
    computed(() => this.tabErrors().get(this.selectedTab() as PanelTab) ?? null),
  );
  protected readonly errorMessage = this.errorText.message(this.error);
  private readonly loadings = new Map<PanelTab, Subscription>();
  /** Changed by someone else: the user is told once the tab has been read again. */
  private isConflictPending = false;
  private imageRemoval?: Subscription;

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
    return (
      this.open() &&
      (this.image() !== null ||
        comparable(withoutEmptyAvailabilities(this.draft())) !==
          comparable(withoutEmptyAvailabilities(this.pristine())))
    );
  }

  protected isLoading(tab: PanelTab): boolean {
    return this.loadingTabs().has(tab);
  }

  protected chooseIcon(icon: StandardServiceIcon): void {
    this.draftForm.icon().value.set(icon);
    this.imageRemovalError.set(null);
    this.setImage(null);
  }

  protected chooseImage(image: File): void {
    this.draftForm.icon().value.set(null);
    this.imageRemovalError.set(null);
    this.setImage(image);
  }

  /** The X of the image: one not saved yet just goes away, the saved one once confirmed. */
  protected removeImage(): void {
    if (this.isLocked()) {
      return;
    }
    if (this.image()) {
      this.setImage(null);
      this.draftForm.icon().value.set(this.pristine().icon);
    } else {
      this.isImageDeletionAsked.set(true);
    }
  }

  protected confirmImageDeletion(): void {
    this.isImageDeletionAsked.set(false);
    const idHourlyService = this.idHourlyService();
    if (this.isLocked() || !idHourlyService) {
      return;
    }
    this.imageRemovalError.set(null);
    this.isRemovingImage.set(true);
    this.imageRemoval = this.catalog
      .deleteServiceImage(idHourlyService)
      .pipe(finalize(() => this.isRemovingImage.set(false)))
      .subscribe({
        next: () => this.savedImageUrl.set(null),
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.imageRemovalError.set(problem);
          this.failed.emit(problem);
        },
      });
  }

  protected startEdit(): void {
    if (this.isEditDisabled()) {
      return;
    }
    this.error.set(null);
    this.isEditing.set(true);
    // What was read already wrong shows at once: it keeps Salva off.
    this.draftForm().markAsTouched();
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
      this.imageRemovalError.set(null);
    }
    clearApiFieldErrors(this.draftForm);
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
    const idHourlyService = this.idHourlyService();
    if (
      this.isSaving() ||
      this.isSaveDisabled() ||
      !this.auth.user()?.propertyId ||
      !idHourlyService
    ) {
      return;
    }
    const tab = this.selectedTab() as PanelTab;
    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.update(tab, idHourlyService));
        const imageError = await this.uploadImage(idHourlyService);
        this.isSaving.set(false);
        this.open.set(false);
        this.updated.emit();
        this.imageError.set(imageError);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.failed.emit(problem);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictPending = true;
          this.reloadTab(tab);
          return undefined;
        }
        if (problem.code === ApiErrorCode.CatalogSectorNotFound) {
          this.sectorsVersion.update((version) => version + 1);
        }
        if (problem.code === ApiErrorCode.PricingPeriodNotFound) {
          this.refreshPeriods();
        }
        return (
          // The VAT refused can be of an availability: its message is the general one.
          serviceFieldError(problem, { icon: this.draftForm.icon }) ??
          apiFieldErrorsOrGeneral(
            problem,
            this.draftForm,
            (general) => this.error.set(general),
            hiddenHourlyServiceFields(this.draft()),
          )
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
    const idHourlyService = this.idHourlyService();
    if (!idHourlyService || !this.auth.user()?.propertyId || this.loadedTabs().has(tab)) {
      return;
    }
    this.loadedTabs.update((tabs) => new Set(tabs).add(tab));
    this.error.set(null);
    this.tabErrors.update((errors) => {
      const next = new Map(errors);
      next.delete(tab);
      return next;
    });
    this.setLoading(tab, true);
    if (tab === PanelTab.GeneralInfo) {
      this.loadings.set(
        tab,
        this.catalog
          .hourlyServiceGeneralInfo(idHourlyService)
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
          prices: this.catalog.hourlyServicePrices(idHourlyService),
          periods: this.seasons.periodList(false),
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
          .hourlyServiceBookingOptions(idHourlyService)
          .pipe(finalize(() => this.setLoading(tab, false)))
          .subscribe({
            next: (answer) => this.setBookingOptions(answer),
            error: (error: unknown) => this.loadFailed(tab, error),
          }),
      );
    }
  }

  /**
   * At the opening (no tab read yet) the panel closes for the read-error popup; later the error stays
   * in the tab, with Riprova.
   */
  private loadFailed(tab: PanelTab, error: unknown): void {
    this.loadedTabs.update((tabs) => {
      const next = new Set(tabs);
      next.delete(tab);
      return next;
    });
    this.isConflictPending = false;
    const problem = toApiProblem(error);
    this.failed.emit(problem);
    if (this.loadedTabs().size === 0) {
      this.readError.set(problem);
      this.open.set(false);
      return;
    }
    this.tabErrors.update((errors) => new Map(errors).set(tab, problem));
  }

  private reloadTab(tab: PanelTab): void {
    this.cancelEdit();
    this.loadedTabs.update((tabs) => {
      const next = new Set(tabs);
      next.delete(tab);
      return next;
    });
    this.loadTab(tab);
  }

  /** The update API of the tab, with the fields of that tab only, and the version read. */
  private update(tab: PanelTab, idHourlyService: string): Observable<string | null> {
    const draft = this.draft();
    const version = this.versions.get(tab) ?? null;
    if (tab === PanelTab.GeneralInfo) {
      return this.catalog.updateHourlyServiceGeneral(
        Object.assign(new HourlyServiceUpdateGeneralRequest(), {
          idHourlyService,
          ...pick(draft, tab),
          // The upload itself takes the icon away: without it, the icon stays.
          icon: draft.icon ?? (this.image() ? this.pristine().icon : null) ?? this.otherIcon(),
        }),
        version,
      );
    }
    if (tab === PanelTab.Prices) {
      return this.catalog.updateHourlyServicePrices(
        Object.assign(new HourlyServiceUpdatePricesRequest(), {
          idHourlyService,
          ...pick(
            withAvailabilitiesInForce({
              ...draft,
              periodAvailabilities: [
                ...draft.periodAvailabilities,
                ...this.hiddenPeriodAvailabilities(),
              ],
            }),
            tab,
          ),
        }),
        version,
      );
    }
    return this.catalog.updateHourlyServiceBooking(
      Object.assign(new HourlyServiceUpdateBookingRequest(), {
        idHourlyService,
        ...pick(withoutEmptyAvailabilities(draft), tab),
      }),
      version,
    );
  }

  /** The image goes after the data of its tab, which stay saved also without it. */
  private async uploadImage(idHourlyService: string): Promise<ApiProblem | null> {
    const image = this.image();
    if (!image) {
      return null;
    }
    try {
      await firstValueFrom(this.catalog.uploadServiceImage(idHourlyService, image));
      return null;
    } catch (error: unknown) {
      return toApiProblem(error);
    }
  }

  protected retryRead(): void {
    this.open.set(true);
  }

  protected retryTab(): void {
    this.loadTab(this.selectedTab());
  }

  /** Closed while a tab is being read: its answer, or its error, is not wanted any more. */
  private stopLoadings(): void {
    this.loadings.forEach((loading) => loading.unsubscribe());
    this.loadings.clear();
  }

  private reset(): void {
    this.tabErrors.set(new Map());
    this.stopLoadings();
    this.isConflictPending = false;
    this.versions.clear();
    this.loadedTabs.set(new Set());
    this.loadingTabs.set(new Set());
    this.error.set(null);
    this.isEditing.set(false);
    this.isSaving.set(false);
    this.draft.set(newDraft());
    this.hiddenPeriodAvailabilities.set([]);
    this.pristine.set(newDraft());
    this.draftForm().reset();
    this.periods.set([]);
    this.setImage(null);
    this.savedImageUrl.set(null);
    this.otherIcon.set(null);
    this.imageRemoval?.unsubscribe();
    this.imageRemovalError.set(null);
    this.isImageDeletionAsked.set(false);
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.tabIndex.set(PanelTab.GeneralInfo);
    this.loadTab(PanelTab.GeneralInfo);
  }

  /** Name and description in every language, also those the API leaves out (empty). */
  private setGeneralInfo(info: HourlyServiceSelectedGeneralInfo): void {
    this.versions.set(PanelTab.GeneralInfo, info.version);
    this.savedImageUrl.set(info.imageUrl);
    const icon = Object.values(StandardServiceIcon).find((value) => value === info.icon) ?? null;
    this.otherIcon.set(icon ? null : info.icon);
    this.applyLoaded({
      translations: SERVICE_LANGUAGES.map((language) => {
        const translation = info.translations.find((item) => item.language === language);
        return Object.assign(new ServiceTranslation(), {
          language,
          serviceName: translation?.serviceName ?? null,
          description: translation?.description ?? null,
        });
      }),
      icon,
      isHighlighted: info.isHighlighted ?? false,
    });
  }

  /**
   * The availability options of the whole season and of each period of the current one: the saved
   * ones, or an empty one as in the add panel, which is not sent while it is empty.
   */
  private setPrices(
    prices: HourlyServiceSelectedPrices,
    periods: readonly SeasonPeriodItem[],
  ): void {
    this.versions.set(PanelTab.Prices, prices.version);
    this.periods.set(periods);
    this.hiddenPeriodAvailabilities.set(
      hiddenOf(prices.periodAvailabilities, periods).map((period) => ({
        ...period,
        availabilityOptions: period.availabilityOptions.map((option) => ({
          ...option,
          isSaved: true,
        })),
      })),
    );
    this.applyLoaded({
      price: prices.price,
      vatRate: prices.vatRate,
      isPeriodPricingEnabled: prices.isPeriodPricingEnabled ?? false,
      availabilityOptions: availabilitiesOf(prices.availabilityOptions),
      periodAvailabilities: periods.map(({ key: idPeriod }) => {
        const saved = prices.periodAvailabilities.find((item) => item.idPeriod === idPeriod);
        return Object.assign(new ServicePeriodAvailabilityDraft(), {
          idPeriod,
          availabilityOptions: availabilitiesOf(saved?.availabilityOptions ?? []),
        });
      }),
      idSectors: [...prices.idSectors],
      isOnlinePurchaseEnabled: prices.isOnlinePurchaseEnabled ?? false,
    });
  }

  /**
   * A period refused is no longer of the season: the periods are read again, with what is written. The
   * saved availabilities of those no longer there are sent back as read.
   */
  private refreshPeriods(): void {
    this.setLoading(PanelTab.Prices, true);
    this.loadings.set(
      PanelTab.Prices,
      this.seasons
        .periodList(false)
        .pipe(finalize(() => this.setLoading(PanelTab.Prices, false)))
        .subscribe({
          next: (periods) => {
            const rowsOf = (
              rows: readonly ServicePeriodAvailabilityDraft[],
            ): ServicePeriodAvailabilityDraft[] =>
              periods.map(
                ({ key }) =>
                  rows.find(({ idPeriod }) => idPeriod === key) ??
                  Object.assign(new ServicePeriodAvailabilityDraft(), {
                    idPeriod: key,
                    availabilityOptions: [new ServiceAvailabilityOptionDraft()],
                  }),
              );
            const gone = hiddenOf(this.pristine().periodAvailabilities, periods);
            this.hiddenPeriodAvailabilities.update((hidden) => [
              ...hidden,
              ...gone.filter(({ availabilityOptions }) =>
                availabilityOptions.some(({ isSaved }) => isSaved),
              ),
            ]);
            this.periods.set(periods);
            this.draft.update((draft) => ({
              ...draft,
              periodAvailabilities: rowsOf(draft.periodAvailabilities),
            }));
            this.pristine.update((pristine) => ({
              ...pristine,
              periodAvailabilities: rowsOf(pristine.periodAvailabilities),
            }));
          },
          error: (error: unknown) => this.error.set(toApiProblem(error)),
        }),
    );
  }

  private setBookingOptions(answer: HourlyServiceSelectedBookingOptions): void {
    this.versions.set(PanelTab.Bookings, answer.version);
    this.applyLoaded({
      bookingOptions: answer.bookingOptions.map((option) =>
        Object.assign(new HourlyServiceBookingOptionDraft(), option, {
          availabilityOption: option.availabilityOption
            ? availabilityOf(option.availabilityOption)
            : new ServiceAvailabilityOptionDraft(),
        }),
      ),
    });
  }

  /** Values read from an API: into the form, and into what the form is compared with. */
  private applyLoaded(values: Partial<HourlyServiceDraft>): void {
    this.draft.update((draft) => ({ ...draft, ...values }));
    this.pristine.update((pristine) => ({ ...pristine, ...structuredClone(values) }));
    if (this.isConflictPending) {
      this.isConflictPending = false;
      this.isConflictShown.set(true);
    }
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
    const chosenImageUrl = this.chosenImageUrl();
    if (chosenImageUrl) {
      URL.revokeObjectURL(chosenImageUrl);
    }
    this.image.set(image);
    this.chosenImageUrl.set(image ? URL.createObjectURL(image) : null);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.stopLoadings();
    this.imageRemoval?.unsubscribe();
    this.setImage(null);
  }
}

/** Empty, with a name and a description for every language: the user's one is the main. */
function newDraft(): HourlyServiceDraft {
  return Object.assign(new HourlyServiceDraft(), {
    translations: SERVICE_LANGUAGES.map((language) =>
      Object.assign(new ServiceTranslation(), { language }),
    ),
  });
}

/** The saved availabilities as the form shows them, or an empty one. */
function availabilitiesOf(
  saved: readonly ServiceAvailabilityOption[],
): ServiceAvailabilityOptionDraft[] {
  return saved.length ? saved.map(availabilityOf) : [new ServiceAvailabilityOptionDraft()];
}

/**
 * A saved availability with every time slot of the list, in its order, then the durations that are
 * not in it. Without days it is of every day.
 */
function availabilityOf(saved: ServiceAvailabilityOption): ServiceAvailabilityOptionDraft {
  const option = new ServiceAvailabilityOptionDraft();
  const timeSlots: readonly string[] = Object.values(ServiceTimeSlot);
  return Object.assign(option, saved, {
    isSaved: true,
    daysOfWeek: saved.daysOfWeek.length ? [...saved.daysOfWeek] : option.daysOfWeek,
    timeSlotPrices: [
      ...timeSlots.map((timeSlot) =>
        Object.assign(
          new ServiceTimeSlotPrice(),
          saved.timeSlotPrices.find((item) => item.timeSlot === timeSlot),
          { timeSlot },
        ),
      ),
      ...saved.timeSlotPrices.filter(({ timeSlot }) => !timeSlots.includes(timeSlot ?? '')),
    ],
  });
}

/** The rows read for a period the tab does not show. */
function hiddenOf<T extends { idPeriod: number | null }>(
  rows: readonly T[],
  periods: readonly SeasonPeriodItem[],
): T[] {
  const idPeriods = new Set(periods.map(({ key }) => key));
  return rows.filter(({ idPeriod }) => idPeriod === null || !idPeriods.has(idPeriod));
}

/** The order of the sectors is not a change. */
function comparable(values: HourlyServiceAddItemRequest): string {
  return comparableJson({ ...values, idSectors: [...values.idSectors].sort() });
}

/** The values of the fields of one tab. */
function pick<T extends HourlyServiceAddItemRequest>(values: T, tab: PanelTab): Partial<T> {
  return Object.fromEntries(TAB_FIELDS[tab].map((field) => [field, values[field]])) as Partial<T>;
}
