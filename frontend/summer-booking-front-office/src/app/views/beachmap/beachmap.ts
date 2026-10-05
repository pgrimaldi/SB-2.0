import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-beachmap',
  templateUrl: './beachmap.html',
  styleUrl: './beachmap.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Beachmap {}
