import {
  ChangeDetectionStrategy,
  Component,
  WritableSignal,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { Location } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { DateAdapter } from '@angular/material/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, firstValueFrom, map, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import {
  dateRangeFilters,
  lastMonthsPeriod,
  lastSevenDaysPeriod,
  thisWeekPeriod,
  todayPeriod,
} from '../../../../behaviours/dates/date-periods';
import { FileDownloadBehaviour } from '../../../../behaviours/files/file-download.behaviour';
import { DateLanguageBehaviour } from '../../../../behaviours/i18n/date-language.behaviour';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { AuditLogFiltersPopup } from '../../../../components/management/settings/user-management/audit-log-filters-popup/audit-log-filters-popup';
import {
  FilterChip,
  FilterChips,
} from '../../../../components/shared/ui/chips/filter-chips/filter-chips';
import { AlertPopup } from '../../../../components/shared/ui/dialogs/alert-popup/alert-popup';
import { TableErrorPopup } from '../../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  DateRangeSelect,
  DateRangeSelectOption,
} from '../../../../components/shared/ui/selects/date-range-select/date-range-select';
import { TableFilterBar } from '../../../../components/shared/ui/tables/table/table-filter-bar';
import { TableHeadingAction } from '../../../../components/shared/ui/tables/table/table-heading-action';
import { Table, TableColumn, TableLoad } from '../../../../components/shared/ui/tables/table/table';
import { PageFilter } from '../../../../entities/pagination/page';
import { ComboboxItem } from '../../../../entities/combobox/combobox-item';
import { AuditLogFilters } from '../../../../entities/settings/user-management/audit-log-filters';
import { AuditLogUserState } from '../../../../entities/settings/user-management/audit-log-user-state';
import { AuditLogFiltersRequest } from '../../../../entities/settings/user-management/audit-log-filters-request';
import { DateRange } from '../../../../entities/shared/date-range';
import { AuditService } from '../../../../services/api/audit/audit.service';

interface UserManagementLogsTexts {
  date_filter: {
    all: string;
    today: string;
    this_week: string;
    last_seven_days: string;
    last_month: string;
    last_six_months: string;
  };
  table: {
    booking_number: string;
    user_number: string;
    full_name: string;
    spot: string;
    action: string;
    occurred_at: string;
  };
  actions: Partial<Record<string, Partial<Record<string, string>>>>;
  chips: {
    action: string;
    user: string;
    more_options: string;
  };
}

interface AuditLogFilterChip extends FilterChip {
  /** What the filters become without this chip. */
  emptied: Partial<AuditLogFilters>;
}

interface AuditEntryRow {
  idAuditEntry: string | null;
  bookingNumber: string | null;
  userNumber: string | null;
  actorName: string | null;
  spotLabel: string | null;
  actionCode: string | null;
  occurredAt: string | null;
}

const EXPORT_FALLBACK_NAME = 'log';
const PRINT_FALLBACK_NAME = 'log-print';

@Component({
  selector: 'app-user-management-logs',
  imports: [
    AlertPopup,
    AuditLogFiltersPopup,
    Button,
    DateRangeSelect,
    FilterChips,
    RouterLink,
    Table,
    TableErrorPopup,
    TableFilterBar,
    TableHeadingAction,
    TranslatePipe,
  ],
  templateUrl: './user-management-logs.html',
  styleUrl: './user-management-logs.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManagementLogs {
  private readonly auth = inject(AuthBehaviour);
  private readonly audit = inject(AuditService);
  private readonly translateService = inject(TranslateService);
  private readonly dateLanguage = inject(DateLanguageBehaviour);
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
  private readonly location = inject(Location);
  private readonly fileDownload = inject(FileDownloadBehaviour);
  private readonly table = viewChild.required<Table<AuditEntryRow, { idProperty: string }>>(Table);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly loadFailed = signal(false);
  protected readonly isExporting = signal(false);
  protected readonly isPrinting = signal(false);
  protected readonly isNotImplementedShown = signal(false);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly dateRange = signal<DateRange | null>(null);
  protected readonly isFiltersOpen = signal(false);
  /** From the Log button of a user row: the only user of the Utente filter while it is chosen. */
  private readonly logUser = signal(logUserOf(this.location.getState()));
  protected readonly logFilters = signal(this.navigationFilters());
  protected readonly onlyUsers = computed(() => {
    const logUser = this.logUser();
    return logUser ? [logUser] : null;
  });
  private readonly logsTexts = toSignal(
    this.translateService.stream(
      'management.settings.user_management.logs',
    ) as Observable<UserManagementLogsTexts>,
  );
  /** One chip for each filter with a choice; with more choices only "more options". */
  protected readonly chips = computed<readonly AuditLogFilterChip[]>(() => {
    const texts = this.logsTexts();
    const filters = this.logFilters();
    if (!texts) {
      return [];
    }
    const chip = (
      name: string,
      labels: readonly string[],
      emptied: Partial<AuditLogFilters>,
    ): AuditLogFilterChip[] =>
      labels.length
        ? [
            {
              label: `${name}: ${labels.length > 1 ? texts.chips.more_options : labels[0]}`,
              emptied,
            },
          ]
        : [];
    return [
      ...chip(
        texts.chips.action,
        filters.actionCodes.map((code) => actionLabelOf(texts.actions, code)),
        { actionCodes: [] },
      ),
      ...chip(
        texts.chips.user,
        filters.users.map((user) => user.value),
        { users: [] },
      ),
    ];
  });
  /** Only a change of the values loads the table again, not a user's name. */
  protected readonly filters = computed(
    () => [...dateRangeFilters(this.dateRange()), ...pageFiltersOf(this.logFilters())],
    { equal: sameFilters },
  );
  protected readonly hasFilters = computed(
    () =>
      this.dateRange() !== null ||
      this.chips().length > 0 ||
      !!this.table().currentRequest()?.search,
  );
  protected readonly chipIcons = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly backIcon = '/assets/images/GeneralIcon_Previous.svg';
  protected readonly titleIcon = '/assets/images/UserManagement_LogTitle.svg';
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  /** In the select's order: [open, previous month, next month]. */
  protected readonly dateFilterIcons = [
    '/assets/images/GeneralIcon_DateFilterOpen.svg',
    '/assets/images/GeneralIcon_Previous.svg',
    '/assets/images/GeneralIcon_Next.svg',
  ] as const;
  /** In the table's order: [search magnifier, search X]. */
  protected readonly tableIcons = [
    '/assets/images/GeneralIcon_Search.svg',
    '/assets/images/GeneralIcon_Clear.svg',
  ] as const;
  private readonly dateFilterTexts = toSignal(
    this.translateService.stream(
      'management.settings.user_management.logs.date_filter',
    ) as Observable<UserManagementLogsTexts['date_filter']>,
    {
      initialValue: {
        all: '',
        today: '',
        this_week: '',
        last_seven_days: '',
        last_month: '',
        last_six_months: '',
      },
    },
  );
  protected readonly dateOptions = computed<readonly DateRangeSelectOption[]>(() => {
    const texts = this.dateFilterTexts();
    const today = this.dateAdapter.today();
    return [
      { label: texts.all, range: null },
      { label: texts.today, range: todayPeriod(today) },
      { label: texts.this_week, range: thisWeekPeriod(today) },
      { label: texts.last_seven_days, range: lastSevenDaysPeriod(today) },
      { label: texts.last_month, range: lastMonthsPeriod(today, 1) },
      { label: texts.last_six_months, range: lastMonthsPeriod(today, 6) },
    ];
  });
  private readonly headers = toSignal(
    this.translateService.stream('management.settings.user_management.logs.table') as Observable<
      UserManagementLogsTexts['table']
    >,
    {
      initialValue: {
        booking_number: '',
        user_number: '',
        full_name: '',
        spot: '',
        action: '',
        occurred_at: '',
      },
    },
  );
  protected readonly columns = computed<readonly TableColumn<AuditEntryRow>[]>(() => [
    { field: 'bookingNumber', header: this.headers().booking_number, sortable: true },
    { field: 'userNumber', header: this.headers().user_number, sortable: true },
    { field: 'actorName', header: this.headers().full_name, sortable: true },
    { field: 'spotLabel', header: this.headers().spot, sortable: true },
    { field: 'actionCode', header: this.headers().action, width: 24, sortable: true },
    { field: 'occurredAt', header: this.headers().occurred_at, width: 18, sortable: true },
  ]);

  constructor() {
    this.nameLogUser();
  }

  protected readonly load: TableLoad<{ idProperty: string }, AuditEntryRow> = (request) =>
    this.audit.entriesList(request).pipe(
      switchMap((page) =>
        (
          this.translateService.get(
            'management.settings.user_management.logs',
          ) as Observable<UserManagementLogsTexts>
        ).pipe(
          map((texts) => {
            const moment = new Intl.DateTimeFormat(this.dateLanguage.locale(), {
              day: '2-digit',
              month: '2-digit',
              year: 'numeric',
              hour: '2-digit',
              minute: '2-digit',
              hourCycle: 'h23',
            });
            return {
              total: page.total,
              rows: page.rows.map((item): AuditEntryRow => ({
                ...item,
                actionCode: item.actionCode && actionLabelOf(texts.actions, item.actionCode),
                occurredAt: item.occurredAt ? moment.format(new Date(item.occurredAt)) : null,
              })),
            };
          }),
        ),
      ),
    );

  /** Without the user of the Log button, the Utente filter offers all the users again. */
  protected applyFilters(filters: AuditLogFilters): void {
    this.logFilters.set(filters);
    if (!filters.users.some(({ id }) => id === this.logUser()?.id)) {
      this.logUser.set(null);
    }
  }

  protected removeChip(chip: AuditLogFilterChip): void {
    this.applyFilters({ ...this.logFilters(), ...chip.emptied });
  }

  protected clearFilters(): void {
    this.applyFilters(new AuditLogFilters());
    this.dateRange.set(null);
    this.table().clearSearch();
  }

  protected exportLog(): void {
    void this.download(this.isExporting, this.audit.exportLog, EXPORT_FALLBACK_NAME);
  }

  protected printLog(): void {
    void this.download(this.isPrinting, this.audit.printLog, PRINT_FALLBACK_NAME);
  }

  /** From the Log button of a user row: the log starts filtered on that user. */
  private navigationFilters(): AuditLogFilters {
    const logUser = this.logUser();
    return logUser ? { actionCodes: [], users: [logUser] } : new AuditLogFilters();
  }

  /** The row brings the email: the name comes from the users of the log, matched by id. */
  private nameLogUser(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (!this.logUser() || !idProperty) {
      return;
    }
    this.audit.userCombobox(idProperty).subscribe({
      next: (users) => {
        const named = users.find(({ id }) => id === this.logUser()?.id);
        if (!named) {
          return;
        }
        this.logUser.set(named);
        this.logFilters.update((filters) => ({
          ...filters,
          users: filters.users.map((user) => (user.id === named.id ? named : user)),
        }));
      },
      error: () => undefined,
    });
  }

  /** One file at a time, with the search, filters and sorting the table is showing. */
  private async download(
    isRunning: WritableSignal<boolean>,
    call: (request: AuditLogFiltersRequest) => Observable<HttpResponse<Blob>>,
    fallbackName: string,
  ): Promise<void> {
    const request = this.table().currentRequest();
    if (!request || this.isExporting() || this.isPrinting()) {
      return;
    }
    const { idProperty, search, filters, sortField, sortDirection } = request;
    isRunning.set(true);
    try {
      const file = await firstValueFrom(
        call({ idProperty, search, filters, sortField, sortDirection }),
      );
      this.fileDownload.save(file, fallbackName);
    } catch {
      this.isNotImplementedShown.set(true);
    } finally {
      isRunning.set(false);
    }
  }
}

/** The translation of a backend action code (`booking.spot_added`); the code itself when unknown. */
function actionLabelOf(actions: UserManagementLogsTexts['actions'], actionCode: string): string {
  const [module, action] = actionCode.split('.');
  return (action && actions[module]?.[action]) || actionCode;
}

/** One filter per field with its chosen values (alternatives); a field with no choice is left out. */
function pageFiltersOf(filters: AuditLogFilters): PageFilter[] {
  const all: PageFilter[] = [
    { field: 'actionCode', value: [...filters.actionCodes] },
    { field: 'idUser', value: filters.users.map(({ id }) => id) },
  ];
  return all.filter(({ value }) => value.length);
}

function sameFilters(first: readonly PageFilter[], second: readonly PageFilter[]): boolean {
  return JSON.stringify(first) === JSON.stringify(second);
}

function logUserOf(state: unknown): ComboboxItem | null {
  const logUser = (state as Partial<AuditLogUserState> | null)?.logUser;
  return typeof logUser?.id === 'string' && typeof logUser.value === 'string'
    ? { id: logUser.id, value: logUser.value }
    : null;
}
