import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField, form } from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { VoucherAddItemRequest } from '../../../../../entities/settings/vouchers/voucher-add-item-request';
import { VoucherTimeSlot } from '../../../../../entities/settings/vouchers/voucher-time-slot';
import { VoucherValidityPeriod } from '../../../../../entities/settings/vouchers/voucher-validity-period';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTimeField } from '../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';

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

@Component({
  selector: 'app-voucher-add-item-panel',
  imports: [
    Button,
    Checkbox,
    FilledDateField,
    FilledMultiSelect,
    FilledTextField,
    FilledTimeField,
    FormField,
    TranslatePipe,
  ],
  templateUrl: './voucher-add-item-panel.html',
  styleUrl: './voucher-add-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class VoucherAddItemPanel extends BasePopup implements UnsavedChanges, OnDestroy {
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly translateService = inject(TranslateService);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  private readonly draft = signal(new VoucherAddItemRequest());
  protected readonly draftForm = form(this.draft);
  protected readonly removePeriodIcon = '/assets/images/Vouchers_Close.svg';
  protected readonly addPeriodIcon = ['/assets/images/Vouchers_Add.svg'] as const;
  protected readonly calendarIcon = ['/assets/images/Vouchers_Calendar.svg'] as const;
  protected readonly clockIcon = ['/assets/images/Vouchers_Clock.svg'] as const;
  protected readonly deleteTimeSlotIcon = '/assets/images/Vouchers_DeleteTimeSlot.svg';
  protected readonly addTimeSlotIcon = ['/assets/images/Vouchers_AddTimeSlot.svg'] as const;
  private readonly dayNames = toSignal(
    this.translateService.stream('management.settings.vouchers.form.periods.days') as Observable<
      Record<DayKey, string>
    >,
    { initialValue: null },
  );
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
    const { voucherName, validityPeriods } = this.draft();
    return this.open() && (voucherName !== null || validityPeriods.length > 0);
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

  protected async leave(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.close();
    }
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
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
