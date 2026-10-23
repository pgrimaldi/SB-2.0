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
  FieldTree,
  FormField,
  TreeValidationResult,
  applyEach,
  form,
  max,
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
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { DiscountScope } from '../../../../../entities/enums/discount-scope';
import { DiscountType } from '../../../../../entities/enums/discount-type';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { VoucherAddItemRequest } from '../../../../../entities/settings/vouchers/voucher-add-item-request';
import { VoucherTimeSlot } from '../../../../../entities/settings/vouchers/voucher-time-slot';
import { VoucherValidityPeriod } from '../../../../../entities/settings/vouchers/voucher-validity-period';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { VoucherService } from '../../../../../services/api/vouchers/voucher.service';
import { minItems, requiredText } from '../../../../shared/forms/validators';
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

/** ISO 8601 numbers: 1 is Monday, 7 is Sunday. */
const DAYS_OF_WEEK = [
  { number: 1, key: 'monday' },
  { number: 2, key: 'tuesday' },
  { number: 3, key: 'wednesday' },
  { number: 4, key: 'thursday' },
  { number: 5, key: 'friday' },
  { number: 6, key: 'saturday' },
  { number: 7, key: 'sunday' },
] as const;

type DayKey = (typeof DAYS_OF_WEEK)[number]['key'];

interface OptionsTexts {
  types: Record<'percentage' | 'fixed', string>;
  scopes: Record<'all' | 'services', string>;
}

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
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  private readonly draft = signal(new VoucherAddItemRequest());
  protected readonly draftForm = form(this.draft, (path) => {
    requiredText(path.voucherName);
    minItems(path.validityPeriods, 1);
    applyEach(path.validityPeriods, (period) => {
      required(period.validFrom);
      required(period.validTo);
      minItems(period.daysOfWeek, 1);
    });
    required(path.discountType);
    required(path.discountValue);
    min(path.discountValue, 1);
    max(path.discountValue, ({ valueOf }) =>
      valueOf(path.discountType) === DiscountType.Percentage ? 100 : undefined,
    );
    required(path.discountScope);
    min(path.maxUseQuantity, 1);
  });
  protected readonly isPercentage = computed(
    () => this.draftForm.discountType().value() === DiscountType.Percentage,
  );
  protected readonly discountTypeError = this.validationText.message(this.draftForm.discountType);
  protected readonly discountValueError = this.validationText.message(this.draftForm.discountValue);
  protected readonly discountScopeError = this.validationText.message(this.draftForm.discountScope);
  protected readonly maxUseQuantityError = this.validationText.message(
    this.draftForm.maxUseQuantity,
  );
  protected readonly isEnabledError = this.validationText.message(this.draftForm.isEnabled);
  protected readonly voucherNameError = this.validationText.message(this.draftForm.voucherName);
  protected readonly validityPeriodsError = this.validationText.message(
    this.draftForm.validityPeriods,
    'management.settings.vouchers.form.field_names.validity_period',
  );
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
  protected readonly addTimeSlotIcon = ['/assets/images/Vouchers_AddTimeSlot.svg'] as const;
  private readonly dayNames = toSignal(
    this.translateService.stream('management.settings.vouchers.form.periods.days') as Observable<
      Record<DayKey, string>
    >,
    { initialValue: null },
  );
  private readonly optionsTexts = toSignal(
    this.translateService.stream(
      'management.settings.vouchers.form.options',
    ) as Observable<OptionsTexts>,
    { initialValue: null },
  );
  protected readonly typeOptions = computed<readonly SelectOption<DiscountType>[]>(() => [
    { value: DiscountType.Percentage, label: this.optionsTexts()?.types.percentage ?? '' },
    { value: DiscountType.Fixed, label: this.optionsTexts()?.types.fixed ?? '' },
  ]);
  protected readonly scopeOptions = computed<readonly SelectOption<DiscountScope>[]>(() => [
    { value: DiscountScope.All, label: this.optionsTexts()?.scopes.all ?? '' },
    { value: DiscountScope.Services, label: this.optionsTexts()?.scopes.services ?? '' },
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
    const start = new VoucherAddItemRequest();
    const draft = this.draft();
    return (
      this.open() &&
      (draft.voucherName !== null ||
        draft.validityPeriods.length > 0 ||
        draft.discountType !== start.discountType ||
        draft.discountValue !== start.discountValue ||
        draft.discountScope !== start.discountScope ||
        draft.maxUseQuantity !== start.maxUseQuantity ||
        draft.isEnabled !== start.isEnabled)
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
  }

  protected setAllDays(period: FieldTree<VoucherValidityPeriod>, isAll: boolean): void {
    period.daysOfWeek().value.set(isAll ? DAYS_OF_WEEK.map(({ number }) => number) : []);
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
    const idProperty = this.auth.user()?.propertyId;
    if (this.isLoading() || !idProperty) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.vouchers.addVoucherItem(idProperty, this.draft()));
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
