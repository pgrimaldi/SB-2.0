import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, map, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import {
  DATE_FORMATS,
  DateLanguageBehaviour,
} from '../../../behaviours/i18n/date-language.behaviour';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import {
  Table,
  TableColumn,
  TableLoad,
  TableRowId,
} from '../../../components/shared/ui/tables/table/table';
import { ExistingBookingItem } from '../../../entities/database/existing-booking-item';
import { ReservationKind } from '../../../entities/enums/reservation-kind';
import { ReservationStatus } from '../../../entities/enums/reservation-status';
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
  imports: [Table, TableErrorPopup, TranslatePipe],
  templateUrl: './database.html',
  styleUrl: './database.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Database {
  private readonly auth = inject(AuthBehaviour);
  private readonly database = inject(DatabaseService);
  private readonly dateLanguage = inject(DateLanguageBehaviour);
  private readonly translateService = inject(TranslateService);

  protected readonly loadFailed = signal(false);
  protected readonly idSelectedBookings = signal<readonly TableRowId[]>([]);
  protected readonly params = computed<{ idProperty: string } | null>(() => {
    const idProperty = this.auth.user()?.idProperty;
    return idProperty ? { idProperty } : null;
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
          map((texts) => ({
            total: page.total,
            rows: page.rows.map((item) => this.toRow(item, texts)),
          })),
        ),
      ),
    );

  private toRow(item: ExistingBookingItem, texts: DatabaseTexts): ExistingBookingRow {
    const day = new Intl.DateTimeFormat(this.dateLanguage.locale(), {
      ...DATE_FORMATS.display.dateInput,
      timeZone: 'UTC',
    });
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
}
