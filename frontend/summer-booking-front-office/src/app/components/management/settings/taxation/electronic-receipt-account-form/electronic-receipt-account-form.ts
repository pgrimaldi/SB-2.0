import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  inject,
  output,
  signal,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, TreeValidationResult, form, readonly, submit } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { ElectronicReceiptAccount } from '../../../../../entities/settings/taxation/electronic-receipt-account';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { TaxationService } from '../../../../../services/api/taxation/taxation.service';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';

@Component({
  selector: 'app-electronic-receipt-account-form',
  imports: [Button, FilledTextField, FormField, TranslatePipe],
  templateUrl: './electronic-receipt-account-form.html',
  styleUrl: './electronic-receipt-account-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ElectronicReceiptAccountForm implements UnsavedChanges, OnDestroy {
  readonly back = output<void>();

  private readonly taxation = inject(TaxationService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly backIcon = '/assets/images/Taxation_PanelChevron.svg';
  protected readonly eyeIcons = [
    '/assets/images/Taxation_EyeClose.svg',
    '/assets/images/Taxation_EyeStart.svg',
  ] as const;
  private readonly account = signal(new ElectronicReceiptAccount());
  private savedAccount = new ElectronicReceiptAccount();
  protected readonly isSaved = signal(false);
  protected readonly isEditing = signal(false);
  /** The server has one saved (it is never kept in the page). */
  protected readonly isPinSaved = signal(false);
  protected readonly isPasswordSaved = signal(false);
  protected readonly accountForm = form(this.account, (path) => {
    readonly(path, () => this.isSaved() && !this.isEditing());
    readonly(path.password, () => this.isSaved());
  });
  protected readonly fields = [
    {
      key: 'company_name',
      field: this.accountForm.companyName,
      error: this.validationText.message(this.accountForm.companyName),
      isSecret: false,
      isSecretSaved: null,
      columns: 6,
    },
    {
      key: 'company_address',
      field: this.accountForm.companyAddress,
      error: this.validationText.message(this.accountForm.companyAddress),
      isSecret: false,
      isSecretSaved: null,
      columns: 11,
    },
    {
      key: 'street_number',
      field: this.accountForm.streetNumber,
      error: this.validationText.message(this.accountForm.streetNumber),
      isSecret: false,
      isSecretSaved: null,
      columns: 3,
    },
    {
      key: 'postal_code',
      field: this.accountForm.postalCode,
      error: this.validationText.message(this.accountForm.postalCode),
      isSecret: false,
      isSecretSaved: null,
      columns: 4,
    },
    {
      key: 'city',
      field: this.accountForm.city,
      error: this.validationText.message(this.accountForm.city),
      isSecret: false,
      isSecretSaved: null,
      columns: 8,
    },
    {
      key: 'country',
      field: this.accountForm.country,
      error: this.validationText.message(this.accountForm.country),
      isSecret: false,
      isSecretSaved: null,
      columns: 8,
    },
    {
      key: 'tax_code',
      field: this.accountForm.taxCode,
      error: this.validationText.message(this.accountForm.taxCode),
      isSecret: false,
      isSecretSaved: null,
      columns: 10,
    },
    {
      key: 'vat_number',
      field: this.accountForm.vatNumber,
      error: this.validationText.message(this.accountForm.vatNumber),
      isSecret: false,
      isSecretSaved: null,
      columns: 10,
    },
    {
      key: 'pin',
      field: this.accountForm.pin,
      error: this.validationText.message(this.accountForm.pin),
      isSecret: true,
      isSecretSaved: this.isPinSaved,
      columns: 10,
    },
    {
      key: 'password',
      field: this.accountForm.password,
      error: this.validationText.message(this.accountForm.password),
      isSecret: true,
      isSecretSaved: this.isPasswordSaved,
      columns: 10,
    },
  ];

  protected readonly isLoading = signal(false);
  protected readonly savedIcon = '/assets/images/Taxation_AccountSaved.svg';
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    this.unsaved.watch(this);
  }

  hasUnsavedChanges(): boolean {
    return this.accountForm().dirty();
  }

  protected save(): void {
    const idProperty = this.auth.user()?.idProperty;
    if (this.isLoading() || !idProperty) {
      return;
    }
    void submit(this.accountForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      const sent = this.account();
      try {
        await firstValueFrom(this.taxation.saveElectronicReceiptAccount(idProperty, sent));
        // Once saved, PIN and password leave the page: the fields are empty again.
        this.savedAccount = { ...sent, pin: null, password: null };
        this.account.set(this.savedAccount);
        this.isPinSaved.update((isSaved) => isSaved || sent.pin !== null);
        this.isPasswordSaved.update((isSaved) => isSaved || sent.password !== null);
        this.accountForm().reset();
        this.isSaved.set(true);
        this.isEditing.set(false);
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.accountForm, (problem) =>
          this.error.set(problem),
        );
      } finally {
        this.isLoading.set(false);
      }
    });
  }

  protected edit(): void {
    this.isEditing.set(true);
  }

  protected cancel(): void {
    this.account.set(this.savedAccount);
    this.accountForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}
