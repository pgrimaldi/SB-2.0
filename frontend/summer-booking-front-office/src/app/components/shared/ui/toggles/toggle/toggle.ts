import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

let nextId = 0;

@Component({
  selector: 'app-toggle',
  imports: [MatSlideToggleModule],
  templateUrl: './toggle.html',
  styleUrl: './toggle.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Toggle {
  readonly name = input<string>();
  readonly checked = model(false);
  readonly disabled = input(false);
  readonly error = input<string | null>();
  /** Name for screen readers when the toggle has no visible label. */
  readonly accessibleLabel = input<string>();

  protected readonly id = `toggle-${nextId++}`;
}
