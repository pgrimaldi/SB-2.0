import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { DayCalendar } from '../../../../shared/ui/datepickers/day-calendar/day-calendar';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';

@Component({
  selector: 'app-public-holiday-add-popup',
  imports: [Button, DayCalendar, TranslatePipe],
  templateUrl: './public-holiday-add-popup.html',
  styleUrl: './public-holiday-add-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class PublicHolidayAddPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  /** The days in the list already: they cannot be chosen again. */
  readonly takenDates = input<readonly string[]>([]);
  readonly confirmed = output<string[]>();

  private readonly unsaved = inject(UnsavedChangesBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  /** [previous month, next month]. */
  protected readonly calendarIcons = [
    '/assets/images/GeneralIcon_Previous.svg',
    '/assets/images/GeneralIcon_Next.svg',
  ] as const;
  protected readonly dates = signal<string[]>([]);

  constructor() {
    super();
    this.unsaved.watch(this);
    effect(() => {
      this.open();
      untracked(() => this.dates.set([]));
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && this.dates().length > 0;
  }

  protected confirm(): void {
    this.confirmed.emit(this.dates());
    this.close();
  }

  protected async leave(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.close();
    }
  }

  protected override closeRequested(): void {
    void this.leave();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(32rem, 95vw)',
      maxWidth: 'none',
      maxHeight: '95vh',
      panelClass: 'public__holiday__add__popup__panel',
      disableClose: true,
      autoFocus: 'dialog',
    };
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
