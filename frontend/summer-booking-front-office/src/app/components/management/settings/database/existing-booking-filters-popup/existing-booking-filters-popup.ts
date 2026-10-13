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
import {
  ExistingBookingFilterTexts,
  filterOptionsOf,
} from '../../../../../behaviours/database/existing-booking-filter-options';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import {
  AgreementBookingFilter,
  BookingTypologyFilter,
  ExistingBookingFilters,
  PaymentStatusFilter,
} from '../../../../../entities/database/existing-booking-filters';
import { DatabaseService } from '../../../../../services/api/database/database.service';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';

@Component({
  selector: 'app-existing-booking-filters-popup',
  imports: [Button, FilledMultiSelect, TranslatePipe],
  templateUrl: './existing-booking-filters-popup.html',
  styleUrl: './existing-booking-filters-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class ExistingBookingFiltersPopup extends BasePopup implements OnDestroy {
  readonly filters = input(new ExistingBookingFilters());
  readonly applied = output<ExistingBookingFilters>();

  private readonly translateService = inject(TranslateService);
  private readonly database = inject(DatabaseService);
  private readonly auth = inject(AuthBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly closeIcon = '/assets/images/CloseDatabaseFilters_setting.svg';
  protected readonly bookingTypologies = signal<readonly BookingTypologyFilter[]>([]);
  protected readonly agreementBookings = signal<readonly AgreementBookingFilter[]>([]);
  protected readonly idAffiliatedHotels = signal<readonly string[]>([]);
  protected readonly paymentStatuses = signal<readonly PaymentStatusFilter[]>([]);
  private readonly affiliatedHotels = signal<readonly ComboboxItem[]>([]);
  private affiliatedHotelsSubscription?: Subscription;
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.database.filters',
    ) as Observable<ExistingBookingFilterTexts>,
  );
  protected readonly bookingTypologyOptions = computed(() =>
    filterOptionsOf(Object.values(BookingTypologyFilter), this.texts()?.booking_typology.options),
  );
  protected readonly agreementBookingOptions = computed(() =>
    filterOptionsOf(Object.values(AgreementBookingFilter), this.texts()?.agreement_booking.options),
  );
  protected readonly affiliatedHotelOptions = computed<readonly SelectOption[]>(() =>
    this.affiliatedHotels().map((hotel) => ({ value: hotel.id, label: hotel.value })),
  );
  protected readonly paymentStatusOptions = computed(() =>
    filterOptionsOf(Object.values(PaymentStatusFilter), this.texts()?.payment_status.options),
  );

  constructor() {
    super();
    effect(() => {
      if (this.open()) {
        untracked(() => {
          this.reset();
          this.loadAffiliatedHotels();
        });
      }
    });
  }

  protected apply(): void {
    this.applied.emit({
      bookingTypologies: [...this.bookingTypologies()],
      agreementBookings: [...this.agreementBookings()],
      affiliatedHotels: this.chosenHotels(),
      paymentStatuses: [...this.paymentStatuses()],
    });
    this.close();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(32rem, 90vw)',
      maxWidth: 'none',
      position: { top: '6.875rem' },
      panelClass: 'existing__booking__filters__popup__panel',
      autoFocus: false,
    };
  }

  private reset(): void {
    const filters = this.filters();
    this.bookingTypologies.set(filters.bookingTypologies);
    this.agreementBookings.set(filters.agreementBookings);
    this.idAffiliatedHotels.set(filters.affiliatedHotels.map((hotel) => hotel.id));
    this.paymentStatuses.set(filters.paymentStatuses);
  }

  /** With their names, for the chips: from the list of the server, or as they were applied. */
  private chosenHotels(): ComboboxItem[] {
    const known = new Map(
      [...this.filters().affiliatedHotels, ...this.affiliatedHotels()].map((hotel) => [
        hotel.id,
        hotel,
      ]),
    );
    return this.idAffiliatedHotels().flatMap((id) => known.get(id) ?? []);
  }

  private loadAffiliatedHotels(): void {
    // A slower answer of an earlier opening must not replace the list of this one.
    this.affiliatedHotelsSubscription?.unsubscribe();
    const idProperty = this.auth.user()?.idProperty;
    if (!idProperty) {
      return;
    }
    this.affiliatedHotelsSubscription = this.database
      .affiliatedHotelCombobox(idProperty)
      .subscribe({
        next: (hotels) => this.affiliatedHotels.set(hotels),
        error: () => this.affiliatedHotels.set([]),
      });
  }

  override ngOnDestroy(): void {
    this.affiliatedHotelsSubscription?.unsubscribe();
    super.ngOnDestroy();
  }
}
