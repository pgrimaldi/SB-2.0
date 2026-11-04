import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { HourlyServiceBookingOption } from '../../../../../entities/settings/services/hourly-service-booking-option';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { ServicesAvailabilityOption } from '../shared/services-availability-option/services-availability-option';

/** "Opzioni di prenotazione" of an hourly service: a period and its availability in each box. */
@Component({
  selector: 'app-hourly-service-bookings-tab',
  imports: [
    Button,
    FilledDateField,
    FormField,
    FormRow,
    FormRowLabel,
    IconButton,
    SectionTitle,
    ServicesAvailabilityOption,
    TranslatePipe,
  ],
  templateUrl: './hourly-service-bookings-tab.html',
  styleUrl: './hourly-service-bookings-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HourlyServiceBookingsTab {
  /** The booking options of the form of the panel, which validates and saves them. */
  readonly form = input.required<FieldTree<HourlyServiceBookingOption[]>>();
  /** Only to look at: adding and removing an option are off. */
  readonly isReadonly = input(false);

  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_Add.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly calendarIcon = ['/assets/images/GeneralIcon_Calendar.svg'] as const;

  protected addOption(): void {
    this.form()().value.update((options) => [...options, new HourlyServiceBookingOption()]);
  }

  protected removeOption(index: number): void {
    this.form()().value.update((options) =>
      options.filter((_, optionIndex) => optionIndex !== index),
    );
  }
}
