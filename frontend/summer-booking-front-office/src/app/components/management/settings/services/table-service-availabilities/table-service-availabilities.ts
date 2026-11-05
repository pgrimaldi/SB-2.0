import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { TableServiceAvailability } from '../../../../../entities/settings/services/table-service-availability';
import { TableServiceAvailabilityDay } from '../../../../../entities/settings/services/table-service-availability-day';
import { DAYS_OF_WEEK } from '../../../../../entities/shared/days-of-week';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { FilledTimeField } from '../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

type DayKey = (typeof DAYS_OF_WEEK)[number]['key'];

/** "Disponibilità" of a table service: period, days and hours of each availability. */
@Component({
  selector: 'app-table-service-availabilities',
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
  templateUrl: './table-service-availabilities.html',
  styleUrl: './table-service-availabilities.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableServiceAvailabilities {
  /** The availabilities of the form of the panel, which validates and saves them. */
  readonly form = input.required<FieldTree<TableServiceAvailability[]>>();
  /** Only to look at: adding and removing availabilities are off. */
  readonly isReadonly = input(false);

  private readonly translateService = inject(TranslateService);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_Add.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly calendarIcon = ['/assets/images/GeneralIcon_Calendar.svg'] as const;
  protected readonly clockIcon = ['/assets/images/GeneralIcon_Clock.svg'] as const;
  private readonly dayTexts = toSignal(
    this.translateService.stream('management.settings.services.form.tables.days') as Observable<
      Record<DayKey, string>
    >,
    { initialValue: null },
  );
  private readonly dayNames = computed(
    () =>
      new Map<number, string>(
        DAYS_OF_WEEK.map(({ number, key }) => [number, this.dayTexts()?.[key] ?? '']),
      ),
  );

  protected dayName(dayOfWeek: number | null): string {
    return (dayOfWeek !== null && this.dayNames().get(dayOfWeek)) || '';
  }

  protected addAvailability(): void {
    this.form()().value.update((availabilities) => [...availabilities, newAvailability()]);
  }

  protected removeAvailability(index: number): void {
    this.form()().value.update((availabilities) =>
      availabilities.filter((_, availabilityIndex) => availabilityIndex !== index),
    );
  }

  protected chooseSameTime(
    availability: FieldTree<TableServiceAvailability>,
    isSameTimeForAllDays: boolean,
  ): void {
    if (isSameTimeForAllDays) {
      copyTimes(availability, availability.startTime().value(), availability.endTime().value());
    }
  }

  protected changeStartTime(
    availability: FieldTree<TableServiceAvailability>,
    startTime: string | null,
  ): void {
    if (availability.isSameTimeForAllDays().value()) {
      copyTimes(availability, startTime, availability.endTime().value());
    }
  }

  protected changeEndTime(
    availability: FieldTree<TableServiceAvailability>,
    endTime: string | null,
  ): void {
    if (availability.isSameTimeForAllDays().value()) {
      copyTimes(availability, availability.startTime().value(), endTime);
    }
  }
}

/** Empty, with a line for every day of the week. */
function newAvailability(): TableServiceAvailability {
  return Object.assign(new TableServiceAvailability(), {
    days: DAYS_OF_WEEK.map(({ number }) =>
      Object.assign(new TableServiceAvailabilityDay(), { dayOfWeek: number }),
    ),
  });
}

/** The hours of "Applica stessi orari per tutti i giorni" become those of every day. */
function copyTimes(
  availability: FieldTree<TableServiceAvailability>,
  startTime: string | null,
  endTime: string | null,
): void {
  availability.days().value.update((days) => days.map((day) => ({ ...day, startTime, endTime })));
}
