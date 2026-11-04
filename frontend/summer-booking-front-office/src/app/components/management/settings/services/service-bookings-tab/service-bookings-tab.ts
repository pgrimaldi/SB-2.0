import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { ServiceBookingOption } from '../../../../../entities/settings/services/service-booking-option';
import { ServicesBookingOption } from '../shared/services-booking-option/services-booking-option';

@Component({
  selector: 'app-service-bookings-tab',
  imports: [ServicesBookingOption],
  templateUrl: './service-bookings-tab.html',
  styleUrl: './service-bookings-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceBookingsTab {
  readonly form = input.required<FieldTree<ServiceBookingOption[]>>();
  /** Only to look at: the buttons that change the data (icons, image, add, remove) are off. */
  readonly isReadonly = input(false);
}
