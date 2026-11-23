import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EMPTY, Observable } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { DropdownList } from '../../../../../behaviours/forms/dropdown-list';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import {
  MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY,
  MAX_PACKAGE_ARTICLE_QUANTITY,
  MAX_PACKAGE_PEOPLE_QUANTITY,
  PackageAddItemRequest,
} from '../../../../../entities/settings/catalog/package-add-item-request';
import { PackageArticle } from '../../../../../entities/settings/catalog/package-article';
import { PackageItemQuantity } from '../../../../../entities/settings/catalog/package-item-quantity';
import { Language } from '../../../../../entities/shared/language';
import { orderedItems } from '../package-items';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
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
}

const ARTICLES_KEY = 'management.settings.catalog.packages.form.articles';
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
export class PackageGeneralInfoTab implements OnDestroy {
  readonly form = input.required<FieldTree<PackageAddItemRequest>>();
  /** Language of the main name: the user's; the others are translations. */
  readonly mainLanguage = input.required<Language>();
  /** When it changes the sectors and the areas are read again, e.g. because one is no longer there. */
  readonly sectorsVersion = input(0);
  /** The articles of the backend, each with its items in the warehouse. */
  readonly articles = input<readonly PackageArticle[]>([]);

  private readonly auth = inject(AuthBehaviour);
  private readonly sectorService = inject(SectorService);
  private readonly errorText = inject(ErrorTextBehaviour);
  protected readonly validationText = inject(ValidationTextBehaviour);

  /** [before the title, arrow]. */
  protected readonly languagesIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly additionalItemOptions = computed(() =>
    quantityOptions(
      MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY,
      this.form().maxAdditionalItemQuantity().value(),
    ),
  );
  protected readonly peopleOptions = computed(() =>
    quantityOptions(MAX_PACKAGE_PEOPLE_QUANTITY, this.form().maxPeopleQuantity().value()),
  );
  private readonly noneText = toSignal(
    inject(TranslateService).stream(`${ARTICLES_KEY}.none`) as Observable<string>,
    { initialValue: '' },
  );
  protected readonly articleRows = computed<readonly ArticleRow[]>(() =>
    this.articles().flatMap(({ article, items }): ArticleRow[] => {
      const number = article?.match(CUSTOM_ARTICLE)?.[1] ?? '';
      const name = number ? 'custom_article' : snakeCase(article ?? '');
      const label = { labelKey: `${ARTICLES_KEY}.names.${name}`, labelParams: { number } };
      return items.length
        ? items.map(({ idInventoryItem, itemName, totalQuantity }) => ({
            ...label,
            itemName: items.length > 1 ? itemName : null,
            idInventoryItem,
            isAvailable: (totalQuantity ?? 0) > 0,
          }))
        : [{ ...label, itemName: null, idInventoryItem: null, isAvailable: false }];
    }),
  );
  protected readonly hasUnavailableArticles = computed(() =>
    this.articleRows().some(({ isAvailable }) => !isAvailable),
  );

  protected readonly sectors = new DropdownList(
    () => (this.auth.user()?.propertyId ? this.sectorService.sectorCombobox() : EMPTY),
    this.errorText,
    () => this.areSectorsRead.set(true),
  );
  private readonly areSectorsRead = signal(false);
  private readonly missingSectorText = toSignal(
    inject(TranslateService).stream(
      'management.settings.catalog.packages.form.missing_sector',
    ) as Observable<string>,
    { initialValue: '' },
  );
  /** Also the chosen ones that are no longer there, for the user to take away. */
  protected readonly sectorOptions = computed<readonly SelectOption[]>(() => {
    const sectors = this.sectors.items();
    const missing =
      this.areSectorsRead() && !this.sectors.loadError()
        ? this.form()
            .idSectors()
            .value()
            .filter((idSector) => !sectors.some(({ id }) => id === idSector))
        : [];
    return [
      ...sectors.map(({ id, value }) => ({ value: id, label: value })),
      ...missing.map((idSector) => ({ value: idSector, label: this.missingSectorText() })),
    ];
  });
  protected readonly sectorAreas = new DropdownList(
    () => (this.auth.user()?.propertyId ? this.sectorService.sectorAreaCombobox() : EMPTY),
    this.errorText,
    () => this.areAreasRead.set(true),
  );
  private readonly areAreasRead = signal(false);
  private readonly missingAreaText = toSignal(
    inject(TranslateService).stream(
      'management.settings.catalog.packages.form.missing_area',
    ) as Observable<string>,
    { initialValue: '' },
  );
  /** Also the chosen ones that are no longer there, for the user to take away. */
  protected readonly sectorAreaOptions = computed<readonly SelectOption[]>(() => {
    const areas = this.sectorAreas.items();
    const missing =
      this.areAreasRead() && !this.sectorAreas.loadError()
        ? this.form()
            .idSectorAreas()
            .value()
            .filter((idSectorArea) => !areas.some(({ id }) => id === idSectorArea))
        : [];
    return [
      ...areas.map(({ id, value }) => ({ value: id, label: value })),
      ...missing.map((idSectorArea) => ({ value: idSectorArea, label: this.missingAreaText() })),
    ];
  });

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

  /** The sectors and the areas are read every time the tab is shown. */
  constructor() {
    effect(() => {
      this.sectorsVersion();
      untracked(() => {
        this.sectors.load();
        this.sectorAreas.load();
      });
    });
  }

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

  protected articleQuantity({ idInventoryItem }: ArticleRow): number | null {
    return (
      this.form()
        .items()
        .value()
        .find((item) => item.idInventoryItem === idInventoryItem)?.quantity ?? null
    );
  }

  /** First the empty choice, to take the article away. */
  protected articleOptions(row: ArticleRow): readonly SelectOption[] {
    return [
      { value: '', label: this.noneText() },
      ...quantityOptions(MAX_PACKAGE_ARTICLE_QUANTITY, this.articleQuantity(row)),
    ];
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
    field().value.set(option === null ? null : Number(option));
  }

  ngOnDestroy(): void {
    this.sectors.destroy();
    this.sectorAreas.destroy();
  }
}

/** The quantities from 1 to the last one, and the one saved when it is not one of them. */
function quantityOptions(last: number, saved: number | null): readonly SelectOption[] {
  const quantities = Array.from({ length: last }, (_, index) => index + 1);
  return [...quantities, ...(saved !== null && !quantities.includes(saved) ? [saved] : [])].map(
    (quantity) => ({ value: String(quantity), label: String(quantity) }),
  );
}

function snakeCase(name: string): string {
  return name.replace(/(?!^)([A-Z])/g, '_$1').toLowerCase();
}
