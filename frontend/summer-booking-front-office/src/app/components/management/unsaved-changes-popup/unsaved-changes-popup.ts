import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { UnsavedChangesBehaviour } from '../../../behaviours/forms/unsaved-changes.behaviour';
import { ConfirmPopup } from '../../shared/ui/dialogs/confirm-popup/confirm-popup';

/** The question of UnsavedChangesBehaviour, once in the layout for every page and popup. */
@Component({
  selector: 'app-unsaved-changes-popup',
  imports: [ConfirmPopup, TranslatePipe],
  templateUrl: './unsaved-changes-popup.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnsavedChangesPopup {
  protected readonly unsaved = inject(UnsavedChangesBehaviour);

  /** Closed without confirming (Resta, Esc, a click outside): the user stays. */
  protected openChange(open: boolean): void {
    if (!open) {
      this.unsaved.answer(false);
    }
  }
}
