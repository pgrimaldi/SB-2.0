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
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
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
import { firstValueFrom, of, switchMap } from 'rxjs';
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
import {
  MAX_SHARING_CREDIT_VALUE,
  MAX_SHARING_QUANTITY,
  SharingSaveInformationRequest,
} from '../../../entities/settings/sharing/sharing-save-information-request';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { onlyFieldsOf } from '../../../services/api/only-fields-of';
import { SharingService } from '../../../services/api/sharing/sharing.service';

const FIELD_NAMES: Partial<Record<keyof SharingSaveInformationRequest, string>> = {
  minimumBookingDayQuantity: 'management.settings.sharing.field_names.days',
  resoldDayCreditQuantity: 'management.settings.sharing.field_names.credits',
  manualCreditValue: 'management.settings.sharing.field_names.credit_value',
  voucherCreditValue: 'management.settings.sharing.field_names.credit_value',
  prepaidCardCreditValue: 'management.settings.sharing.field_names.credit_value',
};

/** The values wanted by each option while it is on. */
const OPTION_VALUES = [
  ['isMinimumBookingDayEnabled', ['minimumBookingDayQuantity']],
  ['isManualCreditAssignmentEnabled', ['resoldDayCreditQuantity', 'manualCreditValue']],
  ['isAutomaticVoucherEnabled', ['voucherCreditValue']],
  ['isPrepaidCardTopUpEnabled', ['prepaidCardCreditValue']],
] as const;

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

  private readonly information = signal(new SharingSaveInformationRequest());
  private savedInformation = new SharingSaveInformationRequest();
  private version: string | null = null;
  private readonly loaded = signal(false);
  /** Asked again after a version conflict: the effect that loads the settings reads it. */
  private readonly reloads = signal(0);
  protected readonly isSaving = signal(false);
  protected readonly saved = signal(false);
  protected readonly informationForm = form(this.information, (path) => {
    disabled(path, () => !this.loaded() || this.isSaving());
    disabled(
      path.minimumBookingDayQuantity,
      ({ valueOf }) => !valueOf(path.isMinimumBookingDayEnabled),
    );
    for (const [option, values] of OPTION_VALUES) {
      applyWhen(
        path,
        ({ value }) => value()[option],
        (chosen) => values.forEach((name) => greaterThan(chosen[name], 0)),
      );
    }
    max(path.minimumBookingDayQuantity, MAX_SHARING_QUANTITY);
    max(path.resoldDayCreditQuantity, MAX_SHARING_QUANTITY);
    max(path.manualCreditValue, MAX_SHARING_CREDIT_VALUE);
    max(path.voucherCreditValue, MAX_SHARING_CREDIT_VALUE);
    max(path.prepaidCardCreditValue, MAX_SHARING_CREDIT_VALUE);
  });
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.information()) as (keyof SharingSaveInformationRequest)[]).map((name) => [
      name,
      this.validationText.message(this.informationForm[name], FIELD_NAMES[name]),
    ]),
  ) as Readonly<Record<keyof SharingSaveInformationRequest, Signal<string | null>>>;
  protected readonly canSave = computed(() => this.loaded() && !this.isSaving());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly isConflictShown = signal(false);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

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
      if (!this.auth.user()?.propertyId) {
        return;
      }
      const subscription = untracked(() => {
        this.error.set(null);
        this.loaded.set(false);
        return this.sharing.generalInformation().subscribe({
          next: (information) => {
            const read = onlyFieldsOf(SharingSaveInformationRequest, information);
            this.version = information.version;
            this.savedInformation = read;
            this.information.set(read);
            this.informationForm().reset();
            this.loaded.set(true);
            this.informationForm().markAsTouched();
          },
          error: (error: unknown) => {
            this.loaded.set(false);
            this.error.set(toApiProblem(error));
          },
        });
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      comparableJson(asSaved(this.information())) !== comparableJson(asSaved(this.savedInformation))
    );
  }

  protected save(): void {
    if (!this.canSave() || !this.auth.user()?.propertyId) {
      return;
    }
    void submit(this.informationForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const sent = asSaved(this.information());
        this.version = await firstValueFrom(this.sharing.saveInformation(sent, this.version));
        // The new version goes with what was sent, even if a reading changed the form meanwhile.
        this.savedInformation = sent;
        this.information.set(sent);
        this.informationForm().reset();
        this.saved.set(true);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictShown.set(true);
          this.reloads.update((reloads) => reloads + 1);
          return undefined;
        }
        return apiFieldErrorsOrGeneral(problem, this.informationForm, (general) =>
          this.error.set(general),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}

/** As the backend keeps them: no days with the minimum off, 0 for an empty number. */
function asSaved(information: SharingSaveInformationRequest): SharingSaveInformationRequest {
  return {
    ...information,
    minimumBookingDayQuantity: information.isMinimumBookingDayEnabled
      ? information.minimumBookingDayQuantity
      : null,
    resoldDayCreditQuantity: information.resoldDayCreditQuantity ?? 0,
    manualCreditValue: information.manualCreditValue ?? 0,
    voucherCreditValue: information.voucherCreditValue ?? 0,
    prepaidCardCreditValue: information.prepaidCardCreditValue ?? 0,
  };
}
