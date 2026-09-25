import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Beach map of the management area (private: reachable only after sign-in). Empty for now. */
@Component({
  selector: 'app-beachmap',
  templateUrl: './beachmap.html',
  styleUrl: './beachmap.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Beachmap {}
