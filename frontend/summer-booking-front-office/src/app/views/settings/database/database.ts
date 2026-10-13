import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DateAdapter } from '@angular/material/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
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
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
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
import { PageFilter } from '../../../entities/pagination/page';
import { DateRange } from '../../../entities/shared/date-range';
import { DatabaseService } from '../../../services/api/database/database.service';

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
    Button,
    ConfirmDeleteBookingPopup,
    DateRangeSelect,
    ExistingBookingFiltersPopup,
    FilterChips,
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

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly loadFailed = signal(false);
  protected readonly trashIcon = ['/assets/images/Trash_setting.svg'] as const;
  protected readonly idSelectedBookings = signal<readonly TableRowId[]>([]);
  protected readonly idBookingsToDelete = computed(() => this.idSelectedBookings().map(String));
  protected readonly isDeleteAsked = signal(false);
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
    this.database.existingBookingList(request).pipe(
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
