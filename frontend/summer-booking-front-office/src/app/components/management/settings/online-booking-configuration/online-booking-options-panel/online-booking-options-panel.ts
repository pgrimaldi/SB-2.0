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
import { Button } from '../../../../shared/ui/buttons/button/button';
import { PeriodCalendar } from '../../../../shared/ui/datepickers/period-calendar/period-calendar';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

@Component({
  selector: 'app-online-booking-options-panel',
  imports: [Button, FormField, PeriodCalendar, SidePanel, Toggle, TranslatePipe],
  templateUrl: './online-booking-options-panel.html',
  styleUrl: './online-booking-options-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnlineBookingOptionsPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();

  private readonly unsaved = inject(UnsavedChangesBehaviour);

  /** [previous month, next month, remove a period]. */
  protected readonly calendarIcons = [
    '/assets/images/OnlineBookingConfiguration_PreviousMonth.svg',
    '/assets/images/OnlineBookingConfiguration_NextMonth.svg',
    '/assets/images/OnlineBookingConfiguration_RemovePeriod.svg',
  ] as const;

  private readonly draft = signal(new OnlineBookingOptions());
  protected readonly draftForm = form(this.draft);

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
