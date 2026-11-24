import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EnvironmentInjector,
  OnDestroy,
  Signal,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  TreeValidationResult,
  applyWhen,
  form,
  max,
  min,
  required,
  submit,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, firstValueFrom } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import {
  apiFieldErrorsOrGeneral,
  clearApiFieldErrors,
} from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { OnlinePaymentMethod } from '../../../../../entities/enums/online-payment-method';
import { SupplementType } from '../../../../../entities/enums/supplement-type';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  MAX_ONLINE_USEFUL_INFO_LENGTH,
  MAX_SERVICES_SECTION_TITLE_LENGTH,
  MAX_SURCHARGE_AMOUNT,
  MAX_SURCHARGE_PERCENTAGE,
  OnlineBookingServicesOptionsUpdateRequest,
} from '../../../../../entities/settings/online-booking-configuration/online-booking-services-options-update-request';
import { OnlineSettingsService } from '../../../../../services/api/catalog/online-settings.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { onlyFieldsOf } from '../../../../../services/api/only-fields-of';
import { showFirstError, showMessage } from '../../../../shared/forms/first-error';
import { maxHtmlLength } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { DATA_RELOAD } from '../../../../shared/ui/data/data-reload';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { RichTextField } from '../../../../shared/ui/inputs/rich-text-field/rich-text-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { ReadErrorPopup } from '../../../read-error-popup/read-error-popup';
import { VersionConflictPopup } from '../../../version-conflict-popup/version-conflict-popup';

interface ServicesOptionsTexts {
  supplement_types: Record<'none' | 'percentage' | 'fixed', string>;
}

/** With no supplement its value has no place for an error. */
const HIDDEN_VALUE: readonly (keyof OnlineBookingServicesOptionsUpdateRequest)[] = [
  'surchargeValue',
];

@Component({
  selector: 'app-online-booking-services-options-popup',
  imports: [
    Button,
    Checkbox,
    FilledNumberField,
    FilledSelect,
    FormField,
    FormRow,
    ReadErrorPopup,
    RichTextField,
    SectionTitle,
    Toggle,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './online-booking-services-options-popup.html',
  styleUrl: './online-booking-services-options-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class OnlineBookingServicesOptionsPopup
  extends BasePopup
  implements UnsavedChanges, OnDestroy
{
  private readonly auth = inject(AuthBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly injector = inject(EnvironmentInjector);
  private readonly onlineSettings = inject(OnlineSettingsService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly paymentMethods = [
    {
      method: OnlinePaymentMethod.PayNow,
      label:
        'management.settings.online_booking_configuration.services_options.payment_methods.pay_now',
    },
    {
      method: OnlinePaymentMethod.PayAtProperty,
      label:
        'management.settings.online_booking_configuration.services_options.payment_methods.pay_at_property',
    },
    {
      method: OnlinePaymentMethod.PayAtPropertyWithoutCard,
      label:
        'management.settings.online_booking_configuration.services_options.payment_methods.pay_at_property_without_card',
    },
    {
      method: OnlinePaymentMethod.Deposit,
      label:
        'management.settings.online_booking_configuration.services_options.payment_methods.deposit',
    },
    {
      method: OnlinePaymentMethod.PrepaidCard,
      label:
        'management.settings.online_booking_configuration.services_options.payment_methods.prepaid_card',
    },
  ] as const;
  /** [bold, italic, underline, bulleted list, numbered list, link]. */
  protected readonly richTextIcons = [
    '/assets/images/GeneralIcon_Bold.svg',
    '/assets/images/GeneralIcon_Italic.svg',
    '/assets/images/GeneralIcon_Underline.svg',
    '/assets/images/GeneralIcon_BulletList.svg',
    '/assets/images/GeneralIcon_OrderedList.svg',
    '/assets/images/GeneralIcon_TextLink.svg',
  ] as const;

  private readonly popup = viewChild<ElementRef<HTMLElement>>('popup');
  private readonly draft = signal(new OnlineBookingServicesOptionsUpdateRequest());
  /** The values as the API gave them: the form is compared with them. */
  private loaded = new OnlineBookingServicesOptionsUpdateRequest();
  private version: string | null = null;
  /** Only a change of the type by the user empties the value, never a reading. */
  private shownType = SupplementType.None;
  protected readonly draftForm = form(this.draft, (path) => {
    applyWhen(
      path.surchargeValue,
      ({ valueOf }) => valueOf(path.surchargeType) !== SupplementType.None,
      (surchargeValue) => {
        required(surchargeValue);
        min(surchargeValue, 0);
        max(surchargeValue, ({ valueOf }) =>
          valueOf(path.surchargeType) === SupplementType.Percentage
            ? MAX_SURCHARGE_PERCENTAGE
            : MAX_SURCHARGE_AMOUNT,
        );
      },
    );
    maxHtmlLength(path.sectionTitle, MAX_SERVICES_SECTION_TITLE_LENGTH);
    maxHtmlLength(path.usefulInfo, MAX_ONLINE_USEFUL_INFO_LENGTH);
  });
  protected readonly hasSupplement = computed(
    () => this.draftForm.surchargeType().value() !== SupplementType.None,
  );
  protected readonly isPercentage = computed(
    () => this.draftForm.surchargeType().value() === SupplementType.Percentage,
  );
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.draft()) as (keyof OnlineBookingServicesOptionsUpdateRequest)[]).map(
      (name) => [name, this.validationText.message(this.draftForm[name])],
    ),
  ) as Readonly<Record<keyof OnlineBookingServicesOptionsUpdateRequest, Signal<string | null>>>;
  protected readonly isReading = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isSaveDisabled = computed(
    () => this.isReading() || !this.isLoaded() || this.draftForm().invalid(),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly readError = signal<ApiProblem | null>(null);
  protected readonly isConflictShown = signal(false);
  protected readonly errorMessage = this.errorText.message(this.error);
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.online_booking_configuration.services_options',
    ) as Observable<ServicesOptionsTexts>,
    { initialValue: null },
  );
  protected readonly supplementTypeOptions = computed<readonly SelectOption<SupplementType>[]>(
    () => [
      { value: SupplementType.None, label: this.texts()?.supplement_types.none ?? '' },
      { value: SupplementType.Percentage, label: this.texts()?.supplement_types.percentage ?? '' },
      { value: SupplementType.Fixed, label: this.texts()?.supplement_types.fixed ?? '' },
    ],
  );

  constructor() {
    super();
    this.unsaved.watch(this);
    effect((onCleanup) => {
      this.dataReload?.();
      if (!this.open()) {
        untracked(() => this.isReading.set(false));
        return;
      }
      const subscription = untracked(() => this.load());
      onCleanup(() => subscription?.unsubscribe());
    });
    effect(() => {
      const type = this.draftForm.surchargeType().value();
      untracked(() => {
        if (this.shownType === type) {
          return;
        }
        this.shownType = type;
        const value = this.draftForm.surchargeValue();
        value.value.set(type === SupplementType.None ? this.valueWithoutSupplement() : null);
        value.markAsTouched();
      });
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && this.isLoaded() && comparable(this.draft()) !== comparable(this.loaded);
  }

  protected isAccepted(method: OnlinePaymentMethod): boolean {
    return this.draftForm.paymentMethods().value().includes(method);
  }

  protected accept(method: OnlinePaymentMethod, isAccepted: boolean): void {
    const methods = this.draftForm.paymentMethods();
    methods.value.update((accepted) =>
      isAccepted ? [...accepted, method] : accepted.filter((other) => other !== method),
    );
    methods.markAsDirty();
  }

  protected save(): void {
    if (this.isReading() || this.isSaving() || !this.isLoaded()) {
      return;
    }
    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(
          this.onlineSettings.updateOnlineServicesOptions(savedOf(this.draft()), this.version),
        );
        this.isSaving.set(false);
        this.close();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictShown.set(true);
          this.isSaving.set(false);
          this.close();
          return undefined;
        }
        return apiFieldErrorsOrGeneral(
          problem,
          this.draftForm,
          (general) => this.error.set(general),
          this.hasSupplement() ? [] : HIDDEN_VALUE,
        );
      } finally {
        this.isSaving.set(false);
      }
    }).then((isSaved) => {
      const error = '.online__booking__services__options__popup__error';
      if (!isSaved) {
        showFirstError(this.popup(), error, this.injector);
      } else if (this.error()) {
        showMessage(
          this.popup(),
          '.online__booking__services__options__popup__general',
          this.injector,
        );
      }
    });
  }

  protected retryRead(): void {
    this.open.set(true);
  }

  protected async leave(): Promise<void> {
    if (!this.isSaving() && (await this.unsaved.confirmDiscard(this))) {
      this.close();
    }
  }

  protected override isCloseDisabled(): boolean {
    return this.isSaving();
  }

  protected override closeRequested(): void {
    void this.leave();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(44rem, 90vw)',
      maxWidth: 'none',
      maxHeight: '90vh',
      panelClass: 'online__booking__services__options__popup__panel',
      disableClose: true,
    };
  }

  /** The saved options are read at every opening: the popup always starts from the server. */
  private load(): Subscription | undefined {
    this.error.set(null);
    this.readError.set(null);
    this.isLoaded.set(false);
    this.isSaving.set(false);
    this.fill(new OnlineBookingServicesOptionsUpdateRequest());
    if (!this.auth.user()?.propertyId) {
      return undefined;
    }
    this.isReading.set(true);
    return this.onlineSettings.onlineServicesOptions().subscribe({
      next: (options) => {
        this.version = options.version;
        this.fill(onlyFieldsOf(OnlineBookingServicesOptionsUpdateRequest, options));
        this.draftForm().markAsTouched();
        this.isLoaded.set(true);
        this.isReading.set(false);
      },
      error: (error: unknown) => {
        this.isReading.set(false);
        this.close();
        this.readError.set(toApiProblem(error));
      },
    });
  }

  private fill(options: OnlineBookingServicesOptionsUpdateRequest): void {
    this.shownType = options.surchargeType;
    this.loaded = options;
    this.draft.set(options);
    clearApiFieldErrors(this.draftForm);
    this.draftForm().reset();
  }

  /** The value read together with no supplement goes back as it was. */
  private valueWithoutSupplement(): number | null {
    return this.loaded.surchargeType === SupplementType.None ? this.loaded.surchargeValue : null;
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}

/** As the backend keeps them: 0 for an empty value. */
function savedOf(
  options: OnlineBookingServicesOptionsUpdateRequest,
): OnlineBookingServicesOptionsUpdateRequest {
  return { ...options, surchargeValue: options.surchargeValue ?? 0 };
}

/** The order of the payment methods means nothing. */
function comparable(options: OnlineBookingServicesOptionsUpdateRequest): string {
  return comparableJson({
    ...savedOf(options),
    paymentMethods: [...options.paymentMethods].sort(),
  });
}
