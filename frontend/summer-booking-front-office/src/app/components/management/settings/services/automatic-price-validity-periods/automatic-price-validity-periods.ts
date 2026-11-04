import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { AutomaticPriceTimeSlot } from '../../../../../entities/settings/services/automatic-price-time-slot';
import { AutomaticPriceValidityPeriod } from '../../../../../entities/settings/services/automatic-price-validity-period';
import { DAYS_OF_WEEK } from '../../../../../entities/shared/days-of-week';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { FilledTimeField } from '../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

type DayKey = (typeof DAYS_OF_WEEK)[number]['key'];

/** "Periodi validità" of an automatic price: days, weekdays and time slots of each period. */
@Component({
  selector: 'app-automatic-price-validity-periods',
  imports: [
    Button,
    Checkbox,
    FilledDateField,
    FilledTimeField,
    FormField,
    FormRow,
    FormRowLabel,
    IconButton,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './automatic-price-validity-periods.html',
  styleUrl: './automatic-price-validity-periods.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomaticPriceValidityPeriods {
  /** The periods of the form of the panel, which validates and saves them. */
  readonly form = input.required<FieldTree<AutomaticPriceValidityPeriod[]>>();
  /** Only to look at: adding and removing periods and time slots are off. */
  readonly isReadonly = input(false);

  private readonly translateService = inject(TranslateService);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_Add.svg'] as const;
  protected readonly addTimeSlotIcon = ['/assets/images/GeneralIcon_AddBlue.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly deleteIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;
  protected readonly calendarIcon = ['/assets/images/GeneralIcon_Calendar.svg'] as const;
  protected readonly clockIcon = ['/assets/images/GeneralIcon_Clock.svg'] as const;
  private readonly dayTexts = toSignal(
    this.translateService.stream(
      'management.settings.services.form.availability.days',
    ) as Observable<Record<DayKey, string>>,
    { initialValue: null },
  );
  protected readonly days = computed(() =>
    DAYS_OF_WEEK.map(({ number, key }) => ({ number, label: this.dayTexts()?.[key] ?? '' })),
  );

  protected addPeriod(): void {
    this.form()().value.update((periods) => [...periods, new AutomaticPriceValidityPeriod()]);
  }

  protected removePeriod(index: number): void {
    this.form()().value.update((periods) =>
      periods.filter((_, periodIndex) => periodIndex !== index),
    );
  }

  protected addTimeSlot(period: FieldTree<AutomaticPriceValidityPeriod>): void {
    period.timeSlots().value.update((timeSlots) => [...timeSlots, new AutomaticPriceTimeSlot()]);
  }

  protected removeTimeSlot(period: FieldTree<AutomaticPriceValidityPeriod>, index: number): void {
    period
      .timeSlots()
      .value.update((timeSlots) => timeSlots.filter((_, slotIndex) => slotIndex !== index));
  }

  /** The days are not form fields: they follow the state of their list. */
  protected areDaysLocked(period: FieldTree<AutomaticPriceValidityPeriod>): boolean {
    return period.daysOfWeek().disabled() || period.daysOfWeek().readonly();
  }

  protected setDay(
    period: FieldTree<AutomaticPriceValidityPeriod>,
    day: number,
    isChosen: boolean,
  ): void {
    period
      .daysOfWeek()
      .value.update((days) =>
        isChosen
          ? [...days, day].sort((first, second) => first - second)
          : days.filter((chosen) => chosen !== day),
      );
  }
}
