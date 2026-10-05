import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
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
 * the threshold alert, then Annulla or Aggiungi. `<app-form-warehouse-item-popup [(open)]="adding" />`.
 * For now only the form: no checks on the fields, and Aggiungi does nothing yet (the API comes later).
 */
@Component({
  selector: 'app-form-warehouse-item-popup',
  imports: [Button, FilledNumberField, FilledSelect, Toggle, TranslatePipe],
  templateUrl: './form-warehouse-item-popup.html',
  styleUrl: './form-warehouse-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class FormWarehouseItemPopup extends BasePopup {
  private readonly warehouse = inject(WarehouseService);
  private readonly auth = inject(AuthBehaviour);
  private readonly destroyRef = inject(DestroyRef);

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
  /** The threshold alert, on or off (independent of the threshold). */
  protected readonly thresholdAlert = signal(false);
  protected readonly closeIcon = '/assets/images/close.svg';
  protected readonly id = `form-warehouse-item-popup-${nextId++}`;

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

  protected dialogConfig(): MatDialogConfig {
    return {
      role: 'dialog',
      ariaLabelledBy: `${this.id}-title`,
      width: 'min(25rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'form__warehouse__item__popup__panel',
      // The keyboard starts in the first field, not on the X.
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.article.set(null);
    this.quantity.set(null);
    this.threshold.set(null);
    this.thresholdAlert.set(false);
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
