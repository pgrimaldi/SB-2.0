import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { AutomaticPriceAddItemRequest } from '../../../../../entities/settings/services/automatic-price-add-item-request';
import { AutomaticPriceServices } from '../../../../../entities/settings/services/automatic-price-services';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { AutomaticPriceServiceValidity } from '../automatic-price-service-validity/automatic-price-service-validity';
import { AutomaticPriceValidityPeriods } from '../automatic-price-validity-periods/automatic-price-validity-periods';
import { ServicesLimits } from '../shared/services-limits/services-limits';
import { ServicesPriceVat } from '../shared/services-price-vat/services-price-vat';

@Component({
  selector: 'app-automatic-price-prices-validity-tab',
  imports: [
    AutomaticPriceServiceValidity,
    AutomaticPriceValidityPeriods,
    FormField,
    ServicesLimits,
    ServicesPriceVat,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './automatic-price-prices-validity-tab.html',
  styleUrl: './automatic-price-prices-validity-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomaticPricePricesValidityTab {
  readonly form = input.required<FieldTree<AutomaticPriceAddItemRequest>>();
  /** The services of the property, by kind; null while they are not read. */
  readonly services = input<AutomaticPriceServices | null>(null);
}
