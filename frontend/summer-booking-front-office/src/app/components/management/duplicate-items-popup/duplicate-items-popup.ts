import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { AlertPopup } from '../../shared/ui/dialogs/alert-popup/alert-popup';
import { ConfirmPopup } from '../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { PageSpinner } from '../../shared/ui/spinners/page-spinner/page-spinner';

/**
 * Translation group with `cancel`, `confirm`, `one.title`, `one.text` ({{name}}), `many.title` and
 * `many.text` ({{count}}).
 */
@Component({
  selector: 'app-duplicate-items-popup',
  imports: [AlertPopup, ConfirmPopup, PageSpinner, TranslatePipe],
  templateUrl: './duplicate-items-popup.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DuplicateItemsPopup {
  readonly open = model(false);
  readonly idItems = input<readonly string[]>([]);
  readonly itemName = input<string | null>(null);
  readonly translationKey = input.required<string>();
  readonly action = input.required<(idProperty: string, idItems: string[]) => Observable<void>>();
  /** [error icon of the alert]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly duplicated = output<readonly string[]>();

  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly isLoading = signal(false);
  protected readonly isErrorShown = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  protected duplicateItems(): void {
    const idProperty = this.auth.user()?.propertyId;
    const idItems = [...this.idItems()];
    if (this.isLoading() || !idProperty || !idItems.length) {
      return;
    }
    this.open.set(false);
    this.isLoading.set(true);
    this.action()(idProperty, idItems).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.duplicated.emit(idItems);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.error.set(toApiProblem(error));
        this.isErrorShown.set(true);
      },
    });
  }
}
