import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EnvironmentInjector,
  OnDestroy,
  computed,
  inject,
  output,
  signal,
} from '@angular/core';
import {
  FormField,
  MAX_LENGTH,
  TreeValidationResult,
  applyWhen,
  form,
  metadata,
  readonly,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
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
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  ElectronicReceiptAccount,
  MAX_ACCOUNT_NAME_LENGTH,
  MAX_ACCOUNT_POSTAL_CODE_LENGTH,
  MAX_ACCOUNT_STREET_NUMBER_LENGTH,
  MAX_ACCOUNT_TAX_CODE_LENGTH,
  MAX_ACCOUNT_VAT_NUMBER_LENGTH,
} from '../../../../../entities/settings/taxation/electronic-receipt-account';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { TaxationService } from '../../../../../services/api/taxation/taxation.service';
import { showFirstError } from '../../../../shared/forms/first-error';
import { comparableJson, trimmedText } from '../../../../shared/forms/text-comparison';
import { requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';

/** Once saved the password cannot be changed here: its error has no field to go under. */
const LOCKED_FIELDS: readonly (keyof ElectronicReceiptAccount)[] = ['password'];
const CODE_FIELDS: readonly (keyof ElectronicReceiptAccount)[] = [
  'postalCode',
  'taxCode',
  'revenueAgencyCode',
  'vatNumber',
  'pin',
];

@Component({
  selector: 'app-electronic-receipt-account-form',
  imports: [Button, FilledTextField, FormField, IconButton, TranslatePipe],
  templateUrl: './electronic-receipt-account-form.html',
  styleUrl: './electronic-receipt-account-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ElectronicReceiptAccountForm implements UnsavedChanges, OnDestroy {
  readonly back = output<void>();
  /** The electronic receipt is off for the property, or its terms are not accepted. */
  readonly refused = output<ApiProblem>();

  private readonly taxation = inject(TaxationService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(EnvironmentInjector);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly backIcon = ['/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly eyeIcons = [
    '/assets/images/GeneralIcon_EyeClose.svg',
    '/assets/images/GeneralIcon_EyeStart.svg',
  ] as const;
  private readonly account = signal(new ElectronicReceiptAccount());
  private savedAccount = new ElectronicReceiptAccount();
  protected readonly isSaved = signal(false);
  protected readonly isEditing = signal(false);
  protected readonly isPinSaved = signal(false);
  protected readonly isPasswordSaved = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly isReady = computed(() => !!this.auth.user()?.propertyId);
  protected readonly accountForm = form(this.account, (path) => {
    readonly(path, () => this.isLoading() || (this.isSaved() && !this.isEditing()));
    readonly(path.password, () => this.isSaved());
    requiredText(path.companyName);
    requiredText(path.companyAddress);
    requiredText(path.streetNumber);
    requiredText(path.postalCode);
    requiredText(path.city);
    requiredText(path.country);
    requiredText(path.taxCode);
    requiredText(path.revenueAgencyCode);
    requiredText(path.vatNumber);
    metadata(path.companyName, MAX_LENGTH, () => MAX_ACCOUNT_NAME_LENGTH);
    metadata(path.companyAddress, MAX_LENGTH, () => MAX_ACCOUNT_NAME_LENGTH);
    metadata(path.streetNumber, MAX_LENGTH, () => MAX_ACCOUNT_STREET_NUMBER_LENGTH);
    metadata(path.postalCode, MAX_LENGTH, () => MAX_ACCOUNT_POSTAL_CODE_LENGTH);
    metadata(path.city, MAX_LENGTH, () => MAX_ACCOUNT_NAME_LENGTH);
    metadata(path.taxCode, MAX_LENGTH, () => MAX_ACCOUNT_TAX_CODE_LENGTH);
    metadata(path.vatNumber, MAX_LENGTH, () => MAX_ACCOUNT_VAT_NUMBER_LENGTH);
    // Empty once one is saved on the server: empty then means "keep the saved one".
    applyWhen(
      path.pin,
      () => !this.isPinSaved(),
      (pin) => requiredText(pin),
    );
    applyWhen(
      path.password,
      () => !this.isPasswordSaved(),
      (password) => requiredText(password),
    );
  });
  protected readonly fields = (
    [
      ['company_name', 'companyName', 18],
      ['company_address', 'companyAddress', 33],
      ['street_number', 'streetNumber', 9],
      ['postal_code', 'postalCode', 12],
      ['city', 'city', 24],
      ['country', 'country', 24],
      ['tax_code', 'taxCode', 20],
      ['revenue_agency_code', 'revenueAgencyCode', 20],
      ['vat_number', 'vatNumber', 20],
      ['pin', 'pin', 30],
      ['password', 'password', 30],
    ] as const
  ).map(([key, name, columns]) => ({
    key,
    columns,
    field: this.accountForm[name],
    error: this.validationText.message(this.accountForm[name]),
    isCode: CODE_FIELDS.includes(name),
    isSecretSaved:
      name === 'pin' ? this.isPinSaved : name === 'password' ? this.isPasswordSaved : null,
  }));
  protected readonly savedIcon = '/assets/images/Taxation_AccountSaved.svg';
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = this.errorText.message(this.error);

  constructor() {
    this.unsaved.watch(this);
  }

  hasUnsavedChanges(): boolean {
    return comparableJson(this.account()) !== comparableJson(this.savedAccount);
  }

  protected save(): void {
    if (this.isLoading() || !this.isReady()) {
      return;
    }
    void submit(this.accountForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      const sent = this.accountToSend();
      try {
        await firstValueFrom(this.taxation.saveElectronicReceiptAccount(sent));
        this.savedAccount = { ...sent, pin: null, password: null };
        this.account.set(this.savedAccount);
        this.isPinSaved.update((isSaved) => isSaved || sent.pin !== null);
        this.isPasswordSaved.update((isSaved) => isSaved || sent.password !== null);
        this.accountForm().reset();
        this.isSaved.set(true);
        this.isEditing.set(false);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (
          problem.code === ApiErrorCode.FiskalyNotEnabled ||
          problem.code === ApiErrorCode.FiskalyTermsNotAccepted
        ) {
          this.refused.emit(problem);
          return undefined;
        }
        return apiFieldErrorsOrGeneral(
          problem,
          this.accountForm,
          (general) => this.error.set(general),
          this.isSaved() ? LOCKED_FIELDS : [],
        );
      } finally {
        this.isLoading.set(false);
      }
    }).then((isSaved) => {
      if (!isSaved || this.error()) {
        showFirstError(this.host, '.electronic__receipt__account__form__error', this.injector);
      }
    });
  }

  protected edit(): void {
    this.isEditing.set(true);
  }

  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancel();
    }
  }

  private cancel(): void {
    clearApiFieldErrors(this.accountForm);
    this.account.set(this.savedAccount);
    this.accountForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  private accountToSend(): ElectronicReceiptAccount {
    const account = this.account();
    return {
      companyName: trimmedText(account.companyName),
      companyAddress: trimmedText(account.companyAddress),
      streetNumber: trimmedText(account.streetNumber),
      postalCode: trimmedText(account.postalCode),
      city: trimmedText(account.city),
      country: trimmedText(account.country),
      taxCode: trimmedText(account.taxCode),
      revenueAgencyCode: trimmedText(account.revenueAgencyCode),
      vatNumber: trimmedText(account.vatNumber),
      pin: trimmedText(account.pin) && account.pin,
      password: trimmedText(account.password) && account.password,
    };
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}
