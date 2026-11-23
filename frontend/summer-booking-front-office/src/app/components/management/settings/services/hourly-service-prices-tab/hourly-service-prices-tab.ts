import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { HourlyServiceDraft } from '../../../../../entities/settings/services/hourly-service-draft';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { ServicesAvailabilityOptions } from '../shared/services-availability-options/services-availability-options';
import { ServicesLimits } from '../shared/services-limits/services-limits';
import { ServicesPeriodContent } from '../shared/services-period-prices/services-period-content';
import { ServicesPeriodPrices } from '../shared/services-period-prices/services-period-prices';
import { ServicesPriceVat } from '../shared/services-price-vat/services-price-vat';

@Component({
  selector: 'app-hourly-service-prices-tab',
  imports: [
    FormField,
    ServicesAvailabilityOptions,
    ServicesLimits,
    ServicesPeriodContent,
    ServicesPeriodPrices,
    ServicesPriceVat,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './hourly-service-prices-tab.html',
  styleUrl: './hourly-service-prices-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HourlyServicePricesTab {
  readonly form = input.required<FieldTree<HourlyServiceDraft>>();
  /** The periods of the current season, each with its own availability options when the toggle is on; off, they are the same for all. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);
  /** Only to look at: adding and removing an availability option are off. */
  readonly isReadonly = input(false);

  protected readonly validationText = inject(ValidationTextBehaviour);

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
