import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EnvironmentInjector,
  OnDestroy,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  MAX_LENGTH,
  TreeValidationResult,
  form,
  max,
  metadata,
  min,
  required,
  submit,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, firstValueFrom } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import {
  apiFieldErrorsOrGeneral,
  clearApiFieldErrors,
} from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { PrinterMode } from '../../../../../entities/enums/printer-mode';
import { PrinterModel } from '../../../../../entities/enums/printer-model';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  AddPrinterItemRequest,
  MAX_PRINTER_IP_LENGTH,
  MAX_PRINTER_LINE_LENGTH,
  MAX_PRINTER_NAME_LENGTH,
  MAX_PRINTER_PORT,
  VAT_RATES,
} from '../../../../../entities/settings/taxation/add-printer-item-request';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { TaxationService } from '../../../../../services/api/taxation/taxation.service';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { RadioGroup, RadioOption } from '../../../../shared/ui/radios/radio-group/radio-group';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { showFirstError } from '../../../../shared/forms/first-error';
import { comparableJson, trimmedText } from '../../../../shared/forms/text-comparison';
import { requiredText } from '../../../../shared/forms/validators';

interface PrinterFormTexts {
  modes: Record<PrinterMode, string>;
  models: Record<PrinterModel, string>;
  no_vat_rate: string;
}

/** The select wants text keys: the answer of a department without a rate. */
const NO_VAT_RATE = 'none';

@Component({
  selector: 'app-add-printer-item-popup',
  imports: [
    Button,
    Checkbox,
    FilledNumberField,
    FilledSelect,
    FilledTextField,
    FormField,
    RadioGroup,
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
  private readonly injector = inject(EnvironmentInjector);
  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonSize = ButtonSize;
  private readonly popup = viewChild<ElementRef<HTMLElement>>('popup');
  protected readonly isReady = computed(() => !!this.auth.user()?.propertyId);
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.taxation.printer.form',
    ) as Observable<PrinterFormTexts>,
    { initialValue: null },
  );
  protected readonly modeOptions = computed<readonly RadioOption<PrinterMode>[]>(() =>
    Object.values(PrinterMode).map((value) => ({
      value,
      label: this.texts()?.modes?.[value] ?? '',
    })),
  );
  protected readonly modelOptions = computed<readonly SelectOption<PrinterModel>[]>(() =>
    Object.values(PrinterModel).map((value) => ({
      value,
      label: this.texts()?.models?.[value] ?? '',
    })),
  );
  private readonly draft = signal(new AddPrinterItemRequest());
  protected readonly draftForm = form(this.draft, (path) => {
    requiredText(path.printerName);
    metadata(path.printerName, MAX_LENGTH, () => MAX_PRINTER_NAME_LENGTH);
    requiredText(path.printerIP);
    metadata(path.printerIP, MAX_LENGTH, () => MAX_PRINTER_IP_LENGTH);
    required(path.printerPort);
    min(path.printerPort, 1);
    max(path.printerPort, MAX_PRINTER_PORT);
    required(path.printerModel);
    required(path.printerMode);
    metadata(path.firstHeader, MAX_LENGTH, () => MAX_PRINTER_LINE_LENGTH);
    metadata(path.secondHeader, MAX_LENGTH, () => MAX_PRINTER_LINE_LENGTH);
    metadata(path.footer, MAX_LENGTH, () => MAX_PRINTER_LINE_LENGTH);
  });
  /** Only a fiscal printer has the departments. */
  protected readonly hasDepartments = computed(
    () => this.draft().printerMode === PrinterMode.Fiscal,
  );
  /** For each department, the rates no other department has. */
  protected readonly vatRateOptions = computed<readonly (readonly SelectOption[])[]>(() => {
    const rates = this.draft().departmentVatRates;
    return rates.map((chosen) => [
      { value: NO_VAT_RATE, label: this.texts()?.no_vat_rate ?? '' },
      ...VAT_RATES.filter((rate) => rate === chosen || !rates.includes(rate)).map((rate) => ({
        value: `${rate}`,
        label: `${rate}%`,
      })),
    ]);
  });
  protected readonly printerNameError = this.validationText.message(this.draftForm.printerName);
  protected readonly printerIPError = this.validationText.message(this.draftForm.printerIP);
  protected readonly printerPortError = this.validationText.message(this.draftForm.printerPort);
  protected readonly printerModelError = this.validationText.message(this.draftForm.printerModel);
  protected readonly isSslError = this.validationText.message(this.draftForm.isSsl);
  protected readonly printerModeError = this.validationText.message(this.draftForm.printerMode);
  protected readonly departmentVatRatesError = this.validationText.message(
    this.draftForm.departmentVatRates,
  );
  protected readonly firstHeaderError = this.validationText.message(this.draftForm.firstHeader);
  protected readonly secondHeaderError = this.validationText.message(this.draftForm.secondHeader);
  protected readonly footerError = this.validationText.message(this.draftForm.footer);
  protected readonly isLoading = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = this.errorText.message(this.error);

  constructor() {
    super();
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
    effect(() => {
      if (!this.hasDepartments()) {
        untracked(() =>
          this.draftForm
            .departmentVatRates()
            .value.set(new AddPrinterItemRequest().departmentVatRates),
        );
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() && comparableJson(this.draft()) !== comparableJson(new AddPrinterItemRequest())
    );
  }

  protected save(): void {
    if (this.isLoading() || !this.isReady()) {
      return;
    }
    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.taxation.addPrinter(this.printerToSend()));
        this.isLoading.set(false);
        this.close();
        this.added.emit();
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(
          toApiProblem(error),
          this.draftForm,
          (problem) => this.error.set(problem),
          this.hasDepartments() ? [] : ['departmentVatRates' satisfies keyof AddPrinterItemRequest],
        );
      } finally {
        this.isLoading.set(false);
      }
    }).then((isSaved) => {
      if (!isSaved || this.error()) {
        showFirstError(this.popup(), '.add__printer__item__popup__error', this.injector);
      }
    });
  }

  protected rateKey(rate: number | null): string {
    return rate === null ? NO_VAT_RATE : `${rate}`;
  }

  protected setRate(index: number, key: string | null): void {
    this.draftForm
      .departmentVatRates()
      .value.update((rates) =>
        rates.map((rate, rateIndex) =>
          rateIndex !== index ? rate : key === null || key === NO_VAT_RATE ? null : Number(key),
        ),
      );
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
      width: 'min(32rem, 90vw)',
      maxWidth: 'none',
      maxHeight: '90vh',
      panelClass: 'add__printer__item__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private printerToSend(): AddPrinterItemRequest {
    const printer = this.draft();
    return {
      ...printer,
      printerName: trimmedText(printer.printerName),
      printerIP: trimmedText(printer.printerIP),
      firstHeader: trimmedText(printer.firstHeader),
      secondHeader: trimmedText(printer.secondHeader),
      footer: trimmedText(printer.footer),
    };
  }

  private reset(): void {
    this.draft.set(new AddPrinterItemRequest());
    clearApiFieldErrors(this.draftForm);
    this.draftForm().reset();
    this.error.set(null);
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
