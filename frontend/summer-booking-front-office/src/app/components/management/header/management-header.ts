import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-management-header',
  imports: [TranslatePipe],
  templateUrl: './management-header.html',
  styleUrl: './management-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManagementHeader {}
