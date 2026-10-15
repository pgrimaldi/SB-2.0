import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Title of a section with the line under it; what is put inside (e.g. a button) goes on the right
 * and moves under the title when the row is too narrow. The space around it is up to the page.
 */
@Component({
  selector: 'app-section-title',
  templateUrl: './section-title.html',
  styleUrl: './section-title.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SectionTitle {
  readonly text = input<string>();
}
