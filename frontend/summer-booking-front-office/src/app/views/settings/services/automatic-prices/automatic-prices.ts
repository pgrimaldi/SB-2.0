import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-automatic-prices',
  templateUrl: './automatic-prices.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomaticPrices {}
