import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { TableServiceAddItemRequest } from '../../../../../entities/settings/services/table-service-add-item-request';
import { Language } from '../../../../../entities/shared/language';
import { ServicesHighlighted } from '../shared/services-highlighted/services-highlighted';
import { ServicesTranslations } from '../shared/services-translations/services-translations';

@Component({
  selector: 'app-table-service-general-info-tab',
  imports: [ServicesHighlighted, ServicesTranslations],
  templateUrl: './table-service-general-info-tab.html',
  styleUrl: './table-service-general-info-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableServiceGeneralInfoTab {
  readonly form = input.required<FieldTree<TableServiceAddItemRequest>>();
  /** Language of the main name and description: the user's; the others are translations. */
  readonly mainLanguage = input.required<Language>();
}
