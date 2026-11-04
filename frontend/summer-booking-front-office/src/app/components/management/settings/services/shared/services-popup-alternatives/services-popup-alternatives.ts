import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ValidationTextBehaviour } from '../../../../../../behaviours/validation/validation-text.behaviour';
import { ServiceVariant } from '../../../../../../entities/settings/services/service-variant';
import { Button, ButtonSize } from '../../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../../shared/ui/buttons/icon-button/icon-button';
import { FilledNumberField } from '../../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { SectionTitle } from '../../../../../shared/ui/titles/section-title/section-title';

@Component({
  selector: 'app-services-popup-alternatives',
  imports: [
    Button,
    FilledNumberField,
    FilledTextField,
    FormField,
    IconButton,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './services-popup-alternatives.html',
  styleUrl: './services-popup-alternatives.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesPopupAlternatives {
  /** The variants of the form of the panel, which validates and saves them. */
  readonly form = input.required<FieldTree<ServiceVariant[]>>();
  /** Only to look at: adding and removing a variant are off. */
  readonly isReadonly = input(false);

  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_Add.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;

  protected addVariant(): void {
    this.form()().value.update((variants) => [...variants, new ServiceVariant()]);
  }

  protected removeVariant(index: number): void {
    this.form()().value.update((variants) =>
      variants.filter((_, variantIndex) => variantIndex !== index),
    );
  }
}
