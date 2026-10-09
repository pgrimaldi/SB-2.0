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
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { AlertPopup } from '../../../../shared/ui/dialogs/alert-popup/alert-popup';
import { ConfirmPopup } from '../../../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { PageSpinner } from '../../../../shared/ui/spinners/page-spinner/page-spinner';

/**
 * With `itemName` the question names the article (button of a row), otherwise it counts them.
 * Once confirmed the question closes and the whole page waits for the answer; an error goes in a
 * popup.
 */
@Component({
  selector: 'app-duplicate-warehouse-items-popup',
  imports: [AlertPopup, ConfirmPopup, PageSpinner, TranslatePipe],
  templateUrl: './duplicate-warehouse-items-popup.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DuplicateWarehouseItemsPopup {
  private readonly warehouse = inject(WarehouseService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);

  readonly open = model(false);
  readonly idItems = input<readonly string[]>([]);
  readonly itemName = input<string | null>(null);
  readonly duplicated = output<readonly string[]>();

  protected readonly isLoading = signal(false);
  protected readonly isErrorShown = signal(false);
  protected readonly errorIcon = ['/assets/images/WarehouseWarning_setting.svg'] as const;
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  protected duplicateItems(): void {
    const idProperty = this.auth.user()?.idProperty;
    const idItems = [...this.idItems()];
    if (this.isLoading() || !idProperty || !idItems.length) {
      return;
    }
    this.open.set(false);
    this.isLoading.set(true);
    this.warehouse.duplicateWarehouseItems({ idProperty, idItems }).subscribe({
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
