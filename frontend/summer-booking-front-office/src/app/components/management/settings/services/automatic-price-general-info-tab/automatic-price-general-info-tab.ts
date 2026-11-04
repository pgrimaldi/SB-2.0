import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { BarServiceIcon } from '../../../../../entities/enums/bar-service-icon';
import { StandardServiceIcon } from '../../../../../entities/enums/standard-service-icon';
import { AutomaticPriceAddItemRequest } from '../../../../../entities/settings/services/automatic-price-add-item-request';
import { Language } from '../../../../../entities/shared/language';
import { ServicesImages, ServicesImagesIcon } from '../shared/services-images/services-images';
import { ServicesTranslations } from '../shared/services-translations/services-translations';

@Component({
  selector: 'app-automatic-price-general-info-tab',
  imports: [ServicesImages, ServicesTranslations],
  templateUrl: './automatic-price-general-info-tab.html',
  styleUrl: './automatic-price-general-info-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomaticPriceGeneralInfoTab {
  readonly form = input.required<FieldTree<AutomaticPriceAddItemRequest>>();
  /** Language of the main name and description: the user's; the others are translations. */
  readonly mainLanguage = input.required<Language>();
  /** Only to look at: the buttons that change the data (icons, image) are off. */
  readonly isReadonly = input(false);
  readonly imagePreviewUrl = input<string | null>(null);
  readonly iconChosen = output<BarServiceIcon | StandardServiceIcon>();
  readonly imageChosen = output<File>();

  /** The icons of the bar services, then those of the other services, as in the grid of the mockup. */
  protected readonly icons: readonly ServicesImagesIcon<BarServiceIcon | StandardServiceIcon>[] = [
    ...Object.values(BarServiceIcon).map((value) => ({
      value,
      url: `/assets/images/Services_Bar${value}.png`,
    })),
    ...Object.values(StandardServiceIcon).map((value) => ({
      value,
      url: `/assets/images/Services_Standard${value}.png`,
    })),
  ];
}
