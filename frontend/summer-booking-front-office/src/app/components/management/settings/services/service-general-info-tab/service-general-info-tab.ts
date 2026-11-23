import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { BarServiceCategory } from '../../../../../entities/enums/bar-service-category';
import { BarServiceIcon } from '../../../../../entities/enums/bar-service-icon';
import { BarServiceAddItemRequest } from '../../../../../entities/settings/services/bar-service-add-item-request';
import { Language } from '../../../../../entities/shared/language';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { ServicesHighlighted } from '../shared/services-highlighted/services-highlighted';
import { ServicesImages, ServicesImagesIcon } from '../shared/services-images/services-images';
import { ServicesTranslations } from '../shared/services-translations/services-translations';

interface BarServiceCategoryTexts {
  bar: string;
  pizzeria: string;
  restaurant: string;
  fry_shop: string;
  sandwich_shop: string;
  appetizers: string;
  first_courses: string;
  second_courses: string;
  desserts: string;
  fruit: string;
  ice_cream_shop: string;
  drinks: string;
}

const BAR_SERVICE_CATEGORY_KEYS: Readonly<
  Record<BarServiceCategory, keyof BarServiceCategoryTexts>
> = {
  [BarServiceCategory.Bar]: 'bar',
  [BarServiceCategory.Pizzeria]: 'pizzeria',
  [BarServiceCategory.Restaurant]: 'restaurant',
  [BarServiceCategory.FryShop]: 'fry_shop',
  [BarServiceCategory.SandwichShop]: 'sandwich_shop',
  [BarServiceCategory.Appetizers]: 'appetizers',
  [BarServiceCategory.FirstCourses]: 'first_courses',
  [BarServiceCategory.SecondCourses]: 'second_courses',
  [BarServiceCategory.Desserts]: 'desserts',
  [BarServiceCategory.Fruit]: 'fruit',
  [BarServiceCategory.IceCreamShop]: 'ice_cream_shop',
  [BarServiceCategory.Drinks]: 'drinks',
};

@Component({
  selector: 'app-service-general-info-tab',
  imports: [
    FilledSelect,
    FormField,
    ServicesHighlighted,
    ServicesImages,
    ServicesTranslations,
    TranslatePipe,
  ],
  templateUrl: './service-general-info-tab.html',
  styleUrl: './service-general-info-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceGeneralInfoTab {
  readonly form = input.required<FieldTree<BarServiceAddItemRequest>>();
  /** Language of the main name and description: the user's; the others are translations. */
  readonly mainLanguage = input.required<Language>();
  /** Only to look at: the buttons that change the data (icons, image, add, remove) are off. */
  readonly isReadonly = input(false);
  readonly imagePreviewUrl = input<string | null>(null);
  readonly imageError = input<string | null>(null);
  readonly iconChosen = output<BarServiceIcon>();
  readonly imageChosen = output<File>();
  readonly imageRemoveRequest = output<void>();

  private readonly translateService = inject(TranslateService);
  protected readonly validationText = inject(ValidationTextBehaviour);

  /** Every icon a bar service can have, as shown in the grid. */
  protected readonly icons: readonly ServicesImagesIcon<BarServiceIcon>[] = Object.values(
    BarServiceIcon,
  ).map((value) => ({ value, url: `/assets/images/Services_Bar${value}.png` }));
  private readonly categoryTexts = toSignal(
    this.translateService.stream(
      'management.settings.services.bar_services.categories',
    ) as Observable<BarServiceCategoryTexts>,
    { initialValue: null },
  );
  protected readonly categories = computed<readonly SelectOption<BarServiceCategory>[]>(() =>
    Object.values(BarServiceCategory).map((value) => ({
      value,
      label: this.categoryTexts()?.[BAR_SERVICE_CATEGORY_KEYS[value]] ?? '',
    })),
  );
}
