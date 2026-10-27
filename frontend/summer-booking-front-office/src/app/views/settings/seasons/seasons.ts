import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-seasons',
  templateUrl: './seasons.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Seasons {}
