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
import { FormField, TreeValidationResult, disabled, form, submit } from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { WarehouseItem } from '../../../../../entities/warehouse/warehouse-item';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { greaterThan } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

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
  readonly item = input<WarehouseItem | null>(null);
  readonly saved = output<void>();

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
  protected readonly draft = signal(new WarehouseItem());
  protected readonly draftForm = form(this.draft, (path) => {
    greaterThan(path.totalQuantity, 0);
    disabled(
      path.isThresholdWarningActive,
      ({ valueOf }) => (valueOf(path.thresholdQuantity) ?? 0) <= 0,
    );
  });
  protected readonly totalQuantityError = this.validationText.message(
    this.draftForm.totalQuantity,
    'management.settings.warehouse.form.field_names.total',
  );
  protected readonly thresholdQuantityError = this.validationText.message(
    this.draftForm.thresholdQuantity,
  );
  protected readonly isThresholdWarningActiveError = this.validationText.message(
    this.draftForm.isThresholdWarningActive,
  );
  protected readonly isLoading = signal(false);
  private opened: WarehouseItem | null = null;
  protected readonly canSave = computed(
    () => this.draft().idArticle !== null && this.draftForm().valid(),
  );
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
      if (this.draftForm.isThresholdWarningActive().disabled()) {
        untracked(() => this.draftForm.isThresholdWarningActive().value.set(false));
      }
    });
    effect(() => {
      if (this.open()) {
        untracked(() => this.fill());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    if (!this.open() || !this.opened) {
      return false;
    }
    const draft = this.draft();
    return (
      draft.totalQuantity !== this.opened.totalQuantity ||
      draft.thresholdQuantity !== this.opened.thresholdQuantity ||
      draft.isThresholdWarningActive !== this.opened.isThresholdWarningActive
    );
  }

  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (this.isLoading() || !idProperty || !this.canSave()) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.warehouse.editWarehouseItem(idProperty, this.draft()));
        this.isLoading.set(false);
        this.close();
        this.saved.emit();
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
      panelClass: 'form__warehouse__edit__item__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private fill(): void {
    const item = this.item();
    const draft = item
      ? {
          ...item,
          isThresholdWarningActive:
            item.isThresholdWarningActive && (item.thresholdQuantity ?? 0) > 0,
        }
      : new WarehouseItem();
    this.draft.set(draft);
    this.draftForm().reset();
    this.opened = item ? draft : null;
    this.error.set(null);
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
