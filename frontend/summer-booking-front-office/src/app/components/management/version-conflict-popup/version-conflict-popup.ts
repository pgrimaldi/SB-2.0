import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { TableErrorPopup } from '../../shared/ui/dialogs/table-error-popup/table-error-popup';

/**
 * A save answered 409 `resource.version_conflict`: someone changed the data after the read. The
 * caller reads the data again and shows this.
 */
@Component({
  selector: 'app-version-conflict-popup',
  imports: [TableErrorPopup, TranslatePipe],
  templateUrl: './version-conflict-popup.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VersionConflictPopup {
  readonly open = model(false);
  /** [X]. */
  readonly pathIcon = input<readonly string[] | null>();
}
