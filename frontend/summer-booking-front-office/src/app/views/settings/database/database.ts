import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { DateAdapter } from '@angular/material/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map, of, switchMap, tap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import {
  ExistingBookingFilterTexts,
  filterOptionsOf,
} from '../../../behaviours/database/existing-booking-filter-options';
import {
  dateRangeFilters,
  nextMonthsPeriod,
  nextWeekPeriod,
  thisWeekPeriod,
  todayPeriod,
} from '../../../behaviours/dates/date-periods';
import {
  DATE_FORMATS,
  DateLanguageBehaviour,
} from '../../../behaviours/i18n/date-language.behaviour';
import { ConfirmDeleteBookingPopup } from '../../../components/management/settings/database/confirm-delete-booking-popup/confirm-delete-booking-popup';
import { I18nText } from '../../../components/i18n/i18n-text/i18n-text';
import { ExistingBookingFiltersPopup } from '../../../components/management/settings/database/existing-booking-filters-popup/existing-booking-filters-popup';
import {
  FilterChip,
  FilterChips,
} from '../../../components/shared/ui/chips/filter-chips/filter-chips';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../components/shared/ui/buttons/button/button';
import { AlertPopup } from '../../../components/shared/ui/dialogs/alert-popup/alert-popup';
import { ConfirmPopup } from '../../../components/shared/ui/dialogs/confirm-popup/confirm-popup';
import { MessagePopup } from '../../../components/shared/ui/dialogs/message-popup/message-popup';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { PageSpinner } from '../../../components/shared/ui/spinners/page-spinner/page-spinner';
import {
  DateRangeSelect,
  DateRangeSelectOption,
} from '../../../components/shared/ui/selects/date-range-select/date-range-select';
import { TableFilterBar } from '../../../components/shared/ui/tables/table/table-filter-bar';
import { TableHeadingAction } from '../../../components/shared/ui/tables/table/table-heading-action';
import {
  Table,
  TableColumn,
  TableLoad,
  TableRowId,
} from '../../../components/shared/ui/tables/table/table';
import {
  AgreementBookingFilter,
  BookingTypologyFilter,
  ExistingBookingFilters,
  PaymentStatusFilter,
} from '../../../entities/database/existing-booking-filters';
import { ExistingBookingItem } from '../../../entities/database/existing-booking-item';
import { ReservationKind } from '../../../entities/enums/reservation-kind';
import { ReservationStatus } from '../../../entities/enums/reservation-status';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { PageFilter } from '../../../entities/pagination/page';
import { DateRange } from '../../../entities/shared/date-range';
import { DatabaseService } from '../../../services/api/database/database.service';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';

interface DatabaseTexts {
  table: {
    customer: string;
    reservation_kind: string;
    date_from: string;
    date_to: string;
    agreement: string;
    reservation_status: string;
  };
  reservation_kind: { spot: string; service: string };
  reservation_status: { pending: string; confirmed: string; cancelled: string; no_show: string };
  date_filter: {
    all: string;
    today: string;
    this_week: string;
    next_seven_days: string;
    next_month: string;
    next_six_months: string;
  };
}

interface BookingFilterChip extends FilterChip {
  /** What the filters become without this chip. */
  emptied: Partial<ExistingBookingFilters>;
}

/** The row as the table shows it: dates and enums already written in the current language. */
interface ExistingBookingRow {
  idBooking: string | null;
  customerName: string | null;
  reservationKind: string | null;
  dateFrom: string | null;
  dateTo: string | null;
  agreementName: string | null;
  reservationStatus: string | null;
}

const RESERVATION_KIND_KEYS: Record<ReservationKind, keyof DatabaseTexts['reservation_kind']> = {
  [ReservationKind.Spot]: 'spot',
  [ReservationKind.Service]: 'service',
};

const RESERVATION_STATUS_KEYS: Record<
  ReservationStatus,
  keyof DatabaseTexts['reservation_status']
> = {
  [ReservationStatus.Pending]: 'pending',
  [ReservationStatus.Confirmed]: 'confirmed',
  [ReservationStatus.Cancelled]: 'cancelled',
  [ReservationStatus.NoShow]: 'no_show',
};

@Component({
  selector: 'app-database',
  imports: [
    AlertPopup,
    Button,
    ConfirmDeleteBookingPopup,
    ConfirmPopup,
    DateRangeSelect,
    ExistingBookingFiltersPopup,
    FilterChips,
    I18nText,
    MessagePopup,
    PageSpinner,
    RouterLink,
    Table,
    TableErrorPopup,
    TableFilterBar,
    TableHeadingAction,
    TranslatePipe,
  ],
  templateUrl: './database.html',
  styleUrl: './database.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Database {
  private readonly auth = inject(AuthBehaviour);
  private readonly database = inject(DatabaseService);
  private readonly dateLanguage = inject(DateLanguageBehaviour);
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
  private readonly translateService = inject(TranslateService);
  private readonly router = inject(Router);
  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly loadFailed = signal(false);
  /** The same page shows the trash too, on its own route (settings/database/trash). */
  protected readonly isTrash = inject(ActivatedRoute).snapshot.data['isTrash'] === true;
  protected readonly trashIcon = ['/assets/images/Trash_setting.svg'] as const;
  protected readonly restoreIcon = ['/assets/images/Restore_setting.svg'] as const;
  protected readonly backIcon = '/assets/images/TrashBack_setting.svg';
  protected readonly idSelectedBookings = signal<readonly TableRowId[]>([]);
  protected readonly idBookings = computed(() => this.idSelectedBookings().map(String));
  protected readonly isDeleteAsked = signal(false);
  protected readonly isTrashedShown = signal(false);
  protected readonly isEmptyingTrash = signal(false);
  protected readonly isEmptyTrashAsked = signal(false);
  protected readonly isTrashEmpty = signal(false);
  protected readonly isRestoring = signal(false);
  protected readonly isDeleteForeverAsked = signal(false);
  protected readonly isDeletingForever = signal(false);
  protected readonly closeIcon = ['/assets/images/CloseDatabaseFilters_setting.svg'] as const;
  protected readonly isActionErrorShown = signal(false);
  protected readonly actionErrorIcon = ['/assets/images/DatabaseWarning_setting.svg'] as const;
  private readonly actionError = signal<ApiProblem | null>(null);
  protected readonly actionErrorMessage = toSignal(
    toObservable(this.actionError).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.idProperty;
    return idProperty ? { idProperty } : null;
  });
  protected readonly dateRange = signal<DateRange | null>(null);
  protected readonly isFiltersOpen = signal(false);
  protected readonly bookingFilters = signal(new ExistingBookingFilters());
  protected readonly chipIcons = ['/assets/images/CloseDatabaseFilters_setting.svg'] as const;
  private readonly filterTexts = toSignal(
    this.translateService.stream(
      'management.settings.database.filters',
    ) as Observable<ExistingBookingFilterTexts>,
  );
  /** One chip for each filter with a choice; with more choices only "more options". */
  protected readonly chips = computed<readonly BookingFilterChip[]>(() => {
    const texts = this.filterTexts();
    const filters = this.bookingFilters();
    if (!texts) {
      return [];
    }
    const chip = (
      name: string,
      labels: readonly string[],
      emptied: Partial<ExistingBookingFilters>,
    ): BookingFilterChip[] =>
      labels.length
        ? [
            {
              label: `${name}: ${labels.length > 1 ? texts.chips.more_options : labels[0]}`,
              emptied,
            },
          ]
        : [];
    const labelsOf = <K extends string>(
      values: readonly K[],
      options: readonly { value: K; label: string }[],
    ) => values.map((value) => options.find((option) => option.value === value)?.label ?? value);
    return [
      ...chip(
        texts.chips.booking_typology,
        labelsOf(
          filters.bookingTypologies,
          filterOptionsOf(Object.values(BookingTypologyFilter), texts.booking_typology.options),
        ),
        { bookingTypologies: [] },
      ),
      ...chip(
        texts.chips.agreement_booking,
        labelsOf(
          filters.agreementBookings,
          filterOptionsOf(Object.values(AgreementBookingFilter), texts.agreement_booking.options),
        ),
        { agreementBookings: [] },
      ),
      ...chip(
        texts.chips.agreement_structure,
        filters.affiliatedHotels.map((hotel) => hotel.value),
        { affiliatedHotels: [] },
      ),
      ...chip(
        texts.chips.payment_status,
        labelsOf(
          filters.paymentStatuses,
          filterOptionsOf(Object.values(PaymentStatusFilter), texts.payment_status.options),
        ),
        { paymentStatuses: [] },
      ),
    ];
  });
  protected readonly filters = computed(() => [
    ...dateRangeFilters(this.dateRange()),
    ...pageFiltersOf(this.bookingFilters()),
  ]);
  /** In the select's order: [open, previous month, next month]. */
  protected readonly dateFilterIcons = [
    '/assets/images/DateFilterOpen_setting.svg',
    '/assets/images/DateFilterPrevious_setting.svg',
    '/assets/images/DateFilterNext_setting.svg',
  ] as const;
  private readonly dateFilterTexts = toSignal(
    this.translateService.stream('management.settings.database.date_filter') as Observable<
      DatabaseTexts['date_filter']
    >,
    {
      initialValue: {
        all: '',
        today: '',
        this_week: '',
        next_seven_days: '',
        next_month: '',
        next_six_months: '',
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
      { label: texts.next_seven_days, range: nextWeekPeriod(today) },
      { label: texts.next_month, range: nextMonthsPeriod(today, 1) },
      { label: texts.next_six_months, range: nextMonthsPeriod(today, 6) },
    ];
  });
  /** In the table's order: [search magnifier, search X]. */
  protected readonly tableIcons = [
    '/assets/images/search.svg',
    '/assets/images/clear.svg',
  ] as const;
  private readonly headers = toSignal(
    this.translateService.stream('management.settings.database.table') as Observable<
      DatabaseTexts['table']
    >,
    {
      initialValue: {
        customer: '',
        reservation_kind: '',
        date_from: '',
        date_to: '',
        agreement: '',
        reservation_status: '',
      },
    },
  );
  protected readonly columns = computed<readonly TableColumn<ExistingBookingRow>[]>(() => [
    { field: 'customerName', header: this.headers().customer, sortable: true },
    { field: 'reservationKind', header: this.headers().reservation_kind, sortable: true },
    { field: 'dateFrom', header: this.headers().date_from, sortable: true },
    { field: 'dateTo', header: this.headers().date_to, sortable: true },
    { field: 'agreementName', header: this.headers().agreement, sortable: true },
    { field: 'reservationStatus', header: this.headers().reservation_status, sortable: true },
  ]);

  /** The texts are asked with each page: after a language change they are those of the new one. */
  protected readonly load: TableLoad<{ idProperty: string }, ExistingBookingRow> = (request) =>
    (this.isTrash ? this.database.trashItemList : this.database.existingBookingList)(request).pipe(
      tap((page) => {
        if (this.isTrash && !request.search && !request.filters.length) {
          this.isTrashEmpty.set(page.total === 0);
        }
      }),
      switchMap((page) =>
        (
          this.translateService.get('management.settings.database') as Observable<DatabaseTexts>
        ).pipe(
          map((texts) => {
            const day = new Intl.DateTimeFormat(this.dateLanguage.locale(), {
              ...DATE_FORMATS.display.dateInput,
              timeZone: 'UTC',
            });
            return {
              total: page.total,
              rows: page.rows.map((item) => this.toRow(item, texts, day)),
            };
          }),
        ),
      ),
    );

  private toRow(
    item: ExistingBookingItem,
    texts: DatabaseTexts,
    day: Intl.DateTimeFormat,
  ): ExistingBookingRow {
    return {
      ...item,
      reservationKind: item.reservationKind
        ? texts.reservation_kind[RESERVATION_KIND_KEYS[item.reservationKind]]
        : null,
      dateFrom: item.dateFrom ? day.format(new Date(item.dateFrom)) : null,
      dateTo: item.dateTo ? day.format(new Date(item.dateTo)) : null,
      reservationStatus: item.reservationStatus
        ? texts.reservation_status[RESERVATION_STATUS_KEYS[item.reservationStatus]]
        : null,
    };
  }

  protected removeChip(chip: BookingFilterChip): void {
    this.bookingFilters.update((filters) => ({ ...filters, ...chip.emptied }));
  }

  /** The bookings are in the trash now: they leave the list, and the choice with them. */
  protected afterTrash(): void {
    this.idSelectedBookings.set([]);
    this.isTrashedShown.set(true);
  }

  /** The page waits under a spinner; then the table is read again, whatever the answer. */
  protected emptyTrash(table: Table<ExistingBookingRow, { idProperty: string }>): void {
    const idProperty = this.auth.user()?.idProperty;
    if (this.isEmptyingTrash() || !idProperty) {
      return;
    }
    this.isEmptyingTrash.set(true);
    this.database.emptyTrash(idProperty).subscribe({
      next: () => {
        this.isEmptyingTrash.set(false);
        this.idSelectedBookings.set([]);
        this.isTrashEmpty.set(true);
        table.reload();
      },
      error: (error: unknown) => {
        this.isEmptyingTrash.set(false);
        this.actionError.set(toApiProblem(error));
        this.isActionErrorShown.set(true);
        table.reload();
      },
    });
  }

  protected restore(table: Table<ExistingBookingRow, { idProperty: string }>): void {
    const idProperty = this.auth.user()?.idProperty;
    if (this.isRestoring() || !idProperty) {
      return;
    }
    this.isRestoring.set(true);
    this.database.restoreBookingItem({ idProperty, idBookings: this.idBookings() }).subscribe({
      next: () => {
        this.isRestoring.set(false);
        this.idSelectedBookings.set([]);
        this.readIsTrashEmpty(idProperty);
        table.reload();
      },
      error: (error: unknown) => {
        this.isRestoring.set(false);
        this.actionError.set(toApiProblem(error));
        this.isActionErrorShown.set(true);
        table.reload();
      },
    });
  }

  protected deleteForever(table: Table<ExistingBookingRow, { idProperty: string }>): void {
    const idProperty = this.auth.user()?.idProperty;
    if (this.isDeletingForever() || !idProperty) {
      return;
    }
    this.isDeletingForever.set(true);
    this.database.deleteSelectedItem({ idProperty, idBookings: this.idBookings() }).subscribe({
      next: () => {
        this.isDeletingForever.set(false);
        this.idSelectedBookings.set([]);
        this.readIsTrashEmpty(idProperty);
        table.reload();
      },
      error: (error: unknown) => {
        this.isDeletingForever.set(false);
        this.actionError.set(toApiProblem(error));
        this.isActionErrorShown.set(true);
        table.reload();
      },
    });
  }

  // The table may be showing a search or filters: its total is not the whole trash.
  private readIsTrashEmpty(idProperty: string): void {
    this.database
      .trashItemList({
        idProperty,
        page: 1,
        pageSize: 1,
        search: '',
        filters: [],
        sortField: '',
        sortDirection: 'Ascending',
      })
      .subscribe({ next: (page) => this.isTrashEmpty.set(page.total === 0) });
  }

  protected openTrash(): void {
    void this.router.navigateByUrl('/settings/database/trash');
  }

  protected clearFilters(): void {
    this.bookingFilters.set(new ExistingBookingFilters());
    this.dateRange.set(null);
  }
}

/** One filter for each chosen value: values of the same field are alternatives. */
function pageFiltersOf(filters: ExistingBookingFilters): PageFilter[] {
  return [
    ...filters.bookingTypologies.map((value) => ({ field: 'bookingTypology', value })),
    ...filters.agreementBookings.map((value) => ({ field: 'agreementBooking', value })),
    ...filters.affiliatedHotels.map(({ id }) => ({ field: 'idAffiliatedHotel', value: id })),
    ...filters.paymentStatuses.map((value) => ({ field: 'paymentStatus', value })),
  ];
}
