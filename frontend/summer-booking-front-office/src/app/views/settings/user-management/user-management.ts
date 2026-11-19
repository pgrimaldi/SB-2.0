import { HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { DeleteItemsPopup } from '../../../components/management/delete-items-popup/delete-items-popup';
import { UserAddItemPopup } from '../../../components/management/settings/user-management/user-add-item-popup/user-add-item-popup';
import { UserEditItemPopup } from '../../../components/management/settings/user-management/user-edit-item-popup/user-edit-item-popup';
import { Button, ButtonSize } from '../../../components/shared/ui/buttons/button/button';
import { standardRowActions } from '../../../components/shared/tables/standard-row-actions';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  Table,
  TableColumn,
  TableLoad,
  TableRowAction,
} from '../../../components/shared/ui/tables/table/table';
import { PageTitle } from '../../../components/shared/ui/titles/page-title/page-title';
import { UserRole } from '../../../entities/enums/user-role';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { AuditLogUserState } from '../../../entities/settings/user-management/audit-log-user-state';
import { IdentityService } from '../../../services/api/identity/identity.service';

interface UserManagementTexts {
  table: {
    email: string;
    mobile_phone: string;
    account_type: string;
  };
  role: {
    super_admin: string;
    admin: string;
    lifeguard: string;
    manager: string;
    cash_operator: string;
    daily_cash_operator: string;
    website_admin: string;
    user1: string;
    waiter: string;
    affiliate: string;
    advanced_lifeguard: string;
    marketing: string;
  };
}

interface UserRow {
  idUser: string | null;
  email: string | null;
  phone: string | null;
  role: string | null;
  /** The owner, the user signed in and the Website admins are not managed from this page. */
  isLocked: boolean;
}

/** Roles the backend does not let the panel manage (`Role.IsAssignableViaPanel`). */
const LOCKED_ROLES: readonly string[] = [UserRole.SuperAdmin, UserRole.WebsiteAdmin];

const USER_ROLE_KEYS: Record<UserRole, keyof UserManagementTexts['role']> = {
  [UserRole.SuperAdmin]: 'super_admin',
  [UserRole.Admin]: 'admin',
  [UserRole.Lifeguard]: 'lifeguard',
  [UserRole.Manager]: 'manager',
  [UserRole.CashOperator]: 'cash_operator',
  [UserRole.DailyCashOperator]: 'daily_cash_operator',
  [UserRole.WebsiteAdmin]: 'website_admin',
  [UserRole.User1]: 'user1',
  [UserRole.Waiter]: 'waiter',
  [UserRole.Affiliate]: 'affiliate',
  [UserRole.AdvancedLifeguard]: 'advanced_lifeguard',
  [UserRole.Marketing]: 'marketing',
};

@Component({
  selector: 'app-user-management',
  imports: [
    Button,
    DeleteItemsPopup,
    PageTitle,
    Table,
    TableErrorPopup,
    TranslatePipe,
    UserAddItemPopup,
    UserEditItemPopup,
  ],
  templateUrl: './user-management.html',
  styleUrl: './user-management.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManagement {
  private readonly auth = inject(AuthBehaviour);
  private readonly identity = inject(IdentityService);
  private readonly translateService = inject(TranslateService);
  private readonly router = inject(Router);

  protected readonly ButtonSize = ButtonSize;
  protected readonly loadFailed = signal(false);
  protected readonly adding = signal(false);
  protected readonly editing = signal(false);
  protected readonly idUserToEdit = signal<string | null>(null);
  protected readonly deleting = signal(false);
  protected readonly idUsersToDelete = signal<readonly string[]>([]);
  protected readonly userNameToDelete = signal<string | null>(null);
  protected readonly params = computed<object | null>(() =>
    this.auth.user()?.propertyId ? {} : null,
  );
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  private readonly logIcon = '/assets/images/UserManagement_Log.svg';
  protected readonly viewLogsIcon = ['/assets/images/UserManagement_LogWhite.svg'] as const;
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  /** In the table's order: [search magnifier, search X, duplicate chosen, delete chosen, create]. */
  protected readonly tableIcons = [
    '/assets/images/GeneralIcon_Search.svg',
    '/assets/images/GeneralIcon_Clear.svg',
    '',
    '',
    '/assets/images/GeneralIcon_Add.svg',
  ] as const;
  private readonly headers = toSignal(
    this.translateService.stream('management.settings.user_management.table') as Observable<
      UserManagementTexts['table']
    >,
    {
      initialValue: {
        email: '',
        mobile_phone: '',
        account_type: '',
      },
    },
  );
  protected readonly columns = computed<readonly TableColumn<UserRow>[]>(() => [
    { field: 'email', header: this.headers().email, sortable: true },
    { field: 'phone', header: this.headers().mobile_phone, sortable: true },
    { field: 'role', header: this.headers().account_type, sortable: true },
  ]);
  private readonly userNotEditable = toSignal(
    this.translateService.stream(
      'management.settings.user_management.user_not_editable',
    ) as Observable<string>,
    { initialValue: '' },
  );
  private readonly standardActions = standardRowActions<UserRow>(
    {
      delete: ({ idUser, email }) => idUser && this.askDelete(idUser, email),
      edit: ({ idUser }) => idUser && this.openEdit(idUser),
    },
    {
      delete: { isHidden: ({ isLocked }) => isLocked },
      edit: { isDisabled: ({ isLocked }) => isLocked, disabledLabel: this.userNotEditable },
    },
  );
  private readonly logText = toSignal(
    this.translateService.stream('management.settings.user_management.log') as Observable<string>,
    { initialValue: '' },
  );
  protected readonly rowActions = computed<readonly TableRowAction<UserRow>[]>(() => [
    {
      label: '',
      icon: this.logIcon,
      text: this.logText(),
      action: ({ idUser, email }) => idUser && this.openUserLogs(idUser, email),
    },
    ...this.standardActions(),
  ]);

  protected readonly deleteUser = ([idUser]: string[]) => this.identity.deleteUser(idUser);

  protected readonly load: TableLoad<object, UserRow> = (request) =>
    this.identity.usersList(request).pipe(
      switchMap((page) =>
        (
          this.translateService.get(
            'management.settings.user_management',
          ) as Observable<UserManagementTexts>
        ).pipe(
          map((texts) => ({
            total: page.total,
            rows: page.rows.map((item): UserRow => ({
              idUser: item.idUser,
              email: item.email,
              phone: item.phone
                ? [phonePrefixOf(item.phoneCountryCode), item.phone].filter(Boolean).join(' ')
                : null,
              role: item.role ? (texts.role[USER_ROLE_KEYS[item.role]] ?? item.role) : null,
              isLocked:
                LOCKED_ROLES.includes(item.role ?? '') || item.idUser === this.auth.user()?.id,
            })),
          })),
        ),
      ),
    );

  protected askDelete(idUser: string, email: string | null): void {
    this.idUsersToDelete.set([idUser]);
    this.userNameToDelete.set(email);
    this.deleting.set(true);
  }

  protected openEdit(idUser: string): void {
    this.idUserToEdit.set(idUser);
    this.editing.set(true);
  }

  /** A user deleted meanwhile by someone else: the table shows it is gone. */
  protected reloadIfGone(problem: ApiProblem, table: { reload(): void }): void {
    if (problem.status === HttpStatusCode.NotFound) {
      table.reload();
    }
  }

  protected openLogs(): void {
    void this.router.navigateByUrl('/settings/user-management/logs');
  }

  /** The log filtered on the user; the id travels in the navigation state, not in the address. */
  private openUserLogs(idUser: string, email: string | null): void {
    const state: AuditLogUserState = { logUser: { id: idUser, value: email ?? '' } };
    void this.router.navigateByUrl('/settings/user-management/logs', { state });
  }
}

/** The prefix with one `+`, also when the saved value already has it. */
function phonePrefixOf(prefix: string | null): string | null {
  const digits = prefix?.trim().replace(/^\+/, '');
  return digits ? `+${digits}` : null;
}
