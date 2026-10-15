import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { UnsavedChangesBehaviour } from '../../../behaviours/forms/unsaved-changes.behaviour';
import { ConfirmPopup } from '../../shared/ui/dialogs/confirm-popup/confirm-popup';

@Component({
  selector: 'app-unsaved-changes-popup',
  imports: [ConfirmPopup, TranslatePipe],
  templateUrl: './unsaved-changes-popup.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnsavedChangesPopup {
  protected readonly unsaved = inject(UnsavedChangesBehaviour);
  protected readonly closeIcon = ['/assets/images/Manager_UnsavedChanges_Close.svg'] as const;

  protected openChange(open: boolean): void {
    if (!open) {
      this.unsaved.answer(false);
    }
  }
}
