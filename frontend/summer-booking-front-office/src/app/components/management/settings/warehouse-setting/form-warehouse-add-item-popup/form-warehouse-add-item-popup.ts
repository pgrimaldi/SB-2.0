import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  TreeValidationResult,
  disabled,
  form,
  max,
  required,
  submit,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { DropdownList } from '../../../../../behaviours/forms/dropdown-list';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { WarehouseItemAddStock } from '../../../../../entities/warehouse/warehouse-item-add-stock';
import { WarehouseItemAddRequest } from '../../../../../entities/warehouse/warehouse-item-add-request';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { greaterThan } from '../../../../shared/forms/validators';
import { MAX_WAREHOUSE_QUANTITY } from '../../../../../entities/warehouse/warehouse-limits';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

@Component({
  selector: 'app-form-warehouse-add-item-popup',
  imports: [Button, FilledNumberField, FilledSelect, FormField, Toggle, TranslatePipe],
  templateUrl: './form-warehouse-add-item-popup.html',
  styleUrl: './form-warehouse-add-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class FormWarehouseAddItemPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  readonly added = output<void>();

  private readonly warehouse = inject(WarehouseService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  protected readonly articles = new DropdownList(
    () => (this.auth.user()?.propertyId ? this.warehouse.articleCombobox() : EMPTY),
    this.errorText,
  );
  protected readonly articleOptions = computed<readonly SelectOption[]>(() =>
    this.articles.items().map(({ id, value }) => ({ value: id, label: value })),
  );
  private readonly draft = signal(new WarehouseItemAddStock());
  protected readonly draftForm = form(this.draft, (path) => {
    required(path.idArticle);
    greaterThan(path.totalQuantity, 0);
    max(path.totalQuantity, MAX_WAREHOUSE_QUANTITY);
    max(path.lowStockThreshold, MAX_WAREHOUSE_QUANTITY);
    disabled(
      path.lowStockAlertEnabled,
      ({ valueOf }) => (valueOf(path.lowStockThreshold) ?? 0) <= 0,
    );
  });
  protected readonly idArticleError = this.validationText.message(this.draftForm.idArticle);
  protected readonly totalQuantityError = this.validationText.message(
    this.draftForm.totalQuantity,
    'management.settings.warehouse.form.field_names.quantity',
  );
  protected readonly lowStockThresholdError = this.validationText.message(
    this.draftForm.lowStockThreshold,
  );
  protected readonly lowStockAlertEnabledError = this.validationText.message(
    this.draftForm.lowStockAlertEnabled,
  );
  protected readonly isLoading = signal(false);
  protected readonly canAdd = computed(() => this.draftForm().valid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    super();
    this.unsaved.watch(this);
    effect(() => {
      if (this.draftForm.lowStockAlertEnabled().disabled()) {
        untracked(() => this.draftForm.lowStockAlertEnabled().value.set(false));
      }
    });
    effect(() => {
      if (this.open()) {
        untracked(() => {
          this.reset();
          this.articles.load();
        });
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() && comparableJson(this.draft()) !== comparableJson(new WarehouseItemAddStock())
    );
  }

  protected add(): void {
    if (this.isLoading() || !this.auth.user()?.propertyId || !this.canAdd()) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      const request = this.request();
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.warehouse.addWarehouseItem(request));
        this.isLoading.set(false);
        this.close();
        this.added.emit();
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.draftForm, (problem) =>
          this.error.set(problem),
        );
      } finally {
        this.isLoading.set(false);
      }
    });
  }

  protected async leave(): Promise<void> {
    if (!this.isLoading() && (await this.unsaved.confirmDiscard(this))) {
      this.close();
    }
  }

  protected override isCloseDisabled(): boolean {
    return this.isLoading();
  }

  protected override closeRequested(): void {
    void this.leave();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(25rem, 90vw)',
      maxWidth: 'none',
      panelClass: 'form__warehouse__add__item__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.draft.set(new WarehouseItemAddStock());
    this.draftForm().reset();
    this.error.set(null);
    this.articles.clear();
  }

  /**
   * The popup does not show threshold and alert of the article: without a threshold both go as null
   * and stay as they are; with one, the alert goes as the popup shows it.
   */
  private request(): WarehouseItemAddRequest {
    const { idArticle, totalQuantity, lowStockThreshold, lowStockAlertEnabled } = this.draft();
    return {
      idArticle,
      totalQuantity,
      lowStockThreshold,
      lowStockAlertEnabled: lowStockThreshold === null ? null : lowStockAlertEnabled,
    };
  }

  override ngOnDestroy(): void {
    this.articles.destroy();
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
