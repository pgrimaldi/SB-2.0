import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { PackageAddItemRequest } from '../../../../../entities/settings/catalog/package-add-item-request';
import { PackagePeriodPrice } from '../../../../../entities/settings/catalog/package-period-price';
import { PackageSectorPrice } from '../../../../../entities/settings/catalog/package-sector-price';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { InfoPopup } from '../../../../shared/ui/dialogs/info-popup/info-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';

/** The fields of a row that hold a price. */
type PriceField<T> = { [K in keyof T]: T[K] extends number | null ? K : never }[keyof T];

interface PriceColumn<T> {
  field: PriceField<T>;
  /** Translation key of the header. */
  label: string;
}

const KEY = 'management.settings.catalog.packages.form.price_list';

@Component({
  selector: 'app-package-price-list-tab',
  imports: [
    Button,
    Checkbox,
    FilledNumberField,
    FilledSelect,
    FormField,
    IconButton,
    InfoPopup,
    TranslatePipe,
  ],
  templateUrl: './package-price-list-tab.html',
  styleUrl: './package-price-list-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackagePriceListTab {
  readonly form = input.required<FieldTree<PackageAddItemRequest>>();
  /** The sectors of the property: a row each. */
  readonly sectors = input<readonly ComboboxItem[]>([]);
  /** The periods of the current season: a row each, for every sector. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);
  /** Whether the rows have been read: until then no "no rows" message. */
  readonly isLoaded = input(false);

  protected readonly ButtonSize = ButtonSize;
  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [X, before the title]. */
  protected readonly infoPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_InfoTitle.svg',
  ] as const;
  protected readonly showingCalculationInfo = signal(false);
  protected readonly showingCumulativeInfo = signal(false);

  protected readonly seasonalColumns: readonly PriceColumn<PackageSectorPrice>[] = [
    { field: 'seasonalHalfDayPrice', label: `${KEY}.seasonal_half_day` },
    { field: 'seasonalPrice', label: `${KEY}.seasonal` },
  ];
  protected readonly periodColumns: readonly PriceColumn<PackagePeriodPrice>[] = [
    { field: 'halfDayPrice', label: `${KEY}.half_day` },
    { field: 'dailyPrice', label: `${KEY}.daily` },
    { field: 'weekendPrice', label: `${KEY}.weekend` },
    { field: 'halfDayWeekendPrice', label: `${KEY}.half_day_weekend` },
    { field: 'halfDaySevenDaysPrice', label: `${KEY}.half_day_seven_days` },
    { field: 'sevenDaysPrice', label: `${KEY}.seven_days` },
    { field: 'halfDayFourteenDaysPrice', label: `${KEY}.half_day_fourteen_days` },
    { field: 'fourteenDaysPrice', label: `${KEY}.fourteen_days` },
    { field: 'halfDayMonthlyPrice', label: `${KEY}.half_day_monthly` },
    { field: 'monthlyPrice', label: `${KEY}.monthly` },
  ];

  private readonly sectorNames = computed(
    () => new Map(this.sectors().map(({ id, value }) => [id, value])),
  );
  private readonly periodNames = computed(
    () => new Map(this.periods().map(({ idPeriod, periodName }) => [idPeriod, periodName])),
  );

  protected sectorName(idSector: string | null): string {
    return (idSector && this.sectorNames().get(idSector)) ?? '';
  }

  protected periodName(idPeriod: string | null): string {
    return (idPeriod && this.periodNames().get(idPeriod)) ?? '';
  }
}
