import { HttpStatusCode } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  model,
  output,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { TableErrorPopup } from '../../shared/ui/dialogs/table-error-popup/table-error-popup';

/**
 * The read at the opening of an edit popup or panel failed: the caller closes it and shows this
 * instead of an empty form. Riprova is missing for a 404, where the element is gone.
 */
@Component({
  selector: 'app-read-error-popup',
  imports: [TableErrorPopup, TranslatePipe],
  templateUrl: './read-error-popup.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReadErrorPopup {
  readonly problem = model<ApiProblem | null>(null);
  /** [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly retry = output<void>();

  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly isGone = computed(() => this.problem()?.status === HttpStatusCode.NotFound);
  protected readonly message = toSignal(
    toObservable(this.problem).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  protected closed(isOpen: boolean): void {
    if (!isOpen) {
      this.problem.set(null);
    }
  }
}
