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
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  FieldTree,
  FormField,
  MAX_LENGTH,
  TreeValidationResult,
  applyEach,
  applyWhen,
  form,
  max,
  metadata,
  min,
  required,
  submit,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import {
  apiFieldError,
  apiFieldErrorsOrGeneral,
} from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { DiscountScope } from '../../../../../entities/enums/discount-scope';
import { DiscountType } from '../../../../../entities/enums/discount-type';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  MAX_FIXED_DISCOUNT,
  MAX_VOUCHER_CODE_LENGTH,
  MAX_VOUCHER_NAME_LENGTH,
  MAX_VOUCHER_PERIOD_QUANTITY,
  MAX_VOUCHER_TIME_SLOT_QUANTITY,
  MAX_VOUCHER_USE_QUANTITY,
  VoucherAddItemRequest,
} from '../../../../../entities/settings/vouchers/voucher-add-item-request';
import { VoucherTimeSlot } from '../../../../../entities/settings/vouchers/voucher-time-slot';
import { VoucherValidityPeriod } from '../../../../../entities/settings/vouchers/voucher-validity-period';
import { DAYS_OF_WEEK } from '../../../../../entities/shared/days-of-week';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { VoucherService } from '../../../../../services/api/vouchers/voucher.service';
import { showFirstError } from '../../../../shared/forms/first-error';
import {
  greaterThan,
  minItems,
  requiredText,
  timeRange,
} from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTimeField } from '../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { comparableVoucher } from '../voucher-scope/comparable-voucher';
import { hiddenVoucherFields } from '../voucher-scope/hidden-voucher-fields';
import { VoucherScope } from '../voucher-scope/voucher-scope';
import { withScope } from '../voucher-scope/with-scope';

type DayKey = (typeof DAYS_OF_WEEK)[number]['key'];

@Component({
  selector: 'app-voucher-add-item-panel',
  imports: [
    Button,
    Checkbox,
    FilledDateField,
    FilledMultiSelect,
    FilledNumberField,
    FilledSelect,
    FilledTextField,
    FilledTimeField,
    FormField,
    FormRow,
    FormRowLabel,
    IconButton,
    SectionTitle,
    Toggle,
    TranslatePipe,
    VoucherScope,
  ],
  templateUrl: './voucher-add-item-panel.html',
  styleUrl: './voucher-add-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class VoucherAddItemPanel extends BasePopup implements UnsavedChanges, OnDestroy {
  readonly added = output<void>();

  private readonly vouchers = inject(VoucherService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly injector = inject(EnvironmentInjector);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');

  private readonly draft = signal(new VoucherAddItemRequest());
  protected readonly draftForm = form(this.draft, (path) => {
    requiredText(path.voucherName);
    metadata(path.voucherName, MAX_LENGTH, () => MAX_VOUCHER_NAME_LENGTH);
    requiredText(path.voucherCode);
    metadata(path.voucherCode, MAX_LENGTH, () => MAX_VOUCHER_CODE_LENGTH);
    // An automatic discount, without a code to type, is valid only in its periods.
    applyWhen(
      path,
      ({ value }) => !value().isCodeRequired,
      (automatic) => minItems(automatic.validityPeriods, 1),
    );
    applyEach(path.validityPeriods, (period) => {
      required(period.validFrom);
      required(period.validTo);
      minItems(period.daysOfWeek, 1);
      applyEach(period.timeSlots, (timeSlot) => {
        required(timeSlot.from);
        required(timeSlot.to);
        timeRange(timeSlot.from, timeSlot.to);
      });
    });
    required(path.discountType);
    required(path.discountValue);
    greaterThan(path.discountValue, 0);
    max(path.discountValue, ({ valueOf }) =>
      valueOf(path.discountType) === DiscountType.Percentage ? 100 : MAX_FIXED_DISCOUNT,
    );
    required(path.discountScope);
    applyWhen(
      path,
      ({ value }) => value().discountScope !== DiscountScope.All && !value().idSectors.length,
      (limited) => minItems(limited.serviceCategories, 1),
    );
    min(path.maxUseQuantity, 1);
    max(path.maxUseQuantity, MAX_VOUCHER_USE_QUANTITY);
  });
  protected readonly isPercentage = computed(
    () => this.draftForm.discountType().value() === DiscountType.Percentage,
  );
  protected readonly discountTypeError = this.validationText.message(this.draftForm.discountType);
  protected readonly discountValueError = this.validationText.message(
    this.draftForm.discountValue,
    'management.settings.vouchers.form.field_names.discount_value',
  );
  protected readonly maxUseQuantityError = this.validationText.message(
    this.draftForm.maxUseQuantity,
  );
  protected readonly isEnabledError = this.validationText.message(this.draftForm.isEnabled);
  protected readonly voucherNameError = this.validationText.message(this.draftForm.voucherName);
  protected readonly voucherCodeError = this.validationText.message(this.draftForm.voucherCode);
  protected readonly canAddPeriod = computed(
    () => this.draftForm.validityPeriods().value().length < MAX_VOUCHER_PERIOD_QUANTITY,
  );
  protected readonly validityPeriodsError = this.validationText.message(
    this.draftForm.validityPeriods,
    'management.settings.vouchers.form.field_names.validity_period',
  );
  protected readonly sectorsVersion = signal(0);
  protected readonly isLoading = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly removePeriodIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly addPeriodIcon = ['/assets/images/GeneralIcon_Add.svg'] as const;
  protected readonly calendarIcon = ['/assets/images/GeneralIcon_Calendar.svg'] as const;
  protected readonly clockIcon = ['/assets/images/GeneralIcon_Clock.svg'] as const;
  protected readonly deleteTimeSlotIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;
  protected readonly addTimeSlotIcon = ['/assets/images/GeneralIcon_AddBlue.svg'] as const;
  private readonly dayNames = toSignal(
    this.translateService.stream('management.settings.vouchers.form.periods.days') as Observable<
      Record<DayKey, string>
    >,
    { initialValue: null },
  );
  private readonly typeNames = toSignal(
    this.translateService.stream('management.settings.vouchers.discount_type') as Observable<
      Record<'percentage' | 'fixed', string>
    >,
    { initialValue: null },
  );
  protected readonly typeOptions = computed<readonly SelectOption<DiscountType>[]>(() => [
    { value: DiscountType.Percentage, label: this.typeNames()?.percentage ?? '' },
    { value: DiscountType.Fixed, label: this.typeNames()?.fixed ?? '' },
  ]);
  protected readonly dayOptions = computed<readonly SelectOption[]>(() =>
    DAYS_OF_WEEK.map(({ number, key }) => ({
      value: String(number),
      label: this.dayNames()?.[key] ?? '',
    })),
  );

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
      this.open() &&
      comparableVoucher(this.draft()) !== comparableVoucher(new VoucherAddItemRequest())
    );
  }

  protected addPeriod(): void {
    this.draftForm
      .validityPeriods()
      .value.update((periods) => [...periods, new VoucherValidityPeriod()]);
  }

  protected removePeriod(index: number): void {
    this.draftForm
      .validityPeriods()
      .value.update((periods) => periods.filter((_, periodIndex) => periodIndex !== index));
  }

  protected chosenDays(period: FieldTree<VoucherValidityPeriod>): string[] {
    return period.daysOfWeek().value().map(String);
  }

  protected hasAllDays(period: FieldTree<VoucherValidityPeriod>): boolean {
    return period.daysOfWeek().value().length === DAYS_OF_WEEK.length;
  }

  protected setDays(period: FieldTree<VoucherValidityPeriod>, days: readonly string[]): void {
    period.daysOfWeek().value.set(days.map(Number).sort((a, b) => a - b));
    period.daysOfWeek().markAsTouched();
  }

  protected setAllDays(period: FieldTree<VoucherValidityPeriod>, isAll: boolean): void {
    period.daysOfWeek().value.set(isAll ? DAYS_OF_WEEK.map(({ number }) => number) : []);
    period.daysOfWeek().markAsTouched();
  }

  protected canAddTimeSlot(period: FieldTree<VoucherValidityPeriod>): boolean {
    return period.timeSlots().value().length < MAX_VOUCHER_TIME_SLOT_QUANTITY;
  }

  protected addTimeSlot(period: FieldTree<VoucherValidityPeriod>): void {
    period.timeSlots().value.update((timeSlots) => [...timeSlots, new VoucherTimeSlot()]);
  }

  protected removeTimeSlot(period: FieldTree<VoucherValidityPeriod>, index: number): void {
    period
      .timeSlots()
      .value.update((timeSlots) => timeSlots.filter((_, slotIndex) => slotIndex !== index));
  }

  protected save(): void {
    if (this.isLoading() || !this.auth.user()?.propertyId) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.vouchers.addVoucherItem(withScope(this.draft())));
        this.isLoading.set(false);
        this.close();
        this.added.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (problem.code === ApiErrorCode.CatalogSectorNotFound) {
          this.sectorsVersion.update((version) => version + 1);
        }
        if (problem.code === ApiErrorCode.PricingDiscountCodeTaken) {
          return apiFieldError(this.draftForm.voucherCode, problem.code, problem.args);
        }
        return apiFieldErrorsOrGeneral(
          problem,
          this.draftForm,
          (general) => this.error.set(general),
          hiddenVoucherFields(this.draft()),
        );
      } finally {
        this.isLoading.set(false);
      }
    }).then((isSaved) => {
      if (!isSaved) {
        showFirstError(this.panel(), '.voucher__add__item__panel__error', this.injector);
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
      width: 'min(42.3125rem, 95vw)',
      maxWidth: 'none',
      maxHeight: '95vh',
      panelClass: 'voucher__add__item__panel__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.draft.set(new VoucherAddItemRequest());
    this.draftForm().reset();
    this.error.set(null);
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
