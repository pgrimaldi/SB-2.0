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
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, TreeValidationResult, form, readonly, submit } from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, finalize, firstValueFrom, of, switchMap } from 'rxjs';
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
import { UpdateUserRequest } from '../../../../../entities/settings/user-management/update-user-request';
import { UserSelectedItem } from '../../../../../entities/settings/user-management/user-selected-item';
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
  selector: 'app-user-edit-item-popup',
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
  templateUrl: './user-edit-item-popup.html',
  styleUrl: './user-edit-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class UserEditItemPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  readonly idUser = input<string | null>(null);
  readonly updated = output<void>();

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
  protected readonly isFetching = signal(false);
  private readonly draft = signal(new UpdateUserRequest());
  private readonly loaded = signal<UpdateUserRequest | null>(null);
  protected readonly draftForm = form(this.draft, (path) => {
    requiredText(path.email);
    passwordRules(path.password, PASSWORD_MIN_LENGTH);
    readonly(path, () => this.isLoading() || this.isFetching());
  });
  protected readonly emailError = this.validationText.message(this.draftForm.email);
  protected readonly isEmailActiveError = this.validationText.message(this.draftForm.isEmailActive);
  protected readonly passwordError = this.validationText.message(this.draftForm.password);
  protected readonly phoneError = this.validationText.message(this.draftForm.phone);
  protected readonly isPhoneActiveError = this.validationText.message(this.draftForm.isPhoneActive);
  protected readonly roleError = this.validationText.message(this.draftForm.role);
  protected readonly canSave = computed(() => this.loaded() !== null && this.draftForm().valid());
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
      this.open() && loaded !== null && JSON.stringify(this.draft()) !== JSON.stringify(loaded)
    );
  }

  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (this.isLoading() || !idProperty || !this.canSave()) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.identity.updateUser(idProperty, this.draft()));
        this.isLoading.set(false);
        this.close();
        this.updated.emit();
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
      panelClass: 'user__edit__item__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.loaded.set(null);
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
          this.draft.set(request);
          this.draftForm().reset();
        },
        error: (error: unknown) => this.error.set(toApiProblem(error)),
      });
  }

  private toUpdateRequest(item: UserSelectedItem): UpdateUserRequest {
    return {
      idUser: item.idUser,
      email: item.email,
      isEmailActive: item.isEmailActive,
      password: null,
      phone: item.phone,
      isPhoneActive: item.isPhoneActive,
      role: item.role,
    };
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
