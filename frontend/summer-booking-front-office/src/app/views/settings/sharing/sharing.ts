import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  Signal,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import {
  FormField,
  TreeValidationResult,
  applyWhen,
  disabled,
  form,
  max,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription, firstValueFrom } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../behaviours/validation/validation-text.behaviour';
import { I18nText } from '../../../components/i18n/i18n-text/i18n-text';
import { VersionConflictPopup } from '../../../components/management/version-conflict-popup/version-conflict-popup';
import { Button, ButtonSize } from '../../../components/shared/ui/buttons/button/button';
import { Checkbox } from '../../../components/shared/ui/checkboxes/checkbox/checkbox';
import { comparableJson } from '../../../components/shared/forms/text-comparison';
import { greaterThan } from '../../../components/shared/forms/validators';
import { DATA_RELOAD } from '../../../components/shared/ui/data/data-reload';
import { MessagePopup } from '../../../components/shared/ui/dialogs/message-popup/message-popup';
import { SectionTitle } from '../../../components/shared/ui/titles/section-title/section-title';
import { FilledNumberField } from '../../../components/shared/ui/inputs/filled-number-field/filled-number-field';
import { Toggle } from '../../../components/shared/ui/toggles/toggle/toggle';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { SharingDraft } from '../../../entities/settings/sharing/sharing-draft';
import {
  MAX_SHARING_BOOKING_DAYS,
  MAX_SHARING_CREDIT_AMOUNT,
  MAX_SHARING_CREDIT_UNITS,
  SharingSaveInformationRequest,
} from '../../../entities/settings/sharing/sharing-save-information-request';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { onlyFieldsOf } from '../../../services/api/only-fields-of';
import { SharingService } from '../../../services/api/sharing/sharing.service';

const FIELD_NAMES: Partial<Record<keyof SharingDraft, string>> = {
  minBookingDays: 'management.settings.sharing.field_names.days',
  manualCreditUnitsPerDay: 'management.settings.sharing.field_names.credits',
  manualCreditAmount: 'management.settings.sharing.field_names.credit_value',
  voucherCreditAmount: 'management.settings.sharing.field_names.credit_value',
  cardTopUpAmount: 'management.settings.sharing.field_names.credit_value',
};

/** The values wanted by each option while it is on. */
const OPTION_VALUES = [
  ['isMinBookingDaysEnabled', ['minBookingDays']],
  ['manualCreditEnabled', ['manualCreditUnitsPerDay', 'manualCreditAmount']],
  ['voucherCreditEnabled', ['voucherCreditAmount']],
  ['cardTopUpEnabled', ['cardTopUpAmount']],
] as const;

/** With the minimum off the days have no place for their error. */
const HIDDEN_DAYS: readonly (keyof SharingDraft)[] = ['minBookingDays'];

@Component({
  selector: 'app-sharing',
  imports: [
    Button,
    Checkbox,
    FilledNumberField,
    FormField,
    I18nText,
    MessagePopup,
    SectionTitle,
    Toggle,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './sharing.html',
  styleUrl: './sharing.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Sharing implements UnsavedChanges, OnDestroy {
  private readonly sharing = inject(SharingService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });

  protected readonly ButtonSize = ButtonSize;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;

  private readonly draft = signal(new SharingDraft());
  private savedDraft = new SharingDraft();
  private version: string | null = null;
  /** Goes up with every save that succeeds: a reading started before it answers older values. */
  private savesDone = 0;
  private isConflictPending = false;
  private readonly idProperty = computed(() => this.auth.user()?.propertyId ?? null);
  protected readonly loaded = signal(false);
  /** Asked again after a version conflict or a failed reading: the effect that loads the settings reads it. */
  private readonly reloads = signal(0);
  protected readonly isSaving = signal(false);
  protected readonly saved = signal(false);
  protected readonly informationForm = form(this.draft, (path) => {
    disabled(path, () => !this.loaded() || this.isSaving());
    disabled(path.minBookingDays, ({ valueOf }) => !valueOf(path.isMinBookingDaysEnabled));
    for (const [option, values] of OPTION_VALUES) {
      applyWhen(
        path,
        ({ value }) => value()[option],
        (chosen) => values.forEach((name) => greaterThan(chosen[name], 0)),
      );
    }
    max(path.minBookingDays, MAX_SHARING_BOOKING_DAYS);
    max(path.manualCreditUnitsPerDay, MAX_SHARING_CREDIT_UNITS);
    max(path.manualCreditAmount, MAX_SHARING_CREDIT_AMOUNT);
    max(path.voucherCreditAmount, MAX_SHARING_CREDIT_AMOUNT);
    max(path.cardTopUpAmount, MAX_SHARING_CREDIT_AMOUNT);
  });
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.draft()) as (keyof SharingDraft)[]).map((name) => [
      name,
      this.validationText.message(this.informationForm[name], FIELD_NAMES[name]),
    ]),
  ) as Readonly<Record<keyof SharingDraft, Signal<string | null>>>;
  protected readonly canSave = computed(() => this.loaded() && !this.isSaving());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly isConflictShown = signal(false);
  protected readonly errorMessage = this.errorText.message(this.error);

  constructor() {
    this.unsaved.watch(this);
    // Turning an option on without its values says at once why Salva is off.
    for (const [option, values] of OPTION_VALUES) {
      effect(() => {
        const field = this.informationForm[option]();
        if (field.dirty() && field.value()) {
          untracked(() => values.forEach((name) => this.informationForm[name]().markAsTouched()));
        }
      });
    }
    effect((onCleanup) => {
      this.reloads();
      this.dataReload?.();
      if (this.idProperty() === null) {
        return;
      }
      const subscription = untracked(() => this.load());
      onCleanup(() => subscription.unsubscribe());
    });
  }

  hasUnsavedChanges(): boolean {
    return comparable(this.draft()) !== comparable(this.savedDraft);
  }

  protected save(): void {
    if (!this.canSave() || this.idProperty() === null) {
      return;
    }
    void submit(this.informationForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const sent = savedOf(this.draft());
        this.version = await firstValueFrom(this.sharing.saveInformation(sent, this.version));
        this.savesDone++;
        // The new version goes with what was sent, even if a reading changed the form meanwhile.
        this.show(sent);
        this.saved.set(true);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictPending = true;
          this.draft.set(this.savedDraft);
          this.informationForm().reset();
          this.reload();
          return undefined;
        }
        return apiFieldErrorsOrGeneral(
          problem,
          this.informationForm,
          (general) => this.error.set(general),
          this.draft().isMinBookingDaysEnabled ? [] : HIDDEN_DAYS,
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  protected reload(): void {
    this.reloads.update((reloads) => reloads + 1);
  }

  private load(): Subscription {
    const savesAtStart = this.savesDone;
    this.error.set(null);
    this.loaded.set(false);
    return this.sharing.generalInformation().subscribe({
      next: (information) => {
        if (savesAtStart === this.savesDone) {
          this.version = information.version;
          this.show(onlyFieldsOf(SharingSaveInformationRequest, information));
        }
        this.loaded.set(true);
        this.informationForm().markAsTouched();
        if (this.isConflictPending) {
          this.isConflictPending = false;
          this.isConflictShown.set(true);
        }
      },
      error: (error: unknown) => {
        this.loaded.set(false);
        this.error.set(toApiProblem(error));
      },
    });
  }

  private show(information: SharingSaveInformationRequest): void {
    this.savedDraft = {
      ...information,
      isMinBookingDaysEnabled: information.minBookingDays !== null,
    };
    this.draft.set(this.savedDraft);
    this.informationForm().reset();
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}

/** As the backend keeps them: no days with the minimum off, 0 for an empty number. */
function savedOf({
  isMinBookingDaysEnabled,
  ...information
}: SharingDraft): SharingSaveInformationRequest {
  return {
    ...information,
    minBookingDays: isMinBookingDaysEnabled ? information.minBookingDays : null,
    manualCreditUnitsPerDay: information.manualCreditUnitsPerDay ?? 0,
    manualCreditAmount: information.manualCreditAmount ?? 0,
    voucherCreditAmount: information.voucherCreditAmount ?? 0,
    cardTopUpAmount: information.cardTopUpAmount ?? 0,
  };
}

function comparable(draft: SharingDraft): string {
  return comparableJson([savedOf(draft), draft.isMinBookingDaysEnabled]);
}
