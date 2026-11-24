import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DateAdapter } from '@angular/material/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map, switchMap, tap } from 'rxjs';
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
import { PageTitle } from '../../../components/shared/ui/titles/page-title/page-title';
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
import { MAX_BOOKING_IDS } from '../../../entities/database/move-to-trash-request';
import { FilterField } from '../../../entities/enums/filter-field';
import { ReservationKind } from '../../../entities/enums/reservation-kind';
import { ReservationStatus } from '../../../entities/enums/reservation-status';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { PageFilter } from '../../../entities/pagination/page';
import { BulkResult } from '../../../entities/shared/bulk-result';
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
  reservation_kind: Record<ReservationKind, string>;
  reservation_status: Record<ReservationStatus, string>;
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
    PageTitle,
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
  protected readonly trashIcon = ['/assets/images/GeneralIcon_DeleteWhite.svg'] as const;
  protected readonly restoreIcon = ['/assets/images/Database_Restore.svg'] as const;
  protected readonly backIcon = '/assets/images/GeneralIcon_Previous.svg';
  protected readonly idSelectedBookings = signal<readonly TableRowId[]>([]);
  protected readonly idBookings = computed(() => this.idSelectedBookings().map(String));
  protected readonly maxBookings = MAX_BOOKING_IDS;
  protected readonly isOverLimit = computed(() => this.idBookings().length > MAX_BOOKING_IDS);
  protected readonly canAct = computed(() => !!this.idBookings().length && !this.isOverLimit());
  protected readonly isDeleteAsked = signal(false);
  protected readonly isTrashedShown = signal(false);
  protected readonly isEmptyTrashAsked = signal(false);
  protected readonly isTrashEmpty = signal(true);
  protected readonly isDeleteForeverAsked = signal(false);
  protected readonly isRunning = signal(false);
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly isActionErrorShown = signal(false);
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  private readonly actionError = signal<ApiProblem | null>(null);
  protected readonly actionErrorMessage = this.errorText.message(this.actionError);
  protected readonly params = computed<object | null>(() =>
    this.auth.user()?.propertyId ? {} : null,
  );
  protected readonly dateRange = signal<DateRange | null>(null);
  protected readonly isFiltersOpen = signal(false);
  protected readonly bookingFilters = signal(new ExistingBookingFilters());
  protected readonly chipIcons = ['/assets/images/GeneralIcon_Close.svg'] as const;
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
    '/assets/images/GeneralIcon_DateFilterOpen.svg',
    '/assets/images/GeneralIcon_Previous.svg',
    '/assets/images/GeneralIcon_Next.svg',
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
    '/assets/images/GeneralIcon_Search.svg',
    '/assets/images/GeneralIcon_Clear.svg',
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
  protected readonly load: TableLoad<object, ExistingBookingRow> = (request) =>
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

  protected removeChip(chip: BookingFilterChip): void {
    this.bookingFilters.update((filters) => ({ ...filters, ...chip.emptied }));
  }

  protected afterTrash(result: BulkResult): void {
    this.idSelectedBookings.set([]);
    if (!this.showFailure(result)) {
      this.isTrashedShown.set(true);
    }
  }

  protected openTrash(): void {
    void this.router.navigateByUrl('/settings/database/trash');
  }

  protected clearFilters(): void {
    this.bookingFilters.set(new ExistingBookingFilters());
    this.dateRange.set(null);
  }

  protected emptyTrash(table: Table<ExistingBookingRow, object>): void {
    this.run(this.database.emptyTrash(), table, () => this.isTrashEmpty.set(true));
  }

  protected restore(table: Table<ExistingBookingRow, object>): void {
    this.run(this.database.restoreBookingItem(this.idBookings()), table, (result) =>
      this.showFailure(result),
    );
  }

  protected deleteForever(table: Table<ExistingBookingRow, object>): void {
    this.run(this.database.deleteSelectedItem(this.idBookings()), table, (result) =>
      this.showFailure(result),
    );
  }

  /** The page waits under a spinner; then the table is read again, whatever the answer. */
  private run<T>(
    call: Observable<T>,
    table: Table<ExistingBookingRow, object>,
    done: (answer: T) => void,
  ): void {
    if (this.isRunning() || !this.auth.user()?.propertyId) {
      return;
    }
    this.isRunning.set(true);
    call.subscribe({
      next: (answer) => {
        this.isRunning.set(false);
        this.idSelectedBookings.set([]);
        done(answer);
        table.reload();
      },
      error: (error: unknown) => {
        this.isRunning.set(false);
        this.showError(toApiProblem(error));
        table.reload();
      },
    });
  }

  /** The first reason why some of the bookings stayed as they were. */
  private showFailure({ failed }: BulkResult): boolean {
    if (failed.length) {
      this.showError({ status: 0, code: failed[0].code });
    }
    return failed.length > 0;
  }

  private showError(problem: ApiProblem): void {
    this.actionError.set(problem);
    this.isActionErrorShown.set(true);
  }

  private toRow(
    item: ExistingBookingItem,
    texts: DatabaseTexts,
    day: Intl.DateTimeFormat,
  ): ExistingBookingRow {
    const { reservationKind: kind, reservationStatus: status } = item;
    return {
      ...item,
      reservationKind: kind && (texts.reservation_kind[kind] ?? kind),
      dateFrom: item.dateFrom ? day.format(new Date(item.dateFrom)) : null,
      dateTo: item.dateTo ? day.format(new Date(item.dateTo)) : null,
      reservationStatus: status && (texts.reservation_status[status] ?? status),
    };
  }
}

/** One filter per field with its chosen values (alternatives); a field with no choice is left out. */
function pageFiltersOf(filters: ExistingBookingFilters): PageFilter[] {
  const all: PageFilter[] = [
    { field: FilterField.BookingTypology, value: [...filters.bookingTypologies] },
    { field: FilterField.AgreementBooking, value: [...filters.agreementBookings] },
    { field: FilterField.IdAffiliatedHotel, value: filters.affiliatedHotels.map(({ id }) => id) },
    { field: FilterField.PaymentStatus, value: [...filters.paymentStatuses] },
  ];
  return all.filter(({ value }) => value.length);
}
