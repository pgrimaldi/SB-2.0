import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { TableServiceAddItemRequest } from '../../../../../entities/settings/services/table-service-add-item-request';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { TableServiceAvailabilities } from '../table-service-availabilities/table-service-availabilities';
import { TableServiceBookingOptions } from '../table-service-booking-options/table-service-booking-options';
import { TableServiceCapacity } from '../table-service-capacity/table-service-capacity';

@Component({
  selector: 'app-table-service-availability-bookings-tab',
  imports: [
    FormField,
    TableServiceAvailabilities,
    TableServiceBookingOptions,
    TableServiceCapacity,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './table-service-availability-bookings-tab.html',
  styleUrl: './table-service-availability-bookings-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableServiceAvailabilityBookingsTab {
  readonly form = input.required<FieldTree<TableServiceAddItemRequest>>();
  /** Only to look at: adding and removing availabilities and tables are off. */
  readonly isReadonly = input(false);
}
