import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-bookings-view',
  imports: [TranslatePipe],
  templateUrl: './bookings-view.component.html',
  styleUrl: './bookings-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookingsViewComponent {}
