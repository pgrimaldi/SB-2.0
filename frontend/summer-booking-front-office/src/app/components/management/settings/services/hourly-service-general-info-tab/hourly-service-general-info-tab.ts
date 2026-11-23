import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { StandardServiceIcon } from '../../../../../entities/enums/standard-service-icon';
import { HourlyServiceDraft } from '../../../../../entities/settings/services/hourly-service-draft';
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
  readonly form = input.required<FieldTree<HourlyServiceDraft>>();
  /** Language of the main name and description: the user's; the others are translations. */
  readonly mainLanguage = input.required<Language>();
  /** Only to look at: the buttons that change the data (icons, image) are off. */
  readonly isReadonly = input(false);
  readonly imagePreviewUrl = input<string | null>(null);
  readonly imageError = input<string | null>(null);
  readonly iconChosen = output<StandardServiceIcon>();
  readonly imageChosen = output<File>();
  readonly imageRemoveRequest = output<void>();

  protected readonly validationText = inject(ValidationTextBehaviour);

  /** Every icon a service can have, as shown in the grid. */
  protected readonly icons: readonly ServicesImagesIcon<StandardServiceIcon>[] = Object.values(
    StandardServiceIcon,
  ).map((value) => ({ value, url: `/assets/images/Services_Standard${value}.png` }));
}
