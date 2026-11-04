import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ServiceAvailabilityOption } from '../../../../../../entities/settings/services/service-availability-option';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../../shared/ui/buttons/icon-button/icon-button';
import { SectionTitle } from '../../../../../shared/ui/titles/section-title/section-title';
import { ServicesAvailabilityOption } from '../services-availability-option/services-availability-option';

/** "Opzioni disponibilità" of a period: one box per option, each with its X. */
@Component({
  selector: 'app-services-availability-options',
  imports: [Button, IconButton, SectionTitle, ServicesAvailabilityOption, TranslatePipe],
  templateUrl: './services-availability-options.html',
  styleUrl: './services-availability-options.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesAvailabilityOptions {
  /** The options of the form of the panel, which validates and saves them. */
  readonly form = input.required<FieldTree<ServiceAvailabilityOption[]>>();
  /** Only to look at: adding and removing an option are off. */
  readonly isReadonly = input(false);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_AddBlue.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;

  protected addOption(): void {
    this.form()().value.update((options) => [...options, new ServiceAvailabilityOption()]);
  }

  protected removeOption(index: number): void {
    this.form()().value.update((options) =>
      options.filter((_, optionIndex) => optionIndex !== index),
    );
  }
}
