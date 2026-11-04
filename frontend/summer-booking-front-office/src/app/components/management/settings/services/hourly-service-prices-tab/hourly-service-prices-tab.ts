import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { HourlyServiceAddItemRequest } from '../../../../../entities/settings/services/hourly-service-add-item-request';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { ServicesAvailabilityOptions } from '../shared/services-availability-options/services-availability-options';
import { ServicesLimits } from '../shared/services-limits/services-limits';
import { ServicesPeriodContent } from '../shared/services-period-prices/services-period-content';
import { ServicesPeriodPrices } from '../shared/services-period-prices/services-period-prices';

@Component({
  selector: 'app-hourly-service-prices-tab',
  imports: [
    FormField,
    ServicesAvailabilityOptions,
    ServicesLimits,
    ServicesPeriodContent,
    ServicesPeriodPrices,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './hourly-service-prices-tab.html',
  styleUrl: './hourly-service-prices-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HourlyServicePricesTab {
  readonly form = input.required<FieldTree<HourlyServiceAddItemRequest>>();
  /** The periods of the current season, each with its own availability options when the toggle is on. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);
  /** Only to look at: adding and removing an availability option are off. */
  readonly isReadonly = input(false);

  protected readonly availabilityByPeriod = computed(
    () =>
      new Map(
        [...this.form().periodAvailabilities].map((periodAvailability) => [
          periodAvailability.idPeriod().value(),
          periodAvailability,
        ]),
      ),
  );
}
