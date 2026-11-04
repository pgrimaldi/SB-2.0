import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { StandardServiceAddItemRequest } from '../../../../../entities/settings/services/standard-service-add-item-request';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { ServicesLimits } from '../shared/services-limits/services-limits';
import { ServicesPeriodPrices } from '../shared/services-period-prices/services-period-prices';
import { ServicesPriceVat } from '../shared/services-price-vat/services-price-vat';

@Component({
  selector: 'app-general-service-prices-tab',
  imports: [
    FormField,
    ServicesLimits,
    ServicesPeriodPrices,
    ServicesPriceVat,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './general-service-prices-tab.html',
  styleUrl: './general-service-prices-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GeneralServicePricesTab {
  readonly form = input.required<FieldTree<StandardServiceAddItemRequest>>();
  /** The periods of the current season, each with its own price when the toggle is on. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);
}
