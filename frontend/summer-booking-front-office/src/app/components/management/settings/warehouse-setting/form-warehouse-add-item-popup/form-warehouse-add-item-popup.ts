import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ViewEncapsulation,
  computed,
  effect,
  inject,
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
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
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
  selector: 'app-form-warehouse-add-item-popup',
  imports: [Button, FilledNumberField, FilledSelect, Toggle, TranslatePipe],
  templateUrl: './form-warehouse-add-item-popup.html',
  styleUrl: './form-warehouse-add-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class FormWarehouseAddItemPopup extends BasePopup {
  private readonly warehouse = inject(WarehouseService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly destroyRef = inject(DestroyRef);

  readonly added = output<void>();

  private readonly articles = signal<readonly ComboboxItem[]>([]);
  protected readonly articleOptions = computed<readonly SelectOption[]>(() =>
    this.articles().map(({ id, value }) => ({ value: id, label: value })),
  );
  protected readonly idArticle = signal<string | null>(null);
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
  protected readonly canAdd = computed(
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
  protected readonly id = `form-warehouse-add-item-popup-${nextId++}`;

  constructor() {
    super();
    effect(() => {
      if (this.open()) {
        untracked(() => {
          this.reset();
          this.loadArticles();
        });
      }
    });
  }

  protected add(): void {
    const idProperty = this.auth.user()?.idProperty;
    const idArticle = this.idArticle();
    const articleQuantity = this.articleQuantity();
    if (this.isLoading() || !idProperty || idArticle === null || articleQuantity === null) {
      return;
    }
    this.setLoading(true);
    this.error.set(null);
    this.warehouse
      .addWarehouseItem({
        idProperty,
        idArticle,
        articleQuantity,
        thresholdQuantity: this.thresholdQuantity(),
        isThresholdWarningActive: this.thresholdAlert(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.setLoading(false);
          this.close();
          this.added.emit();
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
      panelClass: 'form__warehouse__add__item__popup__panel',
      // The keyboard starts in the first field, not on the X.
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.idArticle.set(null);
    this.articleQuantity.set(null);
    this.thresholdQuantity.set(null);
    this.thresholdAlert.set(false);
    this.error.set(null);
  }

  private setLoading(loading: boolean): void {
    this.isLoading.set(loading);
    this.setClosable(!loading);
  }

  private loadArticles(): void {
    const idProperty = this.auth.user()?.idProperty;
    if (!idProperty) {
      return;
    }
    this.warehouse
      .comboboxList({ idProperty })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (articles) => this.articles.set(articles),
        error: () => this.articles.set([]),
      });
  }
}
