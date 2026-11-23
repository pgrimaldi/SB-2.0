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
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { BarServiceIcon } from '../../../../../entities/enums/bar-service-icon';
import { StandardServiceIcon } from '../../../../../entities/enums/standard-service-icon';
import { AutomaticPriceAddItemRequest } from '../../../../../entities/settings/services/automatic-price-add-item-request';
import { AutomaticPriceServices } from '../../../../../entities/settings/services/automatic-price-services';
import {
  MAX_SERVICE_DESCRIPTION_LENGTH,
  MAX_SERVICE_NAME_LENGTH,
  ServiceTranslation,
} from '../../../../../entities/settings/services/service-translation';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { requiredText } from '../../../../shared/forms/validators';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { AutomaticPriceGeneralInfoTab } from '../automatic-price-general-info-tab/automatic-price-general-info-tab';
import { AutomaticPricePricesValidityTab } from '../automatic-price-prices-validity-tab/automatic-price-prices-validity-tab';
import {
  HIDDEN_AUTOMATIC_PRICE_FIELDS,
  serviceErrorFields,
  serviceFieldError,
} from '../service-field-errors';
import {
  automaticPriceRules,
  otherTranslationRules,
  withServicesInForce,
} from '../service-form-rules';
import { ServiceImageErrorPopup } from '../service-image-error-popup/service-image-error-popup';
import { SERVICE_LANGUAGES } from '../shared/services-translations/services-translations';
import { comparableJson } from '../../../../shared/forms/text-comparison';

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

/** The fields of the form the Info Generali tab shows: the others are in Prezzi e Validità. */
const GENERAL_INFO_FIELDS: readonly string[] = [
  'translations',
  'icon',
] satisfies (keyof AutomaticPriceAddItemRequest)[];

@Component({
  selector: 'app-automatic-price-add-item-panel',
  imports: [
    AutomaticPriceGeneralInfoTab,
    AutomaticPricePricesValidityTab,
    ServiceImageErrorPopup,
    SidePanel,
    TabGroup,
    TranslatePipe,
  ],
  templateUrl: './automatic-price-add-item-panel.html',
  styleUrl: './automatic-price-add-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomaticPriceAddItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly created = output<void>();

  private readonly auth = inject(AuthBehaviour);
  private readonly catalog = inject(CatalogService);
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
    { label: this.texts()?.tabs.general_info ?? '', widthPercent: 50 },
    { label: this.texts()?.tabs.prices_validity ?? '', widthPercent: 50 },
  ]);
  private readonly draft = signal(newDraft());
  protected readonly draftForm = form(this.draft, (path) => {
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
    automaticPriceRules(path);
  });
  /** The form as it was when the panel opened. */
  private readonly pristine = comparableJson(newDraft());
  /** The services of the property for "Validità sui servizi"; null until they are read. */
  protected readonly services = signal<AutomaticPriceServices | null>(null);
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
    return this.open() && (this.image() !== null || comparableJson(this.draft()) !== this.pristine);
  }

  protected chooseIcon(icon: BarServiceIcon | StandardServiceIcon): void {
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
        const idAutomaticPrice = await firstValueFrom(
          this.catalog.createAutomaticPrice(withServicesInForce(this.draft())),
        );
        const imageError = await this.uploadImage(idAutomaticPrice);
        this.isSaving.set(false);
        this.open.set(false);
        this.created.emit();
        this.imageError.set(imageError);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.showTabOf(problem);
        return (
          serviceFieldError(problem, this.draftForm) ??
          apiFieldErrorsOrGeneral(
            problem,
            this.draftForm,
            (general) => this.error.set(general),
            HIDDEN_AUTOMATIC_PRICE_FIELDS,
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

  /** The image goes after the automatic price, which stays saved also without it. */
  private async uploadImage(idAutomaticPrice: string): Promise<ApiProblem | null> {
    const image = this.image();
    if (!image) {
      return null;
    }
    try {
      await firstValueFrom(this.catalog.uploadAutomaticPriceImage(idAutomaticPrice, image));
      return null;
    } catch (error: unknown) {
      return toApiProblem(error);
    }
  }

  /** The tab of the first wrong field, where its error shows. */
  private showTabOf(problem: ApiProblem): void {
    const fields = serviceErrorFields(problem);
    if (fields.length) {
      this.selectedTab.set(
        fields.some((field) => GENERAL_INFO_FIELDS.includes(field))
          ? PanelTab.GeneralInfo
          : PanelTab.PricesValidity,
      );
    }
  }

  private reset(): void {
    this.draft.set(newDraft());
    this.draftForm().reset();
    this.services.set(null);
    this.setImage(null);
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.fetchServices();
  }

  /** The services the automatic price can apply to, in the Prezzi e Validità tab. */
  private fetchServices(): void {
    this.fetching?.unsubscribe();
    this.error.set(null);
    if (!this.auth.user()?.propertyId) {
      return;
    }
    this.isFetching.set(true);
    this.fetching = this.catalog
      .automaticPriceServices()
      .pipe(finalize(() => this.isFetching.set(false)))
      .subscribe({
        next: (services) => this.services.set(services),
        error: (error: unknown) => this.error.set(toApiProblem(error)),
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
    this.fetching?.unsubscribe();
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
