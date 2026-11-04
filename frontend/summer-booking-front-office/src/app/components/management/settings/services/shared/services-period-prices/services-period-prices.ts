import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { DateLanguageBehaviour } from '../../../../../../behaviours/i18n/date-language.behaviour';
import { ValidationTextBehaviour } from '../../../../../../behaviours/validation/validation-text.behaviour';
import { SeasonPeriodItem } from '../../../../../../entities/settings/seasons/season-period-item';
import { ServicePeriodPrice } from '../../../../../../entities/settings/services/service-period-price';
import { IconButton } from '../../../../../shared/ui/buttons/icon-button/icon-button';
import { InfoPopup } from '../../../../../shared/ui/dialogs/info-popup/info-popup';
import { ExpansionPanel } from '../../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { FilledNumberField } from '../../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { Toggle } from '../../../../../shared/ui/toggles/toggle/toggle';

/** "Abilita variazioni di prezzo per periodo": when on, a price for each period of the season. */
@Component({
  selector: 'app-services-period-prices',
  imports: [
    ExpansionPanel,
    FilledNumberField,
    FormField,
    IconButton,
    InfoPopup,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './services-period-prices.html',
  styleUrl: './services-period-prices.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesPeriodPrices {
  /** The fields of the form of the panel, which validates and saves them. */
  readonly isPeriodPricingEnabled = input.required<FieldTree<boolean>>();
  readonly periodPrices = input.required<FieldTree<ServicePeriodPrice[]>>();
  /** The periods of the current season, each with its own price when the toggle is on. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);

  private readonly dateLanguage = inject(DateLanguageBehaviour);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [X, before the title]. */
  protected readonly infoPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_InfoTitle.svg',
  ] as const;
  /** [before the title, arrow]. */
  protected readonly periodIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly showingInfo = signal(false);

  protected readonly priceByPeriod = computed(
    () =>
      new Map(
        [...this.periodPrices()].map((periodPrice) => [
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
}
