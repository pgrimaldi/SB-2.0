import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  model,
  signal,
  untracked,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormField, applyWhen, form, max } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { OnlineBookingAudience } from '../../../../../entities/enums/online-booking-audience';
import { OnlineBookingBlock } from '../../../../../entities/enums/online-booking-block';
import { SupplementType } from '../../../../../entities/enums/supplement-type';
import { BookingLimitRule } from '../../../../../entities/settings/online-booking-configuration/booking-limit-rule';
import { LegendItem } from '../../../../../entities/settings/online-booking-configuration/legend-item';
import { OnlineBookingOptions } from '../../../../../entities/settings/online-booking-configuration/online-booking-options';
import { greaterThan } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { PeriodCalendar } from '../../../../shared/ui/datepickers/period-calendar/period-calendar';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTimeField } from '../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

interface BookingOptionsTexts {
  supplement_types: Record<'none' | 'percentage' | 'fixed', string>;
  online_booking_blocks: Record<OnlineBookingBlockKey, string>;
  online_booking_audiences: Record<'all' | 'contacts_only', string>;
  yes: string;
  no: string;
}

/** The select wants text keys: the answer it shows for a yes/no field. */
enum Answer {
  Yes = 'Yes',
  No = 'No',
}

type OnlineBookingBlockKey =
  | 'disabled'
  | 'three_hours_before'
  | 'five_hours_before'
  | 'eight_hours_before'
  | 'twelve_hours_before'
  | 'twenty_four_hours_before'
  | 'forty_eight_hours_before'
  | 'seventy_two_hours_before';

const ONLINE_BOOKING_BLOCK_KEYS: Readonly<Record<OnlineBookingBlock, OnlineBookingBlockKey>> = {
  [OnlineBookingBlock.Disabled]: 'disabled',
  [OnlineBookingBlock.ThreeHoursBefore]: 'three_hours_before',
  [OnlineBookingBlock.FiveHoursBefore]: 'five_hours_before',
  [OnlineBookingBlock.EightHoursBefore]: 'eight_hours_before',
  [OnlineBookingBlock.TwelveHoursBefore]: 'twelve_hours_before',
  [OnlineBookingBlock.TwentyFourHoursBefore]: 'twenty_four_hours_before',
  [OnlineBookingBlock.FortyEightHoursBefore]: 'forty_eight_hours_before',
  [OnlineBookingBlock.SeventyTwoHoursBefore]: 'seventy_two_hours_before',
};

@Component({
  selector: 'app-online-booking-options-panel',
  imports: [
    Button,
    Checkbox,
    FilledDateField,
    FilledNumberField,
    FilledSelect,
    FilledTextField,
    FilledTimeField,
    FormField,
    FormRow,
    FormRowLabel,
    IconButton,
    PeriodCalendar,
    SectionTitle,
    SidePanel,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './online-booking-options-panel.html',
  styleUrl: './online-booking-options-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnlineBookingOptionsPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();

  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  /** [previous month, next month, remove a period]. */
  protected readonly calendarIcons = [
    '/assets/images/OnlineBookingConfiguration_PreviousMonth.svg',
    '/assets/images/OnlineBookingConfiguration_NextMonth.svg',
    '/assets/images/OnlineBookingConfiguration_RemovePeriod.svg',
  ] as const;
  protected readonly clockIcon = ['/assets/images/OnlineBookingConfiguration_Clock.svg'] as const;
  protected readonly calendarIcon = [
    '/assets/images/OnlineBookingConfiguration_Calendar.svg',
  ] as const;
  protected readonly addIcon = ['/assets/images/OnlineBookingConfiguration_AddRule.svg'] as const;
  protected readonly removeIcon = [
    '/assets/images/OnlineBookingConfiguration_DeleteRule.svg',
  ] as const;
  /** Example rows until an API gives the rows and sectors of the property. */
  protected readonly sectorOptions: readonly SelectOption[] = [
    { value: '7c0e2a51-3b9f-4d6e-8a21-5f4b9c1d0e01', label: 'Fila 1' },
    { value: '7c0e2a51-3b9f-4d6e-8a21-5f4b9c1d0e02', label: 'Fila 2' },
    { value: '7c0e2a51-3b9f-4d6e-8a21-5f4b9c1d0e03', label: 'Fila 3' },
  ];

  private readonly draft = signal(new OnlineBookingOptions());
  protected readonly draftForm = form(this.draft, (path) => {
    applyWhen(
      path.supplementValue,
      ({ valueOf }) => valueOf(path.supplementType) !== SupplementType.None,
      (supplementValue) => greaterThan(supplementValue, 0),
    );
    max(path.supplementValue, ({ valueOf }) =>
      valueOf(path.supplementType) === SupplementType.Percentage ? 100 : undefined,
    );
  });
  protected readonly halfDays = [
    {
      label: 'management.settings.online_booking_configuration.booking_options.morning',
      start: this.draftForm.morningStartTime,
      end: this.draftForm.morningEndTime,
    },
    {
      label: 'management.settings.online_booking_configuration.booking_options.afternoon',
      start: this.draftForm.afternoonStartTime,
      end: this.draftForm.afternoonEndTime,
    },
  ];
  protected readonly hasSupplement = computed(
    () => this.draftForm.supplementType().value() !== SupplementType.None,
  );
  protected readonly isPercentage = computed(
    () => this.draftForm.supplementType().value() === SupplementType.Percentage,
  );
  protected readonly supplementValueError = this.validationText.message(
    this.draftForm.supplementValue,
    'management.settings.online_booking_configuration.booking_options.field_names.supplement_value',
  );
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.online_booking_configuration.booking_options',
    ) as Observable<BookingOptionsTexts>,
    { initialValue: null },
  );
  protected readonly supplementTypeOptions = computed<readonly SelectOption<SupplementType>[]>(
    () => [
      { value: SupplementType.None, label: this.texts()?.supplement_types.none ?? '' },
      { value: SupplementType.Percentage, label: this.texts()?.supplement_types.percentage ?? '' },
      { value: SupplementType.Fixed, label: this.texts()?.supplement_types.fixed ?? '' },
    ],
  );
  protected readonly answerOptions = computed<readonly SelectOption<Answer>[]>(() => [
    { value: Answer.Yes, label: this.texts()?.yes ?? '' },
    { value: Answer.No, label: this.texts()?.no ?? '' },
  ]);
  protected readonly bookingConfirmation = computed(() =>
    this.draftForm.isBookingConfirmationRequired().value() ? Answer.Yes : Answer.No,
  );
  protected readonly onlineBookingAudienceOptions = computed<
    readonly SelectOption<OnlineBookingAudience>[]
  >(() => [
    {
      value: OnlineBookingAudience.All,
      label: this.texts()?.online_booking_audiences.all ?? '',
    },
    {
      value: OnlineBookingAudience.ContactsOnly,
      label: this.texts()?.online_booking_audiences.contacts_only ?? '',
    },
  ]);
  protected readonly onlineBookingBlockOptions = computed<
    readonly SelectOption<OnlineBookingBlock>[]
  >(() =>
    Object.values(OnlineBookingBlock).map((value) => ({
      value,
      label: this.texts()?.online_booking_blocks[ONLINE_BOOKING_BLOCK_KEYS[value]] ?? '',
    })),
  );

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
    effect(() => {
      if (!this.draftForm.isSelectedPeriodsOnlyEnabled().value()) {
        untracked(() => {
          if (this.draft().selectedPeriods.length) {
            this.draftForm.selectedPeriods().value.set([]);
          }
        });
      }
    });
    effect(() => {
      if (!this.draftForm.isHalfDayBookingEnabled().value()) {
        untracked(() =>
          [
            this.draftForm.morningStartTime,
            this.draftForm.morningEndTime,
            this.draftForm.afternoonStartTime,
            this.draftForm.afternoonEndTime,
          ]
            .filter((time) => time().value() !== null)
            .forEach((time) => time().value.set(null)),
        );
      }
    });
    effect(() => {
      if (!this.hasSupplement()) {
        untracked(() => this.draftForm.supplementValue().value.set(null));
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() && JSON.stringify(this.draft()) !== JSON.stringify(new OnlineBookingOptions())
    );
  }

  protected addBookingLimitRule(): void {
    this.draftForm.bookingLimitRules().value.update((rules) => [...rules, new BookingLimitRule()]);
  }

  protected removeBookingLimitRule(index: number): void {
    this.draftForm
      .bookingLimitRules()
      .value.update((rules) => rules.filter((_, ruleIndex) => ruleIndex !== index));
  }

  protected addLegendItem(): void {
    this.draftForm.legendItems().value.update((items) => [...items, new LegendItem()]);
  }

  protected removeLegendItem(index: number): void {
    this.draftForm
      .legendItems()
      .value.update((items) => items.filter((_, itemIndex) => itemIndex !== index));
  }

  protected setBookingConfirmation(answer: Answer | null): void {
    this.draftForm.isBookingConfirmationRequired().value.set(answer === Answer.Yes);
  }

  protected async leave(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.open.set(false);
    }
  }

  private reset(): void {
    this.draft.set(new OnlineBookingOptions());
    this.draftForm().reset();
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}
