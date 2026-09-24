import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

/** Hub card showing a single centred logo (e.g. a press outlet). */
@Component({
  selector: 'app-card-logo',
  imports: [MatCardModule],
  templateUrl: './card-logo.html',
  styleUrl: './card-logo.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardLogo {
  readonly logoSrc = input.required<string>();
  readonly logoAlt = input.required<string>();
}
