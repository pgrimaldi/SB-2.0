import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ValidationTextBehaviour } from '../../../../../../behaviours/validation/validation-text.behaviour';
import { FilledNumberField } from '../../../../../shared/ui/inputs/filled-number-field/filled-number-field';

/** Price in euro and VAT of a service, side by side. */
@Component({
  selector: 'app-services-price-vat',
  imports: [FilledNumberField, FormField, TranslatePipe],
  templateUrl: './services-price-vat.html',
  styleUrl: './services-price-vat.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesPriceVat {
  /** The fields of the form of the panel, which validates and saves them. */
  readonly price = input.required<FieldTree<number | null>>();
  readonly vatRate = input.required<FieldTree<number | null>>();

  protected readonly validationText = inject(ValidationTextBehaviour);
}
