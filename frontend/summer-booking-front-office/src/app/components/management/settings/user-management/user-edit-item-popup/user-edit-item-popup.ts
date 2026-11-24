import { HttpStatusCode } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  MAX_LENGTH,
  TreeValidationResult,
  disabled,
  form,
  metadata,
  required,
  submit,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, finalize, firstValueFrom } from 'rxjs';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import {
  apiFieldError,
  apiFieldErrorsOrGeneral,
} from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import {
  PhoneCountry,
  PhonePrefixBehaviour,
} from '../../../../../behaviours/i18n/phone-prefix.behaviour';
import { PhonePrefix } from '../../../../../entities/enums/phone-prefix';
import { UserRole } from '../../../../../entities/enums/user-role';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  PASSWORD_MAX_LENGTH,
  PASSWORD_MIN_LENGTH,
  PHONE_MAX_LENGTH,
} from '../../../../../entities/settings/user-management/create-user-request';
import { UpdateUserRequest } from '../../../../../entities/settings/user-management/update-user-request';
import { UserSelectedItem } from '../../../../../entities/settings/user-management/user-selected-item';
import { MAX_EMAIL_LENGTH } from '../../../../../entities/shared/email-address';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { IdentityService } from '../../../../../services/api/identity/identity.service';
import {
  emailAddress,
  passwordRules,
  phoneNumber,
  requiredText,
} from '../../../../shared/forms/validators';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { InfoPopup } from '../../../../shared/ui/dialogs/info-popup/info-popup';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTextFieldLabelAction } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field-label-action';
import { FilledIconSelect } from '../../../../shared/ui/selects/filled-icon-select/filled-icon-select';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { ReadErrorPopup } from '../../../read-error-popup/read-error-popup';
import { VersionConflictPopup } from '../../../version-conflict-popup/version-conflict-popup';

interface UserRoleTexts {
  admin: string;
  lifeguard: string;
  manager: string;
  cash_operator: string;
  daily_cash_operator: string;
  user1: string;
  waiter: string;
  affiliate: string;
  advanced_lifeguard: string;
  marketing: string;
}

const ASSIGNABLE_USER_ROLES: readonly { role: UserRole; key: keyof UserRoleTexts }[] = [
  { role: UserRole.Admin, key: 'admin' },
  { role: UserRole.Lifeguard, key: 'lifeguard' },
  { role: UserRole.Manager, key: 'manager' },
  { role: UserRole.CashOperator, key: 'cash_operator' },
  { role: UserRole.DailyCashOperator, key: 'daily_cash_operator' },
  { role: UserRole.User1, key: 'user1' },
  { role: UserRole.Waiter, key: 'waiter' },
  { role: UserRole.Affiliate, key: 'affiliate' },
  { role: UserRole.AdvancedLifeguard, key: 'advanced_lifeguard' },
  { role: UserRole.Marketing, key: 'marketing' },
];

@Component({
  selector: 'app-user-edit-item-popup',
  imports: [
    Button,
    Checkbox,
    FilledIconSelect,
    FilledSelect,
    FilledTextField,
    FilledTextFieldLabelAction,
    FormField,
    IconButton,
    InfoPopup,
    ReadErrorPopup,
    SectionTitle,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './user-edit-item-popup.html',
  styleUrl: './user-edit-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class UserEditItemPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  readonly idUser = input<string | null>(null);
  readonly updated = output<void>();
  /** Every error of the read and of the save, e.g. for the page to reload the table on a 404. */
  readonly failed = output<ApiProblem>();

  private readonly identity = inject(IdentityService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly phonePrefix = inject(PhonePrefixBehaviour);

  protected readonly ButtonSize = ButtonSize;
  /** [content shown, content hidden]. */
  protected readonly eyeIcons = [
    '/assets/images/GeneralIcon_EyeClose.svg',
    '/assets/images/GeneralIcon_EyeStart.svg',
  ] as const;
  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [X, before the title]. */
  protected readonly infoPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_InfoTitle.svg',
  ] as const;
  protected readonly showingInfo = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly isFetching = signal(false);
  private readonly draft = signal(new UpdateUserRequest());
  private readonly loaded = signal<UpdateUserRequest | null>(null);
  private version: string | null = null;
  protected readonly draftForm = form(this.draft, (path) => {
    requiredText(path.email);
    emailAddress(path.email);
    metadata(path.email, MAX_LENGTH, () => MAX_EMAIL_LENGTH);
    passwordRules(path.password, PASSWORD_MIN_LENGTH);
    metadata(path.password, MAX_LENGTH, () => PASSWORD_MAX_LENGTH);
    phoneNumber(path.phone, PHONE_MAX_LENGTH);
    required(path.role);
    disabled(path, () => this.isLoading() || this.isFetching());
  });
  protected readonly emailError = this.validationText.message(this.draftForm.email);
  protected readonly emailIsContactError = this.validationText.message(
    this.draftForm.emailIsContact,
  );
  protected readonly passwordError = this.validationText.message(this.draftForm.password);
  protected readonly phoneCountryCodeError = this.validationText.message(
    this.draftForm.phoneCountryCode,
  );
  protected readonly phoneError = this.validationText.message(this.draftForm.phone);
  protected readonly phoneIsContactError = this.validationText.message(
    this.draftForm.phoneIsContact,
  );
  protected readonly roleError = this.validationText.message(this.draftForm.role);
  protected readonly canSave = computed(() => this.loaded() !== null && this.draftForm().valid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly readError = signal<ApiProblem | null>(null);
  protected readonly isConflictShown = signal(false);
  protected readonly errorMessage = this.errorText.message(this.error);
  private readonly roleTexts = toSignal(
    this.translateService.stream(
      'management.settings.user_management.role',
    ) as Observable<UserRoleTexts>,
  );
  protected readonly prefixOptions = this.phonePrefix.options;
  protected readonly phoneCountry = computed(() =>
    this.phonePrefix.countryOf(this.draft().phoneCountryCode),
  );
  protected readonly roleOptions = computed<readonly SelectOption<UserRole>[]>(() => {
    const texts = this.roleTexts();
    return ASSIGNABLE_USER_ROLES.map(({ role, key }) => ({
      value: role,
      label: texts?.[key] ?? '',
    }));
  });

  constructor() {
    super();
    this.unsaved.watch(this);
    // The typed password never stays in memory once the popup is closed.
    effect((onCleanup) => {
      const idUser = this.idUser();
      const isOpen = this.open();
      untracked(() => this.reset());
      if (isOpen && idUser) {
        const subscription = untracked(() => this.loadSelectedUser(idUser));
        onCleanup(() => subscription.unsubscribe());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    const loaded = this.loaded();
    return (
      this.open() && loaded !== null && comparableJson(this.draft()) !== comparableJson(loaded)
    );
  }

  protected save(): void {
    if (this.isLoading() || !this.canSave()) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.identity.updateUser(this.sent(), this.version));
        this.isLoading.set(false);
        this.close();
        this.updated.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.failed.emit(problem);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictShown.set(true);
          this.isLoading.set(false);
          this.close();
          return undefined;
        }
        return this.errorsOf(problem);
      } finally {
        this.isLoading.set(false);
      }
    });
  }

  protected setPhoneCountry(country: PhoneCountry): void {
    const prefix = this.draftForm.phoneCountryCode();
    prefix.value.set(PhonePrefix[country]);
    prefix.markAsDirty();
  }

  protected async leave(): Promise<void> {
    if (!this.isLoading() && (await this.unsaved.confirmDiscard(this))) {
      this.close();
    }
  }

  protected override isCloseDisabled(): boolean {
    return this.isLoading();
  }

  protected override closeRequested(): void {
    void this.leave();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(42.3125rem, 95vw)',
      maxWidth: 'none',
      maxHeight: '95vh',
      panelClass: 'user__edit__item__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  protected retryRead(): void {
    this.open.set(true);
  }

  private reset(): void {
    this.loaded.set(null);
    this.version = null;
    this.draft.set(new UpdateUserRequest());
    this.draftForm().reset();
    this.error.set(null);
  }

  private loadSelectedUser(idUser: string): Subscription {
    this.isFetching.set(true);
    return this.identity
      .selectedUser(idUser)
      .pipe(finalize(() => this.isFetching.set(false)))
      .subscribe({
        next: (item) => {
          const request = this.toUpdateRequest(item);
          this.loaded.set(structuredClone(request));
          this.version = item.version;
          this.draft.set(request);
          this.draftForm().reset();
        },
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.failed.emit(problem);
          this.readError.set(problem);
          this.close();
        },
      });
  }

  private toUpdateRequest(item: UserSelectedItem): UpdateUserRequest {
    return {
      idUser: item.idUser,
      email: item.email,
      emailIsContact: item.emailIsContact,
      password: null,
      phoneCountryCode: item.phoneCountryCode,
      phone: item.phone,
      phoneIsContact: item.phoneIsContact,
      role: item.role,
    };
  }

  /** The prefix goes only with a number. */
  private sent(): UpdateUserRequest {
    const draft = this.draft();
    return {
      ...draft,
      phoneCountryCode: this.phonePrefix.prefixFor(draft.phone, draft.phoneCountryCode),
    };
  }

  /** Codes that come without `errors[]` but are about one field go under it. */
  private errorsOf(problem: ApiProblem): TreeValidationResult {
    if (problem.code === ApiErrorCode.AccountEmailTaken) {
      return apiFieldError(this.draftForm.email, problem.code, problem.args);
    }
    if (
      problem.code === ApiErrorCode.AccountRoleNotAssignable &&
      problem.status === HttpStatusCode.UnprocessableEntity
    ) {
      return apiFieldError(this.draftForm.role, problem.code, problem.args);
    }
    return apiFieldErrorsOrGeneral(problem, this.draftForm, (general) => this.error.set(general));
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
