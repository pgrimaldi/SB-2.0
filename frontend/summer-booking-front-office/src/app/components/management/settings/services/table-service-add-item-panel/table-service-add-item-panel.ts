import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';

@Component({
  selector: 'app-table-service-add-item-panel',
  imports: [SidePanel, TranslatePipe],
  templateUrl: './table-service-add-item-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableServiceAddItemPanel {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
}
