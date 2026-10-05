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

  /** Read only when the popup opens. */
  readonly item = input<WarehouseItem | null>(null);
  readonly saved = output<void>();

  protected readonly articleOptions = computed<readonly SelectOption[]>(() => {
    const item = this.item();
    return item ? [{ value: item.idArticle, label: item.name }] : [];
  });
  protected readonly idArticle = computed(() => this.item()?.idArticle ?? null);
  protected readonly articleQuantity = signal<number | null>(null);
  protected readonly thresholdQuantity = signal<number | null>(null);
  protected readonly canAlert = computed(() => (this.thresholdQuantity() ?? 0) > 0);
  /** Goes off by itself when the threshold is no longer above 0. */
  protected readonly thresholdAlert = linkedSignal<boolean, boolean>({
    source: this.canAlert,
    computation: (canAlert, previous) => canAlert && (previous?.value ?? false),
  });
  protected readonly isLoading = signal(false);
  /** Minimal check: the full validation will come later. */
  protected readonly canSave = computed(
    () => this.idArticle() !== null && (this.articleQuantity() ?? 0) > 0,
  );
  private readonly error = signal<ApiProblem | null>(null);
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
    effect(() => {
      if (this.open()) {
        untracked(() => this.fill());
      }
    });
  }

  protected save(): void {
    const idProperty = this.auth.user()?.idProperty;
    const idItem = this.idArticle();
    const articleQuantity = this.articleQuantity();
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
        thresholdQuantity: this.thresholdQuantity(),
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

  private fill(): void {
    const item = this.item();
    const thresholdQuantity = item?.thresholdQuantity ?? null;
    this.articleQuantity.set(item?.totalQuantity ?? null);
    this.thresholdQuantity.set(thresholdQuantity);
    // Set by hand the alert would win over the threshold: checked here too.
    this.thresholdAlert.set(
      (item?.isThresholdWarningActive ?? false) && (thresholdQuantity ?? 0) > 0,
    );
    this.error.set(null);
  }

  private setLoading(loading: boolean): void {
    this.isLoading.set(loading);
    this.setClosable(!loading);
  }
}
