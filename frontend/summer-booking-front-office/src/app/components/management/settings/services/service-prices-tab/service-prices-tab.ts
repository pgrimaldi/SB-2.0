import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { BarServiceAddItemRequest } from '../../../../../entities/settings/services/bar-service-add-item-request';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { ServicesLimits } from '../shared/services-limits/services-limits';
import { ServicesPeriodPrices } from '../shared/services-period-prices/services-period-prices';
import { ServicesPopupAlternatives } from '../shared/services-popup-alternatives/services-popup-alternatives';
import { ServicesPriceVat } from '../shared/services-price-vat/services-price-vat';

@Component({
  selector: 'app-service-prices-tab',
  imports: [
    FormField,
    ServicesLimits,
    ServicesPeriodPrices,
    ServicesPopupAlternatives,
    ServicesPriceVat,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './service-prices-tab.html',
  styleUrl: './service-prices-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicePricesTab {
  readonly form = input.required<FieldTree<BarServiceAddItemRequest>>();
  /** The periods of the current season, each with its own price when the toggle is on. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);
  /** Only to look at: the buttons that change the data (add, remove) are off. */
  readonly isReadonly = input(false);
}
