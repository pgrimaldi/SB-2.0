import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { ServiceBookingOption } from '../../../../../entities/settings/services/service-booking-option';
import { ServicesBookingOption } from '../shared/services-booking-option/services-booking-option';

@Component({
  selector: 'app-general-service-bookings-tab',
  imports: [ServicesBookingOption],
  templateUrl: './general-service-bookings-tab.html',
  styleUrl: './general-service-bookings-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GeneralServiceBookingsTab {
  readonly form = input.required<FieldTree<ServiceBookingOption[]>>();
  /** Only to look at: the buttons that change the data (add, remove) are off. */
  readonly isReadonly = input(false);
}
