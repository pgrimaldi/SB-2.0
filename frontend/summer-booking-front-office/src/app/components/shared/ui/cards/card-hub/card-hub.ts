import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-card-hub',
  imports: [MatCardModule],
  templateUrl: './card-hub.html',
  styleUrl: './card-hub.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardHub {
  readonly iconSrc = input.required<string>();
}
