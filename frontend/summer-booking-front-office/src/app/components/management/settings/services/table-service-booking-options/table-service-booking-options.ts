import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { TableServiceBookingPayment } from '../../../../../entities/enums/table-service-booking-payment';
import { TableServiceDepositMode } from '../../../../../entities/enums/table-service-deposit-mode';
import { TableServiceMealDuration } from '../../../../../entities/enums/table-service-meal-duration';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { ConfirmPopup } from '../../../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { InfoPopup } from '../../../../shared/ui/dialogs/info-popup/info-popup';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { RadioGroup, RadioOption } from '../../../../shared/ui/radios/radio-group/radio-group';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

interface MealDurationTexts {
  thirty_minutes: string;
  forty_five_minutes: string;
  one_hour: string;
  one_hour_fifteen_minutes: string;
  one_hour_thirty_minutes: string;
  one_hour_forty_five_minutes: string;
  two_hours: string;
  two_hours_thirty_minutes: string;
  three_hours: string;
}

interface BookingOptionsTexts {
  meal_durations: MealDurationTexts;
  payments: {
    none: string;
    deposit: string;
  };
  deposit_modes: {
    total: string;
    per_person: string;
  };
}

const MEAL_DURATION_KEYS: Readonly<Record<TableServiceMealDuration, keyof MealDurationTexts>> = {
  [TableServiceMealDuration.ThirtyMinutes]: 'thirty_minutes',
  [TableServiceMealDuration.FortyFiveMinutes]: 'forty_five_minutes',
  [TableServiceMealDuration.OneHour]: 'one_hour',
  [TableServiceMealDuration.OneHourFifteenMinutes]: 'one_hour_fifteen_minutes',
  [TableServiceMealDuration.OneHourThirtyMinutes]: 'one_hour_thirty_minutes',
  [TableServiceMealDuration.OneHourFortyFiveMinutes]: 'one_hour_forty_five_minutes',
  [TableServiceMealDuration.TwoHours]: 'two_hours',
  [TableServiceMealDuration.TwoHoursThirtyMinutes]: 'two_hours_thirty_minutes',
  [TableServiceMealDuration.ThreeHours]: 'three_hours',
};

/** "Opzioni di prenotazione" of a table service: with a deposit, also how much and how it applies. */
@Component({
  selector: 'app-table-service-booking-options',
  imports: [
    ConfirmPopup,
    FilledNumberField,
    FilledSelect,
    FormField,
    FormRow,
    FormRowLabel,
    IconButton,
    InfoPopup,
    RadioGroup,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './table-service-booking-options.html',
  styleUrl: './table-service-booking-options.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableServiceBookingOptions {
  /** The fields of the form of the panel, which validates and saves them. */
  readonly averageMealDuration = input.required<FieldTree<TableServiceMealDuration | null>>();
  readonly maxBookingPerTimeSlotQuantity = input.required<FieldTree<number | null>>();
  readonly bookingPayment = input.required<FieldTree<TableServiceBookingPayment>>();
  readonly depositMode = input.required<FieldTree<TableServiceDepositMode>>();
  readonly depositPercentage = input.required<FieldTree<number | null>>();

  private readonly translateService = inject(TranslateService);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly TableServiceBookingPayment = TableServiceBookingPayment;
  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [X, before the title]. */
  protected readonly infoPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_InfoTitle.svg',
  ] as const;
  protected readonly showingDepositInfo = signal(false);
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly isAskingPaymentChange = signal(false);
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.services.form.tables',
    ) as Observable<BookingOptionsTexts>,
    { initialValue: null },
  );
  protected readonly mealDurations = computed<readonly SelectOption<TableServiceMealDuration>[]>(
    () =>
      Object.values(TableServiceMealDuration).map((value) => ({
        value,
        label: this.texts()?.meal_durations[MEAL_DURATION_KEYS[value]] ?? '',
      })),
  );
  protected readonly payments = computed<readonly RadioOption<TableServiceBookingPayment>[]>(() => [
    { value: TableServiceBookingPayment.None, label: this.texts()?.payments.none ?? '' },
    { value: TableServiceBookingPayment.Deposit, label: this.texts()?.payments.deposit ?? '' },
  ]);
  protected readonly depositModes = computed<readonly SelectOption<TableServiceDepositMode>[]>(
    () => [
      { value: TableServiceDepositMode.Total, label: this.texts()?.deposit_modes.total ?? '' },
      {
        value: TableServiceDepositMode.PerPerson,
        label: this.texts()?.deposit_modes.per_person ?? '',
      },
    ],
  );

  /** Leaving the deposit with its data typed asks first: the data is lost. */
  protected changePayment(payment: TableServiceBookingPayment | null): void {
    if (payment === TableServiceBookingPayment.None && this.hasDeposit()) {
      this.isAskingPaymentChange.set(true);
    }
  }

  protected confirmPaymentChange(): void {
    this.depositMode()().value.set(TableServiceDepositMode.Total);
    this.depositPercentage()().value.set(null);
    this.isAskingPaymentChange.set(false);
  }

  /** Closed without changing: back to the deposit, with its data. */
  protected closePaymentChange(isOpen: boolean): void {
    this.isAskingPaymentChange.set(isOpen);
    if (!isOpen && this.hasDeposit()) {
      this.bookingPayment()().value.set(TableServiceBookingPayment.Deposit);
    }
  }

  /** Something typed in the deposit: an amount, or "A persona" instead of the starting "Totale". */
  private hasDeposit(): boolean {
    return (
      this.depositPercentage()().value() !== null ||
      this.depositMode()().value() !== TableServiceDepositMode.Total
    );
  }
}
