import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { resolveIcons } from '../../icons/icons';

/**
 * Title of a section with the line under it; what is put inside (e.g. a button) goes on the right
 * and moves under the title when the row is too narrow, what is marked `appSectionTitleAside` right
 * after the text. The space around it is up to the page.
 */
@Component({
  selector: 'app-section-title',
  imports: [MatIconModule],
  templateUrl: './section-title.html',
  styleUrl: './section-title.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SectionTitle {
  readonly text = input<string>();
  /** Icons as Material icon names (Material Symbols font): [icon before the title]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [icon before the title]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
}
