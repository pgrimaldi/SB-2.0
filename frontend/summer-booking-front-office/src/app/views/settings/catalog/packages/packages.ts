import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-packages',
  templateUrl: './packages.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Packages {}
