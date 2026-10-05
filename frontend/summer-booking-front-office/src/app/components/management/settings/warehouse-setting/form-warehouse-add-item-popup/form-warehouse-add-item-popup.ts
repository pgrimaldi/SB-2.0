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

/**
 * Popup of the warehouse settings to add an article: the article (chosen among those of the property,
 * `POST /api/warehouse/combobox-list`, loaded at every opening), quantity, threshold and the switch of
 * the threshold alert, then Annulla or Aggiungi:
 * `<app-form-warehouse-add-item-popup [(open)]="adding" (added)="table.reload()" />`.
 * The alert switch can be turned on only with a threshold above 0; Aggiungi only works with an article
 * and a quantity above 0. Aggiungi calls `POST /api/warehouse/add-warehouse-item`: meanwhile its spinner turns
 * and the popup cannot be changed or closed; then the popup closes and `added` fires, or the error of
 * the API is shown above the buttons and the form stays as it was.
 */
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

  /** The article was added: the page reloads its list. */
  readonly added = output<void>();

  /** The articles of the property, for the select; none until they arrive (or if they do not). */
  private readonly articles = signal<readonly ComboboxItem[]>([]);
  protected readonly articleOptions = computed<readonly SelectOption[]>(() =>
    this.articles().map(({ id, value }) => ({ value: id, label: value })),
  );
  /** Id of the chosen article; `null` while none is chosen. */
  protected readonly article = signal<string | null>(null);
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
  /** True while the API adds the article: spinner on Aggiungi, popup blocked. */
  protected readonly isLoading = signal(false);
  /** Aggiungi works only with an article and a quantity above 0 (the full checks will come later). */
  protected readonly canAdd = computed(() => this.article() !== null && (this.quantity() ?? 0) > 0);
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
  protected readonly id = `form-warehouse-add-item-popup-${nextId++}`;

  constructor() {
    super();
    // Every opening starts from an empty form, with the articles of the property loaded again.
    effect(() => {
      if (this.open()) {
        untracked(() => {
          this.reset();
          this.loadArticles();
        });
      }
    });
  }

  /** Aggiungi: sends the article to the API; on success closes the popup and tells the page. */
  protected add(): void {
    const idProperty = this.auth.user()?.idProperty;
    const idArticle = this.article();
    const articleQuantity = this.quantity();
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
        thresholdQuantity: this.threshold(),
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
    this.article.set(null);
    this.quantity.set(null);
    this.threshold.set(null);
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
