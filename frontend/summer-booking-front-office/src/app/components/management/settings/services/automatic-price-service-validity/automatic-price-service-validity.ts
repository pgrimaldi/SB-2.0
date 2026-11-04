import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { AutomaticPriceServices } from '../../../../../entities/settings/services/automatic-price-services';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { InfoPopup } from '../../../../shared/ui/dialogs/info-popup/info-popup';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

interface ServiceValidityTexts {
  bar_services: string;
  standard_services: string;
  hourly_services: string;
}

/** One kind of services: its panel, its services and the ids chosen among them. */
interface ServiceGroup {
  title: string;
  services: readonly ComboboxItem[];
  field: FieldTree<string[]>;
}

/** "Validità sui servizi" of an automatic price: the services it applies to, by kind. */
@Component({
  selector: 'app-automatic-price-service-validity',
  imports: [
    Checkbox,
    ExpansionPanel,
    FormField,
    IconButton,
    InfoPopup,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './automatic-price-service-validity.html',
  styleUrl: './automatic-price-service-validity.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomaticPriceServiceValidity {
  /** The fields of the form of the panel, which validates and saves them. */
  readonly idBarServices = input.required<FieldTree<string[]>>();
  readonly idStandardServices = input.required<FieldTree<string[]>>();
  readonly idHourlyServices = input.required<FieldTree<string[]>>();
  readonly isPricePerServiceEnabled = input.required<FieldTree<boolean>>();
  /** The services of the property, by kind; null while they are not read. */
  readonly services = input<AutomaticPriceServices | null>(null);

  private readonly translateService = inject(TranslateService);

  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [X, before the title]. */
  protected readonly infoPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_InfoTitle.svg',
  ] as const;
  /** [before the title, arrow]. */
  protected readonly groupIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly showingInfo = signal(false);
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.services.form.validity',
    ) as Observable<ServiceValidityTexts>,
    { initialValue: null },
  );
  protected readonly groups = computed<readonly ServiceGroup[]>(() => [
    {
      title: this.texts()?.bar_services ?? '',
      services: this.services()?.barServices ?? [],
      field: this.idBarServices(),
    },
    {
      title: this.texts()?.standard_services ?? '',
      services: this.services()?.standardServices ?? [],
      field: this.idStandardServices(),
    },
    {
      title: this.texts()?.hourly_services ?? '',
      services: this.services()?.hourlyServices ?? [],
      field: this.idHourlyServices(),
    },
  ]);

  protected isAllChosen(group: ServiceGroup): boolean {
    const chosen = group.field().value();
    return group.services.length > 0 && group.services.every(({ id }) => chosen.includes(id));
  }

  /** The services are not form fields: they follow the state of their list. */
  protected isLocked(group: ServiceGroup): boolean {
    return group.field().disabled() || group.field().readonly();
  }

  protected chooseAll(group: ServiceGroup, isChosen: boolean): void {
    group.field().value.set(isChosen ? group.services.map(({ id }) => id) : []);
  }

  protected choose(group: ServiceGroup, id: string, isChosen: boolean): void {
    group
      .field()
      .value.update((chosen) =>
        isChosen ? [...chosen, id] : chosen.filter((chosenId) => chosenId !== id),
      );
  }
}
