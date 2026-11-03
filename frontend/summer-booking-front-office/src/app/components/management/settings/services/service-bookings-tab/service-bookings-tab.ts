import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { BookingTimeInterval } from '../../../../../entities/enums/booking-time-interval';
import { ServiceBookingOption } from '../../../../../entities/settings/services/service-booking-option';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { InfoPopup } from '../../../../shared/ui/dialogs/info-popup/info-popup';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTimeField } from '../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

interface TimeIntervalTexts {
  fifteen_minutes: string;
  thirty_minutes: string;
  forty_five_minutes: string;
  one_hour: string;
  two_hours: string;
}

const TIME_INTERVAL_KEYS: Readonly<Record<BookingTimeInterval, keyof TimeIntervalTexts>> = {
  [BookingTimeInterval.FifteenMinutes]: 'fifteen_minutes',
  [BookingTimeInterval.ThirtyMinutes]: 'thirty_minutes',
  [BookingTimeInterval.FortyFiveMinutes]: 'forty_five_minutes',
  [BookingTimeInterval.OneHour]: 'one_hour',
  [BookingTimeInterval.TwoHours]: 'two_hours',
};

@Component({
  selector: 'app-service-bookings-tab',
  imports: [
    Button,
    FilledDateField,
    FilledNumberField,
    FilledSelect,
    FilledTimeField,
    FormField,
    FormRow,
    FormRowLabel,
    IconButton,
    InfoPopup,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './service-bookings-tab.html',
  styleUrl: './service-bookings-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceBookingsTab {
  readonly form = input.required<FieldTree<ServiceBookingOption[]>>();
  /** Only to look at: the buttons that change the data (icons, image, add, remove) are off. */
  readonly isReadonly = input(false);

  private readonly translateService = inject(TranslateService);

  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_Add.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly calendarIcon = ['/assets/images/GeneralIcon_Calendar.svg'] as const;
  protected readonly clockIcon = ['/assets/images/GeneralIcon_Clock.svg'] as const;
  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [X, before the title]. */
  protected readonly infoPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_InfoTitle.svg',
  ] as const;
  protected readonly showingTimeIntervalInfo = signal(false);

  private readonly timeIntervalTexts = toSignal(
    this.translateService.stream(
      'management.settings.services.form.bookings.time_intervals',
    ) as Observable<TimeIntervalTexts>,
    { initialValue: null },
  );
  protected readonly timeIntervalOptions = computed<readonly SelectOption<BookingTimeInterval>[]>(
    () =>
      Object.values(BookingTimeInterval).map((value) => ({
        value,
        label: this.timeIntervalTexts()?.[TIME_INTERVAL_KEYS[value]] ?? '',
      })),
  );

  protected addOption(): void {
    this.form()().value.update((options) => [...options, new ServiceBookingOption()]);
  }

  protected removeOption(index: number): void {
    this.form()().value.update((options) =>
      options.filter((_, optionIndex) => optionIndex !== index),
    );
  }
}
