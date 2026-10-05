import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  input,
  linkedSignal,
  output,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { WarehouseItem } from '../../../../../entities/warehouse/warehouse-item';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { Button } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

let nextId = 0;

/**
 * Popup of the warehouse settings to change an article (pencil of its row), with the same size and
 * fields as the one to add it: the article, shown but locked, then total, threshold and the switch of
 * the threshold alert, filled with the values of the row, then Annulla or Salva:
 * `<app-form-warehouse-edit-item-popup [item]="editedItem()" [(open)]="editing" (saved)="table.reload()" />`.
 * The alert switch can be turned on only with a threshold above 0; Salva only works with a total above
 * 0. Salva calls `POST /api/warehouse/edit-warehouse-item`: meanwhile its spinner turns and the popup
 * cannot be changed or closed; then the popup closes and `saved` fires, or the error of the API is
 * shown above the buttons and the form stays as it was.
 */
@Component({
  selector: 'app-form-warehouse-edit-item-popup',
  imports: [Button, FilledNumberField, FilledSelect, Toggle, TranslatePipe],
  templateUrl: './form-warehouse-edit-item-popup.html',
  styleUrl: './form-warehouse-edit-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class FormWarehouseEditItemPopup extends BasePopup {
  private readonly warehouse = inject(WarehouseService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly destroyRef = inject(DestroyRef);

  /** The row to change, read when the popup opens. */
  readonly item = input<WarehouseItem | null>(null);
  /** The article was saved: the page reloads its list. */
  readonly saved = output<void>();

  /** The only choice of the locked select: the article of the row. */
  protected readonly articleOptions = computed<readonly SelectOption[]>(() => {
    const item = this.item();
    return item ? [{ value: item.idArticle, label: item.name }] : [];
  });
  /** Id of the article of the row. */
  protected readonly article = computed(() => this.item()?.idArticle ?? null);
  protected readonly quantity = signal<number | null>(null);
  /** Threshold of the article; empty is `null`. */
  protected readonly threshold = signal<number | null>(null);
  /** The threshold alert can be turned on only with a threshold above 0. */
  protected readonly canAlert = computed(() => (this.threshold() ?? 0) > 0);
  /** The threshold alert, on or off; it goes off by itself when the threshold is no longer above 0. */
  protected readonly thresholdAlert = linkedSignal<boolean, boolean>({
    source: this.canAlert,
    computation: (canAlert, previous) => canAlert && (previous?.value ?? false),
  });
  /** True while the API saves the article: spinner on Salva, popup blocked. */
  protected readonly isLoading = signal(false);
  /** Salva works only with a total above 0 (the full checks will come later). */
  protected readonly canSave = computed(
    () => this.article() !== null && (this.quantity() ?? 0) > 0,
  );
  /** Error of the last try, as the API answered it. */
  private readonly error = signal<ApiProblem | null>(null);
  /** Message of the error, in the language of the app. */
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly closeIcon = '/assets/images/close.svg';
  protected readonly id = `form-warehouse-edit-item-popup-${nextId++}`;

  constructor() {
    super();
    // Every opening starts from the values of the row.
    effect(() => {
      if (this.open()) {
        untracked(() => this.fill());
      }
    });
  }

  /** Salva: sends the new values to the API; on success closes the popup and tells the page. */
  protected save(): void {
    const idProperty = this.auth.user()?.idProperty;
    const idItem = this.article();
    const articleQuantity = this.quantity();
    if (this.isLoading() || !idProperty || idItem === null || articleQuantity === null) {
      return;
    }
    this.setLoading(true);
    this.error.set(null);
    this.warehouse
      .editWarehouseItem({
        idProperty,
        idItem,
        articleQuantity,
        thresholdQuantity: this.threshold(),
        isThresholdWarningActive: this.thresholdAlert(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.setLoading(false);
          this.close();
          this.saved.emit();
        },
        error: (error: unknown) => {
          this.setLoading(false);
          this.error.set(toApiProblem(error));
        },
      });
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      role: 'dialog',
      ariaLabelledBy: `${this.id}-title`,
      width: 'min(25rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'form__warehouse__edit__item__popup__panel',
      // The keyboard starts in the total (the article is locked), not on the X.
      autoFocus: 'input',
    };
  }

  /** Total, threshold and alert of the row (the alert stays off without a threshold above 0). */
  private fill(): void {
    const item = this.item();
    const threshold = item?.thresholdNumber ?? null;
    this.quantity.set(item?.total ?? null);
    this.threshold.set(threshold);
    // Set by hand the alert would win over the threshold: checked here too.
    this.thresholdAlert.set((item?.isThresholdWarningActive ?? false) && (threshold ?? 0) > 0);
    this.error.set(null);
  }

  private setLoading(loading: boolean): void {
    this.isLoading.set(loading);
    this.setClosable(!loading);
  }
}
