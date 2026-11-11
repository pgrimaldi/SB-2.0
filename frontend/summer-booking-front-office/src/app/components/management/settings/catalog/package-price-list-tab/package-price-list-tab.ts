import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { PriceCalculationMethod } from '../../../../../entities/enums/price-calculation-method';
import { PriceMonthlySetting } from '../../../../../entities/enums/price-monthly-setting';
import { PriceWeekendSetting } from '../../../../../entities/enums/price-weekend-setting';
import { PackageAddItemRequest } from '../../../../../entities/settings/catalog/package-add-item-request';
import { PackagePeriodPrice } from '../../../../../entities/settings/catalog/package-period-price';
import { PackageSectorPrice } from '../../../../../entities/settings/catalog/package-sector-price';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { InfoPopup } from '../../../../shared/ui/dialogs/info-popup/info-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';

/** The fields of a row that hold a price. */
type PriceField<T> = { [K in keyof T]: T[K] extends number | null ? K : never }[keyof T];

interface PriceColumn<T> {
  field: PriceField<T>;
  /** Translation key of the header. */
  label: string;
}

const KEY = 'management.settings.catalog.packages.form.price_list';

/** The texts of the choices of the dropdowns, in the translation group of the tab. */
interface ChoiceTexts {
  calculation_methods: {
    proportional: string;
    daily: string;
    logarithmic: string;
    daily_with_weekend: string;
  };
  monthly_settings: {
    effective_month_duration: string;
    thirty_days: string;
  };
  weekend_settings: {
    saturday_sunday: string;
    sunday_only: string;
  };
}

const CALCULATION_METHOD_KEYS: Readonly<
  Record<PriceCalculationMethod, keyof ChoiceTexts['calculation_methods']>
> = {
  [PriceCalculationMethod.Proportional]: 'proportional',
  [PriceCalculationMethod.Daily]: 'daily',
  [PriceCalculationMethod.Logarithmic]: 'logarithmic',
  [PriceCalculationMethod.DailyWithWeekend]: 'daily_with_weekend',
};

const MONTHLY_SETTING_KEYS: Readonly<
  Record<PriceMonthlySetting, keyof ChoiceTexts['monthly_settings']>
> = {
  [PriceMonthlySetting.EffectiveMonthDuration]: 'effective_month_duration',
  [PriceMonthlySetting.Fixed30Days]: 'thirty_days',
};

const WEEKEND_SETTING_KEYS: Readonly<
  Record<PriceWeekendSetting, keyof ChoiceTexts['weekend_settings']>
> = {
  [PriceWeekendSetting.SaturdaySunday]: 'saturday_sunday',
  [PriceWeekendSetting.SundayOnly]: 'sunday_only',
};

@Component({
  selector: 'app-package-price-list-tab',
  imports: [
    Button,
    Checkbox,
    ExpansionPanel,
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

  private readonly translateService = inject(TranslateService);

  protected readonly ButtonSize = ButtonSize;
  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [before the title, arrow]. */
  protected readonly sectionIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
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

  private readonly choiceTexts = toSignal(
    this.translateService.stream(KEY) as Observable<ChoiceTexts>,
    { initialValue: null },
  );
  protected readonly calculationMethods = computed<readonly SelectOption<PriceCalculationMethod>[]>(
    () =>
      Object.values(PriceCalculationMethod).map((value) => ({
        value,
        label: this.choiceTexts()?.calculation_methods[CALCULATION_METHOD_KEYS[value]] ?? '',
      })),
  );
  protected readonly monthlySettings = computed<readonly SelectOption<PriceMonthlySetting>[]>(() =>
    Object.values(PriceMonthlySetting).map((value) => ({
      value,
      label: this.choiceTexts()?.monthly_settings[MONTHLY_SETTING_KEYS[value]] ?? '',
    })),
  );
  protected readonly weekendSettings = computed<readonly SelectOption<PriceWeekendSetting>[]>(() =>
    Object.values(PriceWeekendSetting).map((value) => ({
      value,
      label: this.choiceTexts()?.weekend_settings[WEEKEND_SETTING_KEYS[value]] ?? '',
    })),
  );

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
