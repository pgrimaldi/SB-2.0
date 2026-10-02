import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { resolveIcons } from '../../icons/icons';

/**
 * Title of a page (its `h1`), with an icon on the left, usually the one of the section:
 * `<app-page-title [text]="'…' | translate" [pathIcon]="['/assets/images/warehouse-dark.svg']" />`.
 * Icons (see `Icons`): [icon]. The space around it is up to the page.
 */
@Component({
  selector: 'app-page-title',
  imports: [MatIconModule],
  templateUrl: './page-title.html',
  styleUrl: './page-title.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageTitle {
  /** Text of the title, already translated. */
  readonly text = input<string>();
  /** Icons as Material icon names (Material Symbols font): [icon]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [icon]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
}
