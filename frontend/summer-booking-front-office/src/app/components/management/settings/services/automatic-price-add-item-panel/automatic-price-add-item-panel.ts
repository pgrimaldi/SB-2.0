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
import { TreeValidationResult, applyEach, applyWhen, form, submit } from '@angular/forms/signals';
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
import { ServiceTranslation } from '../../../../../entities/settings/services/service-translation';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { requiredText } from '../../../../shared/forms/validators';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
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

@Component({
  selector: 'app-automatic-price-add-item-panel',
  imports: [
    AutomaticPriceGeneralInfoTab,
    AutomaticPricePricesValidityTab,
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
      applyWhen(
        translation,
        ({ valueOf }) => valueOf(translation.language) === this.mainLanguage(),
        (main) => {
          requiredText(main.serviceName);
          requiredText(main.description);
        },
      );
    });
  });
  /** The form as it was when the panel opened. */
  private readonly pristine = JSON.stringify(newDraft());
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
    return this.open() && (this.image() !== null || JSON.stringify(this.draft()) !== this.pristine);
  }

  protected chooseIcon(icon: BarServiceIcon | StandardServiceIcon): void {
    this.draftForm.icon().value.set(icon);
    this.setImage(null);
  }

  protected chooseImage(image: File): void {
    this.draftForm.icon().value.set(null);
    this.setImage(image);
  }

  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (this.isSaving() || !idProperty) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.catalog.createAutomaticPrice(idProperty, this.draft()));
        this.isSaving.set(false);
        this.open.set(false);
        this.created.emit();
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
    const idProperty = this.auth.user()?.propertyId;
    if (!idProperty) {
      return;
    }
    this.isFetching.set(true);
    this.fetching = this.catalog
      .automaticPriceServices(idProperty)
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
