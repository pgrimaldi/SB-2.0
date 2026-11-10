import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-additional-items',
  templateUrl: './additional-items.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdditionalItems {}
