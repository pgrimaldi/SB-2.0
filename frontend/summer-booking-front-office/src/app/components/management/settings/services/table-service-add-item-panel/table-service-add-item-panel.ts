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
import { Observable, firstValueFrom } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  MAX_SERVICE_DESCRIPTION_LENGTH,
  MAX_SERVICE_NAME_LENGTH,
  ServiceTranslation,
} from '../../../../../entities/settings/services/service-translation';
import { TableServiceAddItemRequest } from '../../../../../entities/settings/services/table-service-add-item-request';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { requiredText } from '../../../../shared/forms/validators';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { hiddenTableServiceFields, serviceErrorFields } from '../service-field-errors';
import { otherTranslationRules, tableServiceRules } from '../service-form-rules';
import { SERVICE_LANGUAGES } from '../shared/services-translations/services-translations';
import { TableServiceAvailabilityBookingsTab } from '../table-service-availability-bookings-tab/table-service-availability-bookings-tab';
import { TableServiceGeneralInfoTab } from '../table-service-general-info-tab/table-service-general-info-tab';
import { comparableJson } from '../../../../shared/forms/text-comparison';

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

/** The fields of the form the Info Generali tab shows: the others are in the other tab. */
const GENERAL_INFO_FIELDS: readonly string[] = [
  'translations',
  'isHighlighted',
] satisfies (keyof TableServiceAddItemRequest)[];

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
    tableServiceRules(path);
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
  private readonly pristine = comparableJson(newDraft());
  protected readonly isSaving = signal(false);
  protected readonly isSaveDisabled = computed(() => this.draftForm().invalid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = this.errorText.message(this.error);

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && comparableJson(this.draft()) !== this.pristine;
  }

  protected save(): void {
    if (this.isSaving() || !this.auth.user()?.propertyId) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.catalog.createTableService(this.draft()));
        this.isSaving.set(false);
        this.open.set(false);
        this.created.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.showTabOf(problem);
        return apiFieldErrorsOrGeneral(
          problem,
          this.draftForm,
          (general) => this.error.set(general),
          hiddenTableServiceFields(this.draft()),
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

  /** The tab of the first wrong field, where its error shows. */
  private showTabOf(problem: ApiProblem): void {
    const fields = serviceErrorFields(problem);
    if (fields.length) {
      this.selectedTab.set(
        fields.some((field) => GENERAL_INFO_FIELDS.includes(field))
          ? PanelTab.GeneralInfo
          : PanelTab.AvailabilityBookings,
      );
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
