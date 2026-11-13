import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  model,
  signal,
} from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import {
  DATE_FORMATS,
  DateLanguageBehaviour,
} from '../../../../../behaviours/i18n/date-language.behaviour';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { PackageReducedSeasonalPrice } from '../../../../../entities/settings/catalog/package-reduced-seasonal-price';
import { PackageSectorPrice } from '../../../../../entities/settings/catalog/package-sector-price';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { ConfirmPopup } from '../../../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';

/** The fields of a sector row that hold a price. */
type SectorPriceField = {
  [K in keyof PackageSectorPrice]: PackageSectorPrice[K] extends number | null ? K : never;
}[keyof PackageSectorPrice];

const PRICE_LIST_KEY = 'management.settings.catalog.price_list';

@Component({
  selector: 'app-package-reduced-price',
  imports: [
    Button,
    ConfirmPopup,
    ExpansionPanel,
    FilledDateField,
    FilledNumberField,
    FilledTextField,
    FormField,
    IconButton,
    TranslatePipe,
  ],
  templateUrl: './package-reduced-price.html',
  styleUrl: './package-reduced-price.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageReducedPrice {
  readonly reducedSeasonalPrices = input.required<FieldTree<PackageReducedSeasonalPrice[]>>();
  /** The fields of the box, kept by the panel so that they outlive the tab. */
  readonly draftForm = input.required<FieldTree<PackageReducedSeasonalPrice>>();
  /**
   * Where the box writes: the position of a row to change it, the end of the list to add one; null
   * while closed.
   */
  readonly editedIndex = model.required<number | null>();
  /** The sectors of the property: a row each in every section, as in Prezzi stagionali. */
  readonly sectors = input<readonly ComboboxItem[]>([]);

  private readonly dateLanguage = inject(DateLanguageBehaviour);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_AddBlue.svg'] as const;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly calendarIcon = ['/assets/images/GeneralIcon_Calendar.svg'] as const;
  protected readonly editIcon = ['/assets/images/Manager_Table_Edit.svg'] as const;
  protected readonly deleteIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;
  /** [before the title, arrow]. */
  protected readonly sectionIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly seasonalColumns: readonly { field: SectorPriceField; label: string }[] = [
    { field: 'seasonalHalfDayPrice', label: `${PRICE_LIST_KEY}.seasonal_half_day` },
    { field: 'seasonalPrice', label: `${PRICE_LIST_KEY}.seasonal` },
  ];
  private readonly rows = computed(() => this.reducedSeasonalPrices()().value());
  private readonly sectorNames = computed(
    () => new Map(this.sectors().map(({ id, value }) => [id, value])),
  );
  protected readonly isOpen = computed(() => this.editedIndex() !== null);
  /** The reduced price whose bin was pressed, until the popup is answered. */
  protected readonly deletingIndex = signal<number | null>(null);
  protected readonly isAdding = computed(() => (this.editedIndex() ?? 0) >= this.rows().length);
  /** An error of the whole list (e.g. from the backend), under it. */
  protected readonly error = computed(() => this.validationText.text(this.reducedSeasonalPrices()));
  /** The package is only to look at (or being saved). */
  protected readonly isLocked = computed(
    () => this.reducedSeasonalPrices()().readonly() || this.reducedSeasonalPrices()().disabled(),
  );
  /** The days of the other reduced prices: they cannot be chosen in the box. */
  protected readonly otherPeriods = computed(() =>
    this.rows().filter((_, index) => index !== this.editedIndex()),
  );
  /** Al stops the day before the next reduced price. */
  protected readonly dateToMax = computed(() =>
    lastFreeDay(this.draftForm().dateFrom().value(), this.otherPeriods()),
  );
  protected readonly isOverlapping = computed(() =>
    this.draftForm()()
      .errors()
      .some(({ kind }) => kind === 'overlap'),
  );
  private readonly dayFormat = computed(
    () =>
      new Intl.DateTimeFormat(this.dateLanguage.locale(), {
        ...DATE_FORMATS.display.dateInput,
        timeZone: 'UTC',
      }),
  );

  protected day(date: string | null): string {
    return date ? this.dayFormat().format(new Date(date)) : '';
  }

  protected sectorName(idSector: string | null): string {
    return (idSector && this.sectorNames().get(idSector)) ?? '';
  }

  /** Prices of sectors the property no longer shows stay in the data, without a row. */
  protected isShownSector(idSector: string | null): boolean {
    return idSector !== null && this.sectorNames().has(idSector);
  }

  protected add(): void {
    this.openBox(this.rows().length, new PackageReducedSeasonalPrice());
  }

  protected edit(index: number, price: PackageReducedSeasonalPrice): void {
    this.openBox(index, price);
  }

  protected close(): void {
    this.editedIndex.set(null);
  }

  protected save(): void {
    const editedIndex = this.editedIndex();
    if (editedIndex === null) {
      return;
    }
    const price = { ...this.draftForm()().value() };
    if (this.isAdding()) {
      price.sectorPrices = this.sectors().map(({ id }) =>
        Object.assign(new PackageSectorPrice(), { idSector: id }),
      );
    }
    this.reducedSeasonalPrices()().value.update((rows) =>
      (editedIndex < rows.length
        ? rows.map((row, index) => (index === editedIndex ? price : row))
        : [...rows, price]
      ).sort(byDateFrom),
    );
    this.editedIndex.set(null);
  }

  protected confirmDelete(): void {
    const deletedIndex = this.deletingIndex();
    if (deletedIndex === null) {
      return;
    }
    this.reducedSeasonalPrices()().value.update((rows) =>
      rows.filter((_, index) => index !== deletedIndex),
    );
    this.deletingIndex.set(null);
  }

  protected closeDelete(isOpen: boolean): void {
    if (!isOpen) {
      this.deletingIndex.set(null);
    }
  }

  private openBox(index: number, price: PackageReducedSeasonalPrice): void {
    this.draftForm()().value.set(structuredClone(price));
    this.draftForm()().reset();
    this.editedIndex.set(index);
  }
}

/**
 * The day before the first of `periods` that starts after `day`: days at midnight UTC as ISO strings,
 * compared by day; without `day` or such a period, no limit.
 */
function lastFreeDay(
  day: string | null,
  periods: readonly PackageReducedSeasonalPrice[],
): string | null {
  if (!day) {
    return null;
  }
  const next = periods.reduce<string | null>(
    (first, { dateFrom }) =>
      dateFrom &&
      dateFrom.slice(0, 10) > day.slice(0, 10) &&
      (!first || dateFrom.slice(0, 10) < first.slice(0, 10))
        ? dateFrom
        : first,
    null,
  );
  return next ? new Date(Date.parse(next.slice(0, 10)) - 86_400_000).toISOString() : null;
}

/** By first day: the days of two reduced prices never overlap. */
function byDateFrom(
  first: PackageReducedSeasonalPrice,
  second: PackageReducedSeasonalPrice,
): number {
  return (first.dateFrom ?? '').localeCompare(second.dateFrom ?? '');
}
