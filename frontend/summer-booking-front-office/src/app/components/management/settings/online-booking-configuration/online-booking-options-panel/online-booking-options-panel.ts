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
import { SupplementType } from '../../../../../entities/enums/supplement-type';
import { OnlineBookingOptions } from '../../../../../entities/settings/online-booking-configuration/online-booking-options';
import { greaterThan } from '../../../../shared/forms/validators';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { PeriodCalendar } from '../../../../shared/ui/datepickers/period-calendar/period-calendar';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTimeField } from '../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

interface BookingOptionsTexts {
  supplement_types: Record<'none' | 'percentage' | 'fixed', string>;
}

@Component({
  selector: 'app-online-booking-options-panel',
  imports: [
    Button,
    FilledNumberField,
    FilledSelect,
    FilledTimeField,
    FormField,
    FormRow,
    FormRowLabel,
    PeriodCalendar,
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

  protected readonly ButtonSize = ButtonSize;
  /** [previous month, next month, remove a period]. */
  protected readonly calendarIcons = [
    '/assets/images/OnlineBookingConfiguration_PreviousMonth.svg',
    '/assets/images/OnlineBookingConfiguration_NextMonth.svg',
    '/assets/images/OnlineBookingConfiguration_RemovePeriod.svg',
  ] as const;
  protected readonly clockIcon = ['/assets/images/OnlineBookingConfiguration_Clock.svg'] as const;

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
