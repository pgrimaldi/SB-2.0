import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, TreeValidationResult, form, readonly, submit } from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { UserRole } from '../../../../../entities/enums/user-role';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { CreateUserRequest } from '../../../../../entities/settings/user-management/create-user-request';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { IdentityService } from '../../../../../services/api/identity/identity.service';
import { passwordRules, requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTextFieldLabelAction } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field-label-action';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

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

const PASSWORD_MIN_LENGTH = 8;

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
  selector: 'app-user-add-item-popup',
  imports: [
    Button,
    Checkbox,
    FilledSelect,
    FilledTextField,
    FilledTextFieldLabelAction,
    FormField,
    IconButton,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './user-add-item-popup.html',
  styleUrl: './user-add-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class UserAddItemPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  readonly added = output<void>();

  private readonly identity = inject(IdentityService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly translateService = inject(TranslateService);

  protected readonly ButtonSize = ButtonSize;
  /** [content shown, content hidden]. */
  protected readonly eyeIcons = [
    '/assets/images/GeneralIcon_EyeClose.svg',
    '/assets/images/GeneralIcon_EyeStart.svg',
  ] as const;
  protected readonly isLoading = signal(false);
  private readonly draft = signal(new CreateUserRequest());
  protected readonly draftForm = form(this.draft, (path) => {
    requiredText(path.email);
    requiredText(path.password);
    passwordRules(path.password, PASSWORD_MIN_LENGTH);
    readonly(path, () => this.isLoading());
  });
  protected readonly emailError = this.validationText.message(this.draftForm.email);
  protected readonly isEmailActiveError = this.validationText.message(this.draftForm.isEmailActive);
  protected readonly passwordError = this.validationText.message(this.draftForm.password);
  protected readonly phoneError = this.validationText.message(this.draftForm.phone);
  protected readonly isPhoneActiveError = this.validationText.message(this.draftForm.isPhoneActive);
  protected readonly roleError = this.validationText.message(this.draftForm.role);
  protected readonly canAdd = computed(() => this.draftForm().valid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private readonly roleTexts = toSignal(
    this.translateService.stream(
      'management.settings.user_management.role',
    ) as Observable<UserRoleTexts>,
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
    effect(() => {
      this.open();
      untracked(() => this.reset());
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && JSON.stringify(this.draft()) !== JSON.stringify(new CreateUserRequest());
  }

  protected add(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (this.isLoading() || !idProperty || !this.canAdd()) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.identity.createUser(idProperty, this.draft()));
        this.isLoading.set(false);
        this.close();
        this.added.emit();
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.draftForm, (problem) =>
          this.error.set(problem),
        );
      } finally {
        this.isLoading.set(false);
      }
    });
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
      panelClass: 'user__add__item__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.draft.set(new CreateUserRequest());
    this.draftForm().reset();
    this.error.set(null);
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
