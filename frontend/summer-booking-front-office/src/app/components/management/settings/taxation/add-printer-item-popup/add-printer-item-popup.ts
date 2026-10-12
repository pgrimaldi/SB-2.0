import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  effect,
  inject,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, TreeValidationResult, form, submit } from '@angular/forms/signals';
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
import { AddPrinterItemRequest } from '../../../../../entities/settings/taxation/add-printer-item-request';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { TaxationService } from '../../../../../services/api/taxation/taxation.service';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';

const VAT_RATES = ['0', '4', '5', '10', '22'];

@Component({
  selector: 'app-add-printer-item-popup',
  imports: [
    Button,
    Checkbox,
    FilledNumberField,
    FilledSelect,
    FilledTextField,
    FormField,
    TranslatePipe,
  ],
  templateUrl: './add-printer-item-popup.html',
  styleUrl: './add-printer-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class AddPrinterItemPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  readonly added = output<void>();

  private readonly taxation = inject(TaxationService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonSize = ButtonSize;
  protected readonly vatRateOptions: readonly SelectOption[] = VAT_RATES.map((rate) => ({
    value: rate,
    label: `${rate}%`,
  }));
  private readonly draft = signal(new AddPrinterItemRequest());
  protected readonly draftForm = form(this.draft);
  protected readonly printerNameError = this.validationText.message(this.draftForm.printerName);
  protected readonly printerIPError = this.validationText.message(this.draftForm.printerIP);
  protected readonly printerPortError = this.validationText.message(this.draftForm.printerPort);
  protected readonly printerModelError = this.validationText.message(this.draftForm.printerModel);
  protected readonly isSslError = this.validationText.message(this.draftForm.isSsl);
  protected readonly isFiscalModeError = this.validationText.message(this.draftForm.isFiscalMode);
  protected readonly isNonFiscalModeError = this.validationText.message(
    this.draftForm.isNonFiscalMode,
  );
  protected readonly isOrderModeError = this.validationText.message(this.draftForm.isOrderMode);
  protected readonly isTextModeError = this.validationText.message(this.draftForm.isTextMode);
  protected readonly departmentVatRatesError = this.validationText.message(
    this.draftForm.departmentVatRates,
  );
  protected readonly firstHeaderError = this.validationText.message(this.draftForm.firstHeader);
  protected readonly secondHeaderError = this.validationText.message(this.draftForm.secondHeader);
  protected readonly footerError = this.validationText.message(this.draftForm.footer);
  protected readonly isLoading = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly closeIcon = '/assets/images/CloseAddPrinter_setting.svg';

  constructor() {
    super();
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() && JSON.stringify(this.draft()) !== JSON.stringify(new AddPrinterItemRequest())
    );
  }

  protected save(): void {
    const idProperty = this.auth.user()?.idProperty;
    if (this.isLoading() || !idProperty) {
      return;
    }
    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.taxation.addPrinter(idProperty, this.draft()));
        this.isLoading.set(false);
        this.close();
        this.added.emit();
        return undefined;
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
      width: 'min(32rem, 90vw)',
      maxWidth: 'none',
      maxHeight: '90vh',
      panelClass: 'add__printer__item__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.draft.set(new AddPrinterItemRequest());
    this.draftForm().reset();
    this.error.set(null);
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
