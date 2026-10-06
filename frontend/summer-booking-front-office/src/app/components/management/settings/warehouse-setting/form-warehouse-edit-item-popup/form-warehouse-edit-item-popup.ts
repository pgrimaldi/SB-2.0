import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnDestroy,
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
import { FormField, form } from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { WarehouseItem } from '../../../../../entities/warehouse/warehouse-item';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { greaterThan } from '../../../../shared/forms/validators';
import { Button } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

let nextId = 0;

interface EditedQuantities {
  articleQuantity: number | null;
  thresholdQuantity: number | null;
}

@Component({
  selector: 'app-form-warehouse-edit-item-popup',
  imports: [Button, FilledNumberField, FilledSelect, FormField, Toggle, TranslatePipe],
  templateUrl: './form-warehouse-edit-item-popup.html',
  styleUrl: './form-warehouse-edit-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class FormWarehouseEditItemPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  private readonly warehouse = inject(WarehouseService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly destroyRef = inject(DestroyRef);

  /** Read only when the popup opens. */
  readonly item = input<WarehouseItem | null>(null);
  readonly saved = output<void>();

  protected readonly articleOptions = computed<readonly SelectOption[]>(() => {
    const item = this.item();
    return item ? [{ value: item.idArticle, label: item.name }] : [];
  });
  protected readonly idArticle = computed(() => this.item()?.idArticle ?? null);
  private readonly quantities = signal<EditedQuantities>({
    articleQuantity: null,
    thresholdQuantity: null,
  });
  protected readonly quantitiesForm = form(this.quantities, (path) => {
    greaterThan(path.articleQuantity, 0);
  });
  protected readonly articleQuantityError = this.validationText.message(
    this.quantitiesForm.articleQuantity,
    'management.settings.warehouse.form.field_names.total',
  );
  protected readonly canAlert = computed(() => (this.quantities().thresholdQuantity ?? 0) > 0);
  /** Goes off by itself when the threshold is no longer above 0. */
  protected readonly thresholdAlert = linkedSignal<boolean, boolean>({
    source: this.canAlert,
    computation: (canAlert, previous) => canAlert && (previous?.value ?? false),
  });
  protected readonly isLoading = signal(false);
  /** The values the popup opened with: changed ones are unsaved. */
  private opened = {
    articleQuantity: null as number | null,
    thresholdQuantity: null as number | null,
    thresholdAlert: false,
  };
  protected readonly canSave = computed(
    () => this.idArticle() !== null && this.quantitiesForm().valid(),
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
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.fill());
      }
    });
  }

  protected save(): void {
    const idProperty = this.auth.user()?.idProperty;
    const idItem = this.idArticle();
    const { articleQuantity, thresholdQuantity } = this.quantities();
    if (this.isLoading() || !idProperty || idItem === null || articleQuantity === null) {
      return;
    }
    this.isLoading.set(true);
    this.error.set(null);
    this.warehouse
      .editWarehouseItem({
        idProperty,
        idItem,
        articleQuantity,
        thresholdQuantity,
        isThresholdWarningActive: this.thresholdAlert(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.close();
          this.saved.emit();
        },
        error: (error: unknown) => {
          this.isLoading.set(false);
          this.error.set(toApiProblem(error));
        },
      });
  }

  hasUnsavedChanges(): boolean {
    const quantities = this.quantities();
    return (
      this.open() &&
      (quantities.articleQuantity !== this.opened.articleQuantity ||
        quantities.thresholdQuantity !== this.opened.thresholdQuantity ||
        this.thresholdAlert() !== this.opened.thresholdAlert)
    );
  }

  /**
   * X, Esc and a click outside: not while saving; after asking with unsaved changes. Annulla closes
   * at once: whoever presses it wants to drop what was typed (user, 06/10/2026).
   */
  protected async leave(): Promise<void> {
    if (!this.isLoading() && (await this.unsaved.confirmDiscard(this))) {
      this.close();
    }
  }

  protected override closeRequested(): void {
    void this.leave();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      role: 'dialog',
      ariaLabelledBy: `${this.id}-title`,
      width: 'min(25rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'form__warehouse__edit__item__popup__panel',
      // Esc and a click outside go through closeRequested: they ask first with unsaved changes.
      disableClose: true,
      // The keyboard starts in the total (the article is locked), not on the X.
      autoFocus: 'input',
    };
  }

  private fill(): void {
    const item = this.item();
    const thresholdQuantity = item?.thresholdQuantity ?? null;
    this.quantities.set({ articleQuantity: item?.totalQuantity ?? null, thresholdQuantity });
    this.quantitiesForm().reset();
    // Set by hand the alert would win over the threshold: checked here too.
    this.thresholdAlert.set(
      (item?.isThresholdWarningActive ?? false) && (thresholdQuantity ?? 0) > 0,
    );
    this.opened = { ...this.quantities(), thresholdAlert: this.thresholdAlert() };
    this.error.set(null);
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
