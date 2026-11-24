import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { DropdownList } from '../../../../../behaviours/forms/dropdown-list';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import {
  MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY,
  MAX_PACKAGE_ARTICLE_QUANTITY,
  MAX_PACKAGE_PEOPLE_QUANTITY,
  PackageAddItemRequest,
} from '../../../../../entities/settings/catalog/package-add-item-request';
import { PackageArticle } from '../../../../../entities/settings/catalog/package-article';
import { PackageItemQuantity } from '../../../../../entities/settings/catalog/package-item-quantity';
import { Language } from '../../../../../entities/shared/language';
import { optionsWithMissing } from '../options-with-missing';
import { orderedItems } from '../package-items';
import { sameTextIgnoringCase } from '../../../../shared/forms/text-comparison';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

/** A row of the articles: an item of the warehouse, or an article the warehouse has not. */
interface ArticleRow {
  labelKey: string;
  labelParams: { number: string };
  /** Only when the warehouse has more items of the same article. */
  itemName: string | null;
  idInventoryItem: string | null;
  isAvailable: boolean;
  quantity: number | null;
  /** First the empty choice, to take the article away. */
  options: readonly SelectOption[];
}

interface PackageGeneralInfoTexts {
  no_limit: string;
  articles: { none: string };
  missing_sector: string;
  missing_area: string;
}

const FORM_KEY = 'management.settings.catalog.packages.form';
const ARTICLES_KEY = `${FORM_KEY}.articles`;
const CUSTOM_ARTICLE = /^CustomArticle(\d)$/;

@Component({
  selector: 'app-package-general-info-tab',
  imports: [
    Checkbox,
    ExpansionPanel,
    FilledMultiSelect,
    FilledSelect,
    FilledTextField,
    FormField,
    FormRow,
    FormRowLabel,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './package-general-info-tab.html',
  styleUrl: './package-general-info-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageGeneralInfoTab {
  readonly form = input.required<FieldTree<PackageAddItemRequest>>();
  /** Language of the main name: the user's; the others are translations. */
  readonly mainLanguage = input.required<Language>();
  /** The lists of the panel, read once per opening. */
  readonly sectors = input.required<DropdownList<ComboboxItem>>();
  readonly sectorAreas = input.required<DropdownList<ComboboxItem>>();
  readonly articles = input<readonly PackageArticle[]>([]);
  /** The panel with the tab is open: the other languages are closed again at every opening. */
  readonly isPanelOpen = input(true);

  protected readonly validationText = inject(ValidationTextBehaviour);

  /** [before the title, arrow]. */
  protected readonly languagesIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly areLanguagesExpanded = linkedSignal({
    source: this.isPanelOpen,
    computation: () => false,
  });
  private readonly texts = toSignal(
    inject(TranslateService).stream(FORM_KEY) as Observable<PackageGeneralInfoTexts>,
    { initialValue: null },
  );
  /** First the empty choice, as in the quantities of the articles, to take the choice away. */
  protected readonly additionalItemOptions = computed(() =>
    quantityOptions(
      MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY,
      this.form().maxAdditionalItemQuantity().value(),
      this.texts()?.no_limit,
    ),
  );
  protected readonly peopleOptions = computed(() =>
    quantityOptions(
      MAX_PACKAGE_PEOPLE_QUANTITY,
      this.form().maxPeopleQuantity().value(),
      this.texts()?.no_limit,
    ),
  );
  private readonly quantities = computed(
    () =>
      new Map(
        this.form()
          .items()
          .value()
          .map(({ idInventoryItem, quantity }) => [idInventoryItem, quantity]),
      ),
  );
  protected readonly articleRows = computed<readonly ArticleRow[]>(() =>
    this.articles().flatMap(({ article, items }): ArticleRow[] => {
      const number = article?.match(CUSTOM_ARTICLE)?.[1] ?? '';
      const name = number ? 'custom_article' : snakeCase(article ?? '');
      const label = { labelKey: `${ARTICLES_KEY}.names.${name}`, labelParams: { number } };
      return items.length
        ? items.map(({ idInventoryItem, itemName, totalQuantity }) => {
            const quantity = this.quantities().get(idInventoryItem) ?? null;
            return {
              ...label,
              itemName: items.length > 1 ? itemName : null,
              idInventoryItem,
              isAvailable: (totalQuantity ?? 0) > 0,
              quantity,
              options: quantityOptions(
                MAX_PACKAGE_ARTICLE_QUANTITY,
                quantity,
                this.texts()?.articles.none,
              ),
            };
          })
        : [
            {
              ...label,
              itemName: null,
              idInventoryItem: null,
              isAvailable: false,
              quantity: null,
              options: [],
            },
          ];
    }),
  );
  protected readonly hasUnavailableArticles = computed(() =>
    this.articleRows().some(({ isAvailable }) => !isAvailable),
  );

  /** Also the chosen ones that are no longer there, for the user to take away. */
  protected readonly sectorOptions = computed<readonly SelectOption[]>(() =>
    optionsWithMissing(
      this.sectors(),
      this.form().idSectors().value(),
      this.texts()?.missing_sector ?? '',
    ),
  );
  /** Also the chosen ones that are no longer there, for the user to take away. */
  protected readonly sectorAreaOptions = computed<readonly SelectOption[]>(() =>
    optionsWithMissing(
      this.sectorAreas(),
      this.form().idSectorAreas().value(),
      this.texts()?.missing_area ?? '',
    ),
  );

  protected readonly mainTranslation = computed(
    () =>
      [...this.form().translations].find(
        (translation) => translation.language().value() === this.mainLanguage(),
      ) ?? null,
  );
  protected readonly otherTranslations = computed(() =>
    [...this.form().translations].filter(
      (translation) => translation.language().value() !== this.mainLanguage(),
    ),
  );
  private readonly languageNames = computed(
    () => new Intl.DisplayNames([this.mainLanguage()], { type: 'language' }),
  );

  /** The name of a language in the main one, with a capital letter (e.g. "Inglese"). */
  protected languageName(language: Language | null): string {
    const name = language ? (this.languageNames().of(language) ?? language) : '';
    return name.charAt(0).toLocaleUpperCase(this.mainLanguage()) + name.slice(1);
  }

  protected articleLabel({ itemName }: ArticleRow, article: string): string {
    return itemName === null || sameTextIgnoringCase(itemName, article)
      ? article
      : `${article}: ${itemName}`;
  }

  /** In the order of the rows; the items with no row stay as they are, after them. */
  protected setArticleQuantity({ idInventoryItem }: ArticleRow, option: string | null): void {
    const items = this.form().items();
    items.value.set(
      orderedItems(
        [
          ...items.value().filter((item) => item.idInventoryItem !== idInventoryItem),
          ...(option
            ? [
                Object.assign(new PackageItemQuantity(), {
                  idInventoryItem,
                  quantity: Number(option),
                }),
              ]
            : []),
        ],
        this.articles(),
      ),
    );
    items.markAsTouched();
  }

  /** The dropdowns work with texts: the form keeps the number. */
  protected optionOf(quantity: number | null): string | null {
    return quantity === null ? null : String(quantity);
  }

  protected setQuantity(field: FieldTree<number | null>, option: string | null): void {
    field().value.set(option ? Number(option) : null);
  }
}

/** The empty choice, the quantities from 1 to the last one, and the one saved when it is not one of them. */
function quantityOptions(
  last: number,
  saved: number | null,
  emptyText: string | undefined,
): readonly SelectOption[] {
  const quantities = Array.from({ length: last }, (_, index) => index + 1);
  return [
    { value: '', label: emptyText ?? '' },
    ...[...quantities, ...(saved !== null && !quantities.includes(saved) ? [saved] : [])].map(
      (quantity) => ({ value: String(quantity), label: String(quantity) }),
    ),
  ];
}

function snakeCase(name: string): string {
  return name.replace(/(?!^)([A-Z])/g, '_$1').toLowerCase();
}
