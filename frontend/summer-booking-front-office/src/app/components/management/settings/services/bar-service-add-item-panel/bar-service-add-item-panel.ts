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
  disabled,
  form,
  max,
  metadata,
  required,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, finalize, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { BarServiceIcon } from '../../../../../entities/enums/bar-service-icon';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { BarServiceAddItemRequest } from '../../../../../entities/settings/services/bar-service-add-item-request';
import { ServicePeriodPrice } from '../../../../../entities/settings/services/service-period-price';
import { MAX_SERVICE_PRICE } from '../../../../../entities/settings/services/service-price';
import {
  MAX_SERVICE_DESCRIPTION_LENGTH,
  MAX_SERVICE_NAME_LENGTH,
  ServiceTranslation,
} from '../../../../../entities/settings/services/service-translation';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { requiredText } from '../../../../shared/forms/validators';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { ServiceBookingsTab } from '../service-bookings-tab/service-bookings-tab';
import { ServicePricesTab } from '../service-prices-tab/service-prices-tab';
import { serviceErrorFields, serviceFieldError } from '../service-field-errors';
import {
  otherTranslationRules,
  serviceBookingOptionRules,
  serviceVariantRules,
} from '../service-form-rules';
import { ServiceGeneralInfoTab } from '../service-general-info-tab/service-general-info-tab';
import { ServiceImageErrorPopup } from '../service-image-error-popup/service-image-error-popup';
import { SERVICE_LANGUAGES } from '../shared/services-translations/services-translations';
import { comparableJson } from '../../../../shared/forms/text-comparison';

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

/** The fields of the form each tab shows. */
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
  selector: 'app-bar-service-add-item-panel',
  imports: [
    ServiceBookingsTab,
    ServiceGeneralInfoTab,
    ServiceImageErrorPopup,
    ServicePricesTab,
    SidePanel,
    TabGroup,
    TranslatePipe,
  ],
  templateUrl: './bar-service-add-item-panel.html',
  styleUrl: './bar-service-add-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarServiceAddItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly created = output<void>();

  private readonly auth = inject(AuthBehaviour);
  private readonly catalog = inject(CatalogService);
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
  /** Until the periods are read, the price per period cannot be turned on. */
  private readonly arePeriodsLoaded = signal(false);
  protected readonly draftForm = form(this.draft, (path) => {
    disabled(path.isPeriodPricingEnabled, () => !this.arePeriodsLoaded());
    applyEach(path.translations, (translation) => {
      metadata(translation.serviceName, MAX_LENGTH, () => MAX_SERVICE_NAME_LENGTH);
      metadata(translation.description, MAX_LENGTH, () => MAX_SERVICE_DESCRIPTION_LENGTH);
      applyWhen(
        translation,
        ({ valueOf }) => valueOf(translation.language) === this.mainLanguage(),
        (main) => {
          requiredText(main.serviceName);
          requiredText(main.description);
        },
      );
      applyWhen(
        translation,
        ({ valueOf }) => valueOf(translation.language) !== this.mainLanguage(),
        otherTranslationRules,
      );
    });
    required(path.price);
    max(path.price, MAX_SERVICE_PRICE);
    applyEach(path.variants, serviceVariantRules);
    applyEach(path.periodPrices, (periodPrice) => max(periodPrice.price, MAX_SERVICE_PRICE));
    applyEach(path.bookingOptions, serviceBookingOptionRules);
  });
  /** The form as it was when the panel opened, with an empty price for every period. */
  private readonly pristine = signal(comparableJson(newDraft()));
  protected readonly periods = signal<readonly SeasonPeriodItem[]>([]);
  protected readonly sectorsVersion = signal(0);
  protected readonly isFetching = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isSaveDisabled = computed(
    () => this.isFetching() || this.draftForm().invalid(),
  );
  /** The image chosen from the computer, instead of an icon of the list. */
  private readonly image = signal<File | null>(null);
  protected readonly imagePreviewUrl = signal<string | null>(null);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly imageError = signal<ApiProblem | null>(null);
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
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() && (this.image() !== null || comparableJson(this.draft()) !== this.pristine())
    );
  }

  protected chooseIcon(icon: BarServiceIcon): void {
    this.draftForm.icon().value.set(icon);
    this.setImage(null);
  }

  protected chooseImage(image: File): void {
    this.draftForm.icon().value.set(null);
    this.setImage(image);
  }

  /** The image is not saved yet: it just goes away. */
  protected removeImage(): void {
    this.setImage(null);
  }

  protected save(): void {
    if (this.isSaving() || !this.auth.user()?.propertyId) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const idBarService = await firstValueFrom(this.catalog.createBarService(this.draft()));
        const imageError = await this.uploadImage(idBarService);
        this.isSaving.set(false);
        this.open.set(false);
        this.created.emit();
        this.imageError.set(imageError);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.showTabOf(problem);
        if (problem.code === ApiErrorCode.CatalogSectorNotFound) {
          this.sectorsVersion.update((version) => version + 1);
        }
        if (problem.code === ApiErrorCode.PricingPeriodNotFound) {
          this.fetchPeriods();
        }
        return (
          serviceFieldError(problem, this.draftForm) ??
          apiFieldErrorsOrGeneral(problem, this.draftForm, (general) => this.error.set(general))
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

  /** The image goes after the service, which stays saved also without it. */
  private async uploadImage(idBarService: string): Promise<ApiProblem | null> {
    const image = this.image();
    if (!image) {
      return null;
    }
    try {
      await firstValueFrom(this.catalog.uploadServiceImage(idBarService, image));
      return null;
    } catch (error: unknown) {
      return toApiProblem(error);
    }
  }

  /** The tab of the first wrong field, where its error shows. */
  private showTabOf(problem: ApiProblem): void {
    const fields = serviceErrorFields(problem);
    const tab = [PanelTab.GeneralInfo, PanelTab.Prices, PanelTab.Bookings].find((panelTab) =>
      TAB_FIELDS[panelTab].some((field) => fields.includes(field)),
    );
    if (tab !== undefined) {
      this.selectedTab.set(tab);
    }
  }

  private reset(): void {
    this.draft.set(newDraft());
    this.draftForm().reset();
    this.setPeriods([]);
    this.arePeriodsLoaded.set(false);
    this.setImage(null);
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.fetchPeriods();
  }

  /** The periods of the current season, each with its own price in the Prezzi tab. */
  private fetchPeriods(): void {
    this.fetching?.unsubscribe();
    this.error.set(null);
    if (!this.auth.user()?.propertyId) {
      return;
    }
    this.isFetching.set(true);
    this.fetching = this.seasons
      .periodList(false)
      .pipe(finalize(() => this.isFetching.set(false)))
      .subscribe({
        next: (periods) => {
          this.setPeriods(periods);
          this.arePeriodsLoaded.set(true);
        },
        error: (error: unknown) => this.error.set(toApiProblem(error)),
      });
  }

  /** A price for each period of the season: the one written, or empty as in the form without changes. */
  private setPeriods(periods: readonly SeasonPeriodItem[]): void {
    const written = this.draft().periodPrices;
    const empty = periods.map(({ key: idPeriod }) =>
      Object.assign(new ServicePeriodPrice(), { idPeriod }),
    );
    this.periods.set(periods);
    this.draftForm
      .periodPrices()
      .value.set(
        empty.map((row) => written.find(({ idPeriod }) => idPeriod === row.idPeriod) ?? row),
      );
    this.pristine.set(comparableJson({ ...newDraft(), periodPrices: empty }));
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
    this.fetching?.unsubscribe();
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
