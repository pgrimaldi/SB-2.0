import { DOCUMENT, NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  contentChild,
  inject,
  input,
  signal,
} from '@angular/core';
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
import { ServicesPeriodContent } from './services-period-content';

/** The phone layout of the scss: the two days of an interval on one line, with the month short. */
const PHONE_QUERY = '(width < 48rem)';

/** "Abilita variazioni di prezzo per periodo": when on, a price for each period of the season. */
@Component({
  selector: 'app-services-period-prices',
  imports: [
    ExpansionPanel,
    FilledNumberField,
    FormField,
    IconButton,
    InfoPopup,
    NgTemplateOutlet,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './services-period-prices.html',
  styleUrl: './services-period-prices.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesPeriodPrices implements OnDestroy {
  /** The fields of the form of the panel, which validates and saves them. */
  readonly isPeriodPricingEnabled = input.required<FieldTree<boolean>>();
  /** A price for each period; without it each period shows the `appServicesPeriodContent` template. */
  readonly periodPrices = input<FieldTree<ServicePeriodPrice[]>>();
  /** The periods of the current season, each with its own price when the toggle is on. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);
  protected readonly content = contentChild(ServicesPeriodContent);

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
  private readonly phoneQuery = inject(DOCUMENT).defaultView?.matchMedia?.(PHONE_QUERY);
  private readonly isPhone = signal(this.phoneQuery?.matches ?? false);

  protected readonly priceByPeriod = computed(
    () =>
      new Map(
        [...(this.periodPrices() ?? [])].map((periodPrice) => [
          periodPrice.idPeriod().value(),
          periodPrice,
        ]),
      ),
  );
  private readonly dayFormat = computed(
    () =>
      new Intl.DateTimeFormat(this.dateLanguage.locale(), {
        day: '2-digit',
        month: this.isPhone() ? 'short' : 'long',
        year: 'numeric',
        timeZone: 'UTC',
      }),
  );

  constructor() {
    this.phoneQuery?.addEventListener('change', this.phoneChanged);
  }

  protected day(date: string | null): string {
    return date ? this.dayFormat().format(new Date(date)) : '';
  }

  /** A phone rotated, or a window resized, while the panel is open. */
  private readonly phoneChanged = (event: MediaQueryListEvent): void => {
    this.isPhone.set(event.matches);
  };

  ngOnDestroy(): void {
    this.phoneQuery?.removeEventListener('change', this.phoneChanged);
  }
}
