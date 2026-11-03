import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { DateLanguageBehaviour } from '../../../../../behaviours/i18n/date-language.behaviour';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { ServicePrices } from '../../../../../entities/settings/services/service-prices';
import { ServiceVariant } from '../../../../../entities/settings/services/service-variant';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { InfoPopup } from '../../../../shared/ui/dialogs/info-popup/info-popup';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

@Component({
  selector: 'app-service-prices-tab',
  imports: [
    Button,
    ExpansionPanel,
    FilledMultiSelect,
    FilledNumberField,
    FilledTextField,
    FormField,
    FormRow,
    FormRowLabel,
    IconButton,
    InfoPopup,
    SectionTitle,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './service-prices-tab.html',
  styleUrl: './service-prices-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicePricesTab {
  readonly form = input.required<FieldTree<ServicePrices>>();
  /** The periods of the current season, each with its own price when the toggle is on. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);

  private readonly dateLanguage = inject(DateLanguageBehaviour);

  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_Add.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;
  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [X, before the title]. */
  protected readonly infoPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_InfoTitle.svg',
  ] as const;
  /** [before the title, arrow]. */
  protected readonly periodIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  /** Example rows until an API gives the rows and sectors of the property. */
  protected readonly sectorOptions: readonly SelectOption[] = [
    { value: '7c0e2a51-3b9f-4d6e-8a21-5f4b9c1d0e01', label: 'Fila 1' },
    { value: '7c0e2a51-3b9f-4d6e-8a21-5f4b9c1d0e02', label: 'Fila 2' },
    { value: '7c0e2a51-3b9f-4d6e-8a21-5f4b9c1d0e03', label: 'Fila 3' },
  ];
  protected readonly showingPeriodPricingInfo = signal(false);

  protected readonly periodPrices = computed(
    () =>
      new Map(
        [...this.form().periodPrices].map((periodPrice) => [
          periodPrice.idPeriod().value(),
          periodPrice,
        ]),
      ),
  );
  private readonly dayFormat = computed(
    () =>
      new Intl.DateTimeFormat(this.dateLanguage.locale(), {
        day: '2-digit',
        month: 'long',
        year: 'numeric',
        timeZone: 'UTC',
      }),
  );

  protected day(date: string | null): string {
    return date ? this.dayFormat().format(new Date(date)) : '';
  }

  protected addVariant(): void {
    this.form()
      .variants()
      .value.update((variants) => [...variants, new ServiceVariant()]);
  }

  protected removeVariant(index: number): void {
    this.form()
      .variants()
      .value.update((variants) => variants.filter((_, variantIndex) => variantIndex !== index));
  }
}
