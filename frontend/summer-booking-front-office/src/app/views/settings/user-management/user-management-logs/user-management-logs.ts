import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DateAdapter } from '@angular/material/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import {
  dateRangeFilters,
  lastMonthsPeriod,
  lastSevenDaysPeriod,
  thisWeekPeriod,
  todayPeriod,
} from '../../../../behaviours/dates/date-periods';
import { DateLanguageBehaviour } from '../../../../behaviours/i18n/date-language.behaviour';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { TableErrorPopup } from '../../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  DateRangeSelect,
  DateRangeSelectOption,
} from '../../../../components/shared/ui/selects/date-range-select/date-range-select';
import { TableHeadingAction } from '../../../../components/shared/ui/tables/table/table-heading-action';
import { Table, TableColumn, TableLoad } from '../../../../components/shared/ui/tables/table/table';
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

@Component({
  selector: 'app-user-management-logs',
  imports: [
    Button,
    DateRangeSelect,
    RouterLink,
    Table,
    TableErrorPopup,
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

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly loadFailed = signal(false);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.propertyId;
    return idProperty ? { idProperty } : null;
  });
  protected readonly dateRange = signal<DateRange | null>(null);
  protected readonly filters = computed(() => dateRangeFilters(this.dateRange()));
  protected readonly backIcon = '/assets/images/GeneralIcon_Previous.svg';
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
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
}

/** The translation of a backend action code (`booking.spot_added`); the code itself when unknown. */
function actionLabelOf(actions: UserManagementLogsTexts['actions'], actionCode: string): string {
  const [module, action] = actionCode.split('.');
  return (action && actions[module]?.[action]) || actionCode;
}
