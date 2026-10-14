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
  required,
  submit,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
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

  private readonly articles = signal<readonly ComboboxItem[]>([]);
  private articlesSubscription?: Subscription;
  protected readonly articleOptions = computed<readonly SelectOption[]>(() =>
    this.articles().map(({ id, value }) => ({ value: id, label: value })),
  );
  private readonly draft = signal(new WarehouseItem());
  protected readonly draftForm = form(this.draft, (path) => {
    required(path.idArticle);
    greaterThan(path.totalQuantity, 0);
    disabled(
      path.isThresholdWarningActive,
      ({ valueOf }) => (valueOf(path.thresholdQuantity) ?? 0) <= 0,
    );
  });
  protected readonly idArticleError = this.validationText.message(this.draftForm.idArticle);
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
  protected readonly canAdd = computed(() => this.draftForm().valid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly closeIcon = '/assets/images/WarehouseSetting_Close.svg';

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
        untracked(() => {
          this.reset();
          this.loadArticles();
        });
      }
    });
  }

  hasUnsavedChanges(): boolean {
    const { idArticle, totalQuantity, thresholdQuantity, isThresholdWarningActive } = this.draft();
    return (
      this.open() &&
      (idArticle !== null ||
        totalQuantity !== null ||
        thresholdQuantity !== null ||
        isThresholdWarningActive)
    );
  }

  protected add(): void {
    const idProperty = this.auth.user()?.idProperty;
    if (this.isLoading() || !idProperty || !this.canAdd()) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.warehouse.addWarehouseItem(idProperty, this.draft()));
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
    this.draft.set(new WarehouseItem());
    this.draftForm().reset();
    this.error.set(null);
  }

  private loadArticles(): void {
    // A slower answer of an earlier opening must not replace the list of this one.
    this.articlesSubscription?.unsubscribe();
    const idProperty = this.auth.user()?.idProperty;
    if (!idProperty) {
      return;
    }
    this.articlesSubscription = this.warehouse.comboboxList(idProperty).subscribe({
      next: (articles) => this.articles.set(articles),
      error: () => this.articles.set([]),
    });
  }

  override ngOnDestroy(): void {
    this.articlesSubscription?.unsubscribe();
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
