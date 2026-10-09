import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Spinner } from '../spinner/spinner';

/**
 * Covers the whole screen while a request runs, so nothing can be clicked until it answers (e.g. the
 * actions of a table row).
 */
@Component({
  selector: 'app-page-spinner',
  imports: [Spinner],
  templateUrl: './page-spinner.html',
  styleUrl: './page-spinner.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageSpinner {
  readonly isLoading = input(false);
}
