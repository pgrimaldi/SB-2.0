import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { StandardServiceIcon } from '../../../../../entities/enums/standard-service-icon';
import { HourlyServiceAddItemRequest } from '../../../../../entities/settings/services/hourly-service-add-item-request';
import { Language } from '../../../../../entities/shared/language';
import { ServicesHighlighted } from '../shared/services-highlighted/services-highlighted';
import { ServicesImages, ServicesImagesIcon } from '../shared/services-images/services-images';
import { ServicesTranslations } from '../shared/services-translations/services-translations';

@Component({
  selector: 'app-hourly-service-general-info-tab',
  imports: [ServicesHighlighted, ServicesImages, ServicesTranslations],
  templateUrl: './hourly-service-general-info-tab.html',
  styleUrl: './hourly-service-general-info-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HourlyServiceGeneralInfoTab {
  readonly form = input.required<FieldTree<HourlyServiceAddItemRequest>>();
  /** Language of the main name and description: the user's; the others are translations. */
  readonly mainLanguage = input.required<Language>();
  /** Only to look at: the buttons that change the data (icons, image) are off. */
  readonly isReadonly = input(false);
  readonly imagePreviewUrl = input<string | null>(null);
  readonly iconChosen = output<StandardServiceIcon>();
  readonly imageChosen = output<File>();

  /** Every icon a service can have, as shown in the grid. */
  protected readonly icons: readonly ServicesImagesIcon<StandardServiceIcon>[] = Object.values(
    StandardServiceIcon,
  ).map((value) => ({ value, url: `/assets/images/Services_Standard${value}.png` }));
}
