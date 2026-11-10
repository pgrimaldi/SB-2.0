import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { PackageAddItemRequest } from '../../../../../entities/settings/catalog/package-add-item-request';
import { Language } from '../../../../../entities/shared/language';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

@Component({
  selector: 'app-package-general-info-tab',
  imports: [
    Button,
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

  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  /** [before the title, arrow]. */
  protected readonly languagesIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly addIcon = ['/assets/images/GeneralIcon_AddBlue.svg'] as const;
  protected readonly articles = [
    'management.settings.catalog.packages.form.articles.umbrella',
    'management.settings.catalog.packages.form.articles.sunbed',
    'management.settings.catalog.packages.form.articles.deckchair',
    'management.settings.catalog.packages.form.articles.director_chair',
    'management.settings.catalog.packages.form.articles.cabin',
    'management.settings.catalog.packages.form.articles.tent',
  ] as const;

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
}
