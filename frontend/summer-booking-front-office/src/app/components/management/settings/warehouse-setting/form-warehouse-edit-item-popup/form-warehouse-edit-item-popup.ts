import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  input,
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
  min,
  required,
  submit,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription, finalize, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { WarehouseItemStock } from '../../../../../entities/warehouse/warehouse-item-stock';
import { MAX_WAREHOUSE_QUANTITY } from '../../../../../entities/warehouse/warehouse-limits';
import { WarehouseSelectedItem } from '../../../../../entities/warehouse/warehouse-selected-item';
import { WarehouseSettingItem } from '../../../../../entities/warehouse/warehouse-setting-item';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { ReadErrorPopup } from '../../../read-error-popup/read-error-popup';
import { VersionConflictPopup } from '../../../version-conflict-popup/version-conflict-popup';

@Component({
  selector: 'app-form-warehouse-edit-item-popup',
  imports: [
    Button,
    FilledNumberField,
    FilledSelect,
    FormField,
    ReadErrorPopup,
    Toggle,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './form-warehouse-edit-item-popup.html',
  styleUrl: './form-warehouse-edit-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class FormWarehouseEditItemPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  readonly item = input<WarehouseSettingItem | null>(null);
  readonly saved = output<void>();
  readonly failed = output<ApiProblem>();

  private readonly warehouse = inject(WarehouseService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  protected readonly articleOptions = computed<readonly SelectOption[]>(() => {
    const item = this.item();
    return item?.idArticle ? [{ value: item.idArticle, label: item.name ?? '' }] : [];
  });
  protected readonly isLoading = signal(false);
  protected readonly isFetching = signal(false);
  private readonly loaded = signal<WarehouseSelectedItem | null>(null);
  private opened: WarehouseItemStock | null = null;
  protected readonly draft = signal(new WarehouseItemStock());
  protected readonly draftForm = form(this.draft, (path) => {
    required(path.totalQuantity);
    min(path.totalQuantity, ({ valueOf }) => valueOf(path.lowStockThreshold) ?? 0);
    max(path.totalQuantity, MAX_WAREHOUSE_QUANTITY);
    max(path.lowStockThreshold, ({ valueOf }) => valueOf(path.totalQuantity) ?? undefined);
    disabled(path, () => this.isFetching());
    disabled(
      path.lowStockAlertEnabled,
      ({ valueOf }) => (valueOf(path.lowStockThreshold) ?? 0) <= 0,
    );
  });
  protected readonly totalQuantityError = this.validationText.message(this.draftForm.totalQuantity);
  protected readonly lowStockThresholdError = this.validationText.message(
    this.draftForm.lowStockThreshold,
  );
  protected readonly lowStockAlertEnabledError = this.validationText.message(
    this.draftForm.lowStockAlertEnabled,
  );
  protected readonly canSave = computed(() => this.loaded() !== null && this.draftForm().valid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly readError = signal<ApiProblem | null>(null);
  protected readonly isConflictShown = signal(false);
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
    effect((onCleanup) => {
      const idArticle = this.item()?.idArticle ?? null;
      const isOpen = this.open();
      untracked(() => this.reset());
      if (isOpen && idArticle) {
        const subscription = untracked(() => this.fetch(idArticle));
        onCleanup(() => subscription.unsubscribe());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() &&
      this.opened !== null &&
      comparableJson(this.draft()) !== comparableJson(this.opened)
    );
  }

  protected save(): void {
    const loaded = this.loaded();
    const idArticle = loaded?.id;
    if (this.isLoading() || !this.auth.user()?.propertyId || !this.canSave() || !idArticle) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(
          this.warehouse.updateArticle(
            idArticle,
            {
              articleCode: loaded.articleCode,
              name: loaded.name,
              ...this.draft(),
            },
            loaded.version,
          ),
        );
        this.isLoading.set(false);
        this.close();
        this.saved.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.failed.emit(problem);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictShown.set(true);
          this.isLoading.set(false);
          this.close();
          return undefined;
        }
        return apiFieldErrorsOrGeneral(problem, this.draftForm, (general) =>
          this.error.set(general),
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
      panelClass: 'form__warehouse__edit__item__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  protected retryRead(): void {
    this.open.set(true);
  }

  private reset(): void {
    this.loaded.set(null);
    this.opened = null;
    this.draft.set(new WarehouseItemStock());
    this.draftForm().reset();
    this.error.set(null);
  }

  private fetch(idArticle: string): Subscription {
    this.isFetching.set(true);
    return this.warehouse
      .selectedArticle(idArticle)
      .pipe(finalize(() => this.isFetching.set(false)))
      .subscribe({
        next: (article) => {
          const stock: WarehouseItemStock = {
            totalQuantity: article.totalQuantity,
            lowStockThreshold: article.lowStockThreshold,
            lowStockAlertEnabled:
              article.lowStockAlertEnabled && (article.lowStockThreshold ?? 0) > 0,
          };
          this.loaded.set(article);
          this.opened = structuredClone(stock);
          this.draft.set(stock);
          this.draftForm().reset();
        },
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.failed.emit(problem);
          this.readError.set(problem);
          this.close();
        },
      });
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
