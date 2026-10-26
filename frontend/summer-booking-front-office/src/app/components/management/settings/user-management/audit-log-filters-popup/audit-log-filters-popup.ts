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
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { AuditLogFilters } from '../../../../../entities/settings/user-management/audit-log-filters';
import { AuditService } from '../../../../../services/api/audit/audit.service';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { AUDIT_ACTIONS } from '../audit-actions';

/** The translated backend action codes: `actions.<module>.<action>`. */
type ActionTexts = Partial<Record<string, Partial<Record<string, string>>>>;

@Component({
  selector: 'app-audit-log-filters-popup',
  imports: [Button, FilledMultiSelect, TranslatePipe],
  templateUrl: './audit-log-filters-popup.html',
  styleUrl: './audit-log-filters-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class AuditLogFiltersPopup extends BasePopup implements OnDestroy {
  readonly filters = input(new AuditLogFilters());
  /** The only users of the Utente list, instead of all the users of the log. */
  readonly onlyUsers = input<readonly ComboboxItem[] | null>(null);
  readonly applied = output<AuditLogFilters>();

  private readonly translateService = inject(TranslateService);
  private readonly audit = inject(AuditService);
  private readonly auth = inject(AuthBehaviour);
  private readonly language = inject(LanguageBehaviour).current;

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly actionCodes = signal<readonly string[]>([]);
  protected readonly idUsers = signal<readonly string[]>([]);
  private readonly users = signal<readonly ComboboxItem[]>([]);
  private usersSubscription?: Subscription;
  private readonly actionTexts = toSignal(
    this.translateService.stream(
      'management.settings.user_management.logs.actions',
    ) as Observable<ActionTexts>,
  );
  protected readonly actionOptions = computed<readonly SelectOption[]>(() =>
    byLabel(actionOptionsOf(this.actionTexts()), this.language()),
  );
  protected readonly userOptions = computed<readonly SelectOption[]>(() =>
    byLabel(
      (this.onlyUsers() ?? this.users()).map((user) => ({ value: user.id, label: user.value })),
      this.language(),
    ),
  );

  constructor() {
    super();
    effect(() => {
      if (this.open()) {
        untracked(() => {
          this.reset();
          if (!this.onlyUsers()) {
            this.loadUsers();
          }
        });
      }
    });
  }

  protected apply(): void {
    this.applied.emit({ actionCodes: [...this.actionCodes()], users: this.chosenUsers() });
    this.close();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(48rem, 95vw)',
      maxWidth: 'none',
      position: { top: '6.875rem' },
      panelClass: 'audit__log__filters__popup__panel',
      autoFocus: false,
    };
  }

  private reset(): void {
    const filters = this.filters();
    this.actionCodes.set(filters.actionCodes);
    this.idUsers.set(filters.users.map((user) => user.id));
  }

  /** With their names, for the chips: from the list of the server, or as they were applied. */
  private chosenUsers(): ComboboxItem[] {
    const known = new Map(
      [...this.filters().users, ...this.users()].map((user) => [user.id, user]),
    );
    return this.idUsers().flatMap((id) => known.get(id) ?? []);
  }

  private loadUsers(): void {
    // A slower answer of an earlier opening must not replace the list of this one.
    this.usersSubscription?.unsubscribe();
    const idProperty = this.auth.user()?.propertyId;
    if (!idProperty) {
      return;
    }
    this.usersSubscription = this.audit.userCombobox(idProperty).subscribe({
      next: (users) => this.users.set(users),
      error: () => this.users.set([]),
    });
  }

  override ngOnDestroy(): void {
    this.usersSubscription?.unsubscribe();
    super.ngOnDestroy();
  }
}

/** One option per action of the catalog: its code, its name in the current language and its icon. */
function actionOptionsOf(texts: ActionTexts | undefined): SelectOption[] {
  return AUDIT_ACTIONS.map(({ code, icon }) => {
    const [module, action] = code.split('.');
    return { value: code, label: (action && texts?.[module]?.[action]) || code, icon };
  });
}

/** A to Z by name, in the alphabet of the current language. */
function byLabel(options: SelectOption[], language: string): SelectOption[] {
  return options.sort((first, second) =>
    first.label.localeCompare(second.label, language, { sensitivity: 'base' }),
  );
}
