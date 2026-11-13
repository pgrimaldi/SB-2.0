import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import {
  MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY,
  MAX_PACKAGE_ARTICLE_QUANTITY,
  MAX_PACKAGE_PEOPLE_QUANTITY,
  PackageAddItemRequest,
} from '../../../../../entities/settings/catalog/package-add-item-request';
import { Language } from '../../../../../entities/shared/language';
import { MapService } from '../../../../../services/api/catalog/map.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

/** The fields of the form that hold a quantity chosen in a dropdown. */
type QuantityField = {
  [K in keyof PackageAddItemRequest]: PackageAddItemRequest[K] extends number | null ? K : never;
}[keyof PackageAddItemRequest];

const ARTICLES_KEY = 'management.settings.catalog.packages.form.articles';

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

  private readonly auth = inject(AuthBehaviour);
  private readonly sectorService = inject(SectorService);
  private readonly mapService = inject(MapService);
  private readonly errorText = inject(ErrorTextBehaviour);
  protected readonly validationText = inject(ValidationTextBehaviour);

  /** [before the title, arrow]. */
  protected readonly languagesIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly additionalItemOptions = quantityOptions(MAX_PACKAGE_ADDITIONAL_ITEM_QUANTITY);
  protected readonly peopleOptions = quantityOptions(MAX_PACKAGE_PEOPLE_QUANTITY);
  protected readonly articleOptions = quantityOptions(MAX_PACKAGE_ARTICLE_QUANTITY);
  protected readonly articles: readonly { field: QuantityField; label: string }[] = [
    { field: 'umbrellaQuantity', label: `${ARTICLES_KEY}.umbrella` },
    { field: 'sunbedQuantity', label: `${ARTICLES_KEY}.sunbed` },
    { field: 'deckchairQuantity', label: `${ARTICLES_KEY}.deckchair` },
    { field: 'directorChairQuantity', label: `${ARTICLES_KEY}.director_chair` },
    { field: 'cabinQuantity', label: `${ARTICLES_KEY}.cabin` },
    { field: 'tentQuantity', label: `${ARTICLES_KEY}.tent` },
  ];

  private readonly sectors = signal<readonly ComboboxItem[]>([]);
  protected readonly sectorsLoadError = signal<string | null>(null);
  private readonly sectorsSubscription?: Subscription;
  protected readonly sectorOptions = computed<readonly SelectOption[]>(() =>
    this.sectors().map(({ id, value }) => ({ value: id, label: value })),
  );
  private readonly sectorAreas = signal<readonly ComboboxItem[]>([]);
  protected readonly sectorAreasLoadError = signal<string | null>(null);
  private readonly sectorAreasSubscription?: Subscription;
  protected readonly sectorAreaOptions = computed<readonly SelectOption[]>(() =>
    this.sectorAreas().map(({ id, value }) => ({ value: id, label: value })),
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

  /** The sectors and the areas are read every time the tab is shown. */
  constructor() {
    const idProperty = this.auth.user()?.propertyId;
    if (idProperty) {
      this.sectorsSubscription = this.sectorService.sectorCombobox(idProperty).subscribe({
        next: (sectors) => this.sectors.set(sectors),
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.sectors.set([]);
          this.sectorsLoadError.set(this.errorText.instant(problem.code, problem.args));
        },
      });
      this.sectorAreasSubscription = this.mapService.sectorAreaCombobox(idProperty).subscribe({
        next: (sectorAreas) => this.sectorAreas.set(sectorAreas),
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.sectorAreas.set([]);
          this.sectorAreasLoadError.set(this.errorText.instant(problem.code, problem.args));
        },
      });
    }
  }

  /** The name of a language in the main one, with a capital letter (e.g. "Inglese"). */
  protected languageName(language: Language | null): string {
    const name = language ? (this.languageNames().of(language) ?? language) : '';
    return name.charAt(0).toLocaleUpperCase(this.mainLanguage()) + name.slice(1);
  }

  /** The dropdowns work with texts: the form keeps the number. */
  protected optionOf(quantity: number | null): string | null {
    return quantity === null ? null : String(quantity);
  }

  protected setQuantity(field: FieldTree<number | null>, option: string | null): void {
    field().value.set(option === null ? null : Number(option));
  }

  ngOnDestroy(): void {
    this.sectorsSubscription?.unsubscribe();
    this.sectorAreasSubscription?.unsubscribe();
  }
}

/** The quantities from 1 to the last one. */
function quantityOptions(last: number): readonly SelectOption[] {
  return Array.from({ length: last }, (_, index) => ({
    value: String(index + 1),
    label: String(index + 1),
  }));
}
