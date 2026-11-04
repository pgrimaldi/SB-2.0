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
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { BarServiceAddItemRequest } from '../../../../../entities/settings/services/bar-service-add-item-request';
import { ServicePeriodPrice } from '../../../../../entities/settings/services/service-period-price';
import { ServiceTranslation } from '../../../../../entities/settings/services/service-translation';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { requiredText } from '../../../../shared/forms/validators';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
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

@Component({
  selector: 'app-bar-service-add-item-panel',
  imports: [
    ServiceBookingsTab,
    ServiceGeneralInfoTab,
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
  /** The form as it was when the panel opened, with an empty price for every period. */
  private readonly pristine = signal(JSON.stringify(newDraft()));
  protected readonly periods = signal<readonly SeasonPeriodItem[]>([]);
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
    return (
      this.open() && (this.image() !== null || JSON.stringify(this.draft()) !== this.pristine())
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

  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (this.isSaving() || !idProperty) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.catalog.createBarService(idProperty, this.draft()));
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
    this.setPeriods([]);
    this.setImage(null);
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.fetchPeriods();
  }

  /** The periods of the current season, each with its own price in the Prezzi tab. */
  private fetchPeriods(): void {
    this.fetching?.unsubscribe();
    this.error.set(null);
    const idProperty = this.auth.user()?.propertyId;
    if (!idProperty) {
      return;
    }
    this.isFetching.set(true);
    this.fetching = this.seasons
      .periodList(idProperty, false)
      .pipe(finalize(() => this.isFetching.set(false)))
      .subscribe({
        next: (periods) => this.setPeriods(periods),
        error: (error: unknown) => this.error.set(toApiProblem(error)),
      });
  }

  /** An empty price for each period of the season: the form without changes. */
  private setPeriods(periods: readonly SeasonPeriodItem[]): void {
    this.periods.set(periods);
    this.draftForm
      .periodPrices()
      .value.set(
        periods.map(({ idPeriod }) => Object.assign(new ServicePeriodPrice(), { idPeriod })),
      );
    this.pristine.set(JSON.stringify(this.draft()));
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
