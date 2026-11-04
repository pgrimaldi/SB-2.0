import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ValidationTextBehaviour } from '../../../../../../behaviours/validation/validation-text.behaviour';
import { ServiceTimeSlot } from '../../../../../../entities/enums/service-time-slot';
import { ServiceAvailabilityOption } from '../../../../../../entities/settings/services/service-availability-option';
import { DAYS_OF_WEEK } from '../../../../../../entities/shared/days-of-week';
import { Checkbox } from '../../../../../shared/ui/checkboxes/checkbox/checkbox';
import { FormRow } from '../../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../../shared/ui/forms/form-row/form-row-label';
import { FilledNumberField } from '../../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTimeField } from '../../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { Toggle } from '../../../../../shared/ui/toggles/toggle/toggle';

type DayKey = (typeof DAYS_OF_WEEK)[number]['key'];

interface TimeSlotTexts {
  ten_minutes: string;
  fifteen_minutes: string;
  thirty_minutes: string;
  forty_five_minutes: string;
  one_hour: string;
  one_hour_thirty_minutes: string;
  two_hours: string;
  three_hours: string;
}

interface AvailabilityTexts {
  days: Record<DayKey, string>;
  time_slot_names: TimeSlotTexts;
}

const TIME_SLOT_KEYS: Readonly<Record<ServiceTimeSlot, keyof TimeSlotTexts>> = {
  [ServiceTimeSlot.TenMinutes]: 'ten_minutes',
  [ServiceTimeSlot.FifteenMinutes]: 'fifteen_minutes',
  [ServiceTimeSlot.ThirtyMinutes]: 'thirty_minutes',
  [ServiceTimeSlot.FortyFiveMinutes]: 'forty_five_minutes',
  [ServiceTimeSlot.OneHour]: 'one_hour',
  [ServiceTimeSlot.OneHourThirtyMinutes]: 'one_hour_thirty_minutes',
  [ServiceTimeSlot.TwoHours]: 'two_hours',
  [ServiceTimeSlot.ThreeHours]: 'three_hours',
};

/** Hours, days and time slots with their prices of an hourly service: one "Opzioni disponibilità". */
@Component({
  selector: 'app-services-availability-option',
  imports: [
    Checkbox,
    FilledNumberField,
    FilledTimeField,
    FormField,
    FormRow,
    FormRowLabel,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './services-availability-option.html',
  styleUrl: './services-availability-option.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesAvailabilityOption {
  /** One option of the form of the panel, which validates and saves it. */
  readonly form = input.required<FieldTree<ServiceAvailabilityOption>>();

  private readonly translateService = inject(TranslateService);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly clockIcon = ['/assets/images/GeneralIcon_Clock.svg'] as const;
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.services.form.availability',
    ) as Observable<AvailabilityTexts>,
    { initialValue: null },
  );
  protected readonly days = computed(() =>
    DAYS_OF_WEEK.map(({ number, key }) => ({ number, label: this.texts()?.days[key] ?? '' })),
  );
  /** The days are not form fields: they follow the state of their list. */
  protected readonly areDaysLocked = computed(
    () => this.form().daysOfWeek().disabled() || this.form().daysOfWeek().readonly(),
  );

  protected timeSlotName(timeSlot: ServiceTimeSlot | null): string {
    return timeSlot ? (this.texts()?.time_slot_names[TIME_SLOT_KEYS[timeSlot]] ?? '') : '';
  }

  protected setDay(day: number, isChosen: boolean): void {
    this.form()
      .daysOfWeek()
      .value.update((days) =>
        isChosen
          ? [...days, day].sort((first, second) => first - second)
          : days.filter((chosen) => chosen !== day),
      );
  }
}
