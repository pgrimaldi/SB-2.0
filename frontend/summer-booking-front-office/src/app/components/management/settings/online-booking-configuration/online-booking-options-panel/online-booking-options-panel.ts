import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  effect,
  inject,
  input,
  model,
  signal,
  untracked,
} from '@angular/core';
import { FormField, form } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { OnlineBookingOptions } from '../../../../../entities/settings/online-booking-configuration/online-booking-options';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { PeriodCalendar } from '../../../../shared/ui/datepickers/period-calendar/period-calendar';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledTimeField } from '../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

@Component({
  selector: 'app-online-booking-options-panel',
  imports: [
    Button,
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

  private readonly unsaved = inject(UnsavedChangesBehaviour);

  protected readonly ButtonSize = ButtonSize;
  /** [previous month, next month, remove a period]. */
  protected readonly calendarIcons = [
    '/assets/images/OnlineBookingConfiguration_PreviousMonth.svg',
    '/assets/images/OnlineBookingConfiguration_NextMonth.svg',
    '/assets/images/OnlineBookingConfiguration_RemovePeriod.svg',
  ] as const;
  protected readonly clockIcon = ['/assets/images/OnlineBookingConfiguration_Clock.svg'] as const;

  private readonly draft = signal(new OnlineBookingOptions());
  protected readonly draftForm = form(this.draft);
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
