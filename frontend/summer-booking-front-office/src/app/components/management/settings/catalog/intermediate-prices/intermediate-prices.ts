import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import {
  MAX_INTERMEDIATE_PRICE_DAY_QUANTITY,
  PackageIntermediatePrice,
} from '../../../../../entities/settings/catalog/package-intermediate-price';
import { PackagePeriodPrice } from '../../../../../entities/settings/catalog/package-period-price';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { ConfirmPopup } from '../../../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';

const KEY = 'management.settings.catalog.price_list.intermediate_prices';

/** The fields of a row of Prezzi per periodo that hold a price. */
type PeriodPriceField = {
  [K in keyof PackagePeriodPrice]: PackagePeriodPrice[K] extends number | null ? K : never;
}[keyof PackagePeriodPrice];

/** The numbers of days whose price is also a column of Prezzi per periodo. */
const PERIOD_PRICE_FIELDS = new Map<number, PeriodPriceField>([
  [7, 'sevenDaysPrice'],
  [14, 'fourteenDaysPrice'],
  [30, 'monthlyPrice'],
]);

/** A choice of the dropdown: Fila 1 or Altre file, in a period. */
interface IntermediatePriceChoice {
  idPeriod: string | null;
  isFirstRow: boolean;
  label: string;
}

@Component({
  selector: 'app-intermediate-prices',
  imports: [Checkbox, ConfirmPopup, ExpansionPanel, FilledNumberField, FilledSelect, TranslatePipe],
  templateUrl: './intermediate-prices.html',
  styleUrl: './intermediate-prices.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IntermediatePrices {
  readonly isIncluded = input.required<FieldTree<boolean>>();
  /** Only the prices written, in order (`byIntermediatePrice`). */
  readonly prices = input.required<FieldTree<PackageIntermediatePrice[]>>();
  /** The rows of Prezzi per periodo: the prices of 7, 14 and 30 days go in their columns too. */
  readonly periodPrices = input.required<FieldTree<PackagePeriodPrice[]>>();
  /** The sector of Fila 1, the first of the property: Altre file are all the others. */
  readonly idFirstSector = input<string | null>(null);
  /** The periods of the current season: Fila 1 and Altre file in each of them. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);
  /** The name of the package or of the article, in every row. */
  readonly name = input('');
  /** The checkbox can be used (e.g. only once the package has a name). */
  readonly isEnabled = input(true);

  private readonly translateService = inject(TranslateService);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly days = Array.from(
    { length: MAX_INTERMEDIATE_PRICE_DAY_QUANTITY },
    (_, index) => index + 1,
  );
  /** [X]. */
  protected readonly popupIcons = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [before the title, arrow]. */
  protected readonly sectionIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly isIncludedValue = computed(() => this.isIncluded()().value());
  /** What the checkbox shows: unticked while the popup asks, its value again after the answer. */
  protected readonly isChecked = linkedSignal(() => this.isIncludedValue());
  protected readonly isCheckboxLocked = computed(
    () => this.isIncluded()().readonly() || this.isIncluded()().disabled() || !this.isEnabled(),
  );
  /** The package or item is only to look at (or being saved). */
  protected readonly isLocked = computed(
    () => this.prices()().readonly() || this.prices()().disabled(),
  );
  protected readonly isAskingOff = signal(false);
  /** Closed at every change of the checkbox: the section opens only while it is ticked. */
  protected readonly isExpanded = linkedSignal({
    source: this.isIncludedValue,
    computation: () => false,
  });
  /** The choice whose prices are shown; none again at every change of the checkbox. */
  protected readonly selectedRow = linkedSignal<boolean, string | null>({
    source: this.isIncludedValue,
    computation: () => null,
  });
  /** The texts follow the language: the labels are read again with the group. */
  private readonly texts = toSignal(this.translateService.stream(KEY), { initialValue: null });
  private readonly choices = computed<readonly IntermediatePriceChoice[]>(() => {
    this.texts();
    return [true, false].flatMap((isFirstRow) =>
      this.periods().map(({ idPeriod, periodName }) => ({
        idPeriod,
        isFirstRow,
        label: this.translateService.instant(`${KEY}.${isFirstRow ? 'first_row' : 'other_rows'}`, {
          period: periodName ?? '',
        }) as string,
      })),
    );
  });
  protected readonly options = computed<readonly SelectOption[]>(() =>
    this.choices().map(({ label }, index) => ({ value: String(index), label })),
  );
  protected readonly choice = computed(() => {
    const row = this.selectedRow();
    return row === null ? null : (this.choices()[Number(row)] ?? null);
  });
  /** The prices of the choice, by number of days. */
  protected readonly shownPrices = computed(() => {
    const choice = this.choice();
    return new Map(
      choice
        ? this.prices()()
            .value()
            .filter((row) => isOfChoice(row, choice))
            .map(({ dayQuantity, price }) => [dayQuantity, price])
        : [],
    );
  });

  /** Ticking turns the option on at once; unticking asks first, as it deletes the prices. */
  protected toggle(checked: boolean): void {
    this.isChecked.set(checked);
    if (checked) {
      this.isIncluded()().value.set(true);
      this.isIncluded()().markAsTouched();
    } else {
      this.isAskingOff.set(true);
    }
  }

  protected confirmOff(): void {
    this.isIncluded()().value.set(false);
    this.isIncluded()().markAsTouched();
    this.prices()().value.set([]);
    this.prices()().markAsTouched();
    this.isAskingOff.set(false);
  }

  protected closeOff(isOpen: boolean): void {
    if (!isOpen) {
      this.isAskingOff.set(false);
      this.isChecked.set(this.isIncludedValue());
    }
  }

  /** An empty price is not kept: the list has only the prices written. */
  protected setPrice(dayQuantity: number, price: number | null): void {
    const choice = this.choice();
    if (!choice) {
      return;
    }
    this.prices()().value.update((rows) => {
      const others = rows.filter(
        (row) => !isOfChoice(row, choice) || row.dayQuantity !== dayQuantity,
      );
      return price === null
        ? others
        : [
            ...others,
            Object.assign(new PackageIntermediatePrice(), {
              idPeriod: choice.idPeriod,
              isFirstRow: choice.isFirstRow,
              dayQuantity,
              price,
            }),
          ].sort(byIntermediatePrice);
    });
    this.prices()().markAsTouched();
    this.setPeriodPrice(choice, dayQuantity, price);
  }

  /** The same price in the column of its days, in the rows of Fila 1 or of Altre file in that period. */
  private setPeriodPrice(
    choice: IntermediatePriceChoice,
    dayQuantity: number,
    price: number | null,
  ): void {
    const field = PERIOD_PRICE_FIELDS.get(dayQuantity);
    if (!field) {
      return;
    }
    const rows = this.periodPrices();
    rows()
      .value()
      .forEach(({ idSector, idPeriod }, index) => {
        if (
          idSector !== null &&
          idPeriod === choice.idPeriod &&
          (idSector === this.idFirstSector()) === choice.isFirstRow
        ) {
          rows[index][field]().value.set(price);
        }
      });
  }
}

/** Fila 1 before Altre file, then by period, then by number of days: the same list, the same order. */
export function byIntermediatePrice(
  first: PackageIntermediatePrice,
  second: PackageIntermediatePrice,
): number {
  return (
    Number(second.isFirstRow) - Number(first.isFirstRow) ||
    (first.idPeriod ?? '').localeCompare(second.idPeriod ?? '') ||
    (first.dayQuantity ?? 0) - (second.dayQuantity ?? 0)
  );
}

function isOfChoice(row: PackageIntermediatePrice, choice: IntermediatePriceChoice): boolean {
  return row.idPeriod === choice.idPeriod && row.isFirstRow === choice.isFirstRow;
}
