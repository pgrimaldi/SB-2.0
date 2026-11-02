import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-services-bar',
  templateUrl: './services-bar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesBar {}
