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
import { Observable, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { ServiceTranslation } from '../../../../../entities/settings/services/service-translation';
import { TableServiceAddItemRequest } from '../../../../../entities/settings/services/table-service-add-item-request';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { requiredText } from '../../../../shared/forms/validators';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { SERVICE_LANGUAGES } from '../shared/services-translations/services-translations';
import { TableServiceAvailabilityBookingsTab } from '../table-service-availability-bookings-tab/table-service-availability-bookings-tab';
import { TableServiceGeneralInfoTab } from '../table-service-general-info-tab/table-service-general-info-tab';

interface ServiceFormTexts {
  tabs: {
    general_info: string;
    availability_bookings: string;
  };
}

/** The tabs of the panel, in their order. */
enum PanelTab {
  GeneralInfo,
  AvailabilityBookings,
}

@Component({
  selector: 'app-table-service-add-item-panel',
  imports: [
    SidePanel,
    TabGroup,
    TableServiceAvailabilityBookingsTab,
    TableServiceGeneralInfoTab,
    TranslatePipe,
  ],
  templateUrl: './table-service-add-item-panel.html',
  styleUrl: './table-service-add-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableServiceAddItemPanel implements UnsavedChanges, OnDestroy {
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
    { label: this.texts()?.tabs.availability_bookings ?? '', widthPercent: 50 },
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
    applyEach(path.availabilities, (availability) => {
      disabled(
        availability.startTime,
        ({ valueOf }) => !valueOf(availability.isSameTimeForAllDays),
      );
      disabled(availability.endTime, ({ valueOf }) => !valueOf(availability.isSameTimeForAllDays));
      applyEach(availability.days, (day) => {
        disabled(day.startTime, ({ valueOf }) => valueOf(availability.isSameTimeForAllDays));
        disabled(day.endTime, ({ valueOf }) => valueOf(availability.isSameTimeForAllDays));
      });
    });
  });
  /** The form as it was when the panel opened. */
  private readonly pristine = JSON.stringify(newDraft());
  protected readonly isSaving = signal(false);
  protected readonly isSaveDisabled = computed(() => this.draftForm().invalid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && JSON.stringify(this.draft()) !== this.pristine;
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
        await firstValueFrom(this.catalog.createTableService(idProperty, this.draft()));
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
    this.error.set(null);
    this.selectedTab.set(PanelTab.GeneralInfo);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}

/** Empty, with a name and a description for every language: the user's one is the main. */
function newDraft(): TableServiceAddItemRequest {
  return Object.assign(new TableServiceAddItemRequest(), {
    translations: SERVICE_LANGUAGES.map((language) =>
      Object.assign(new ServiceTranslation(), { language }),
    ),
  });
}
