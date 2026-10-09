import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

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

}
