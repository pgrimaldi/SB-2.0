import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ValidationTextBehaviour } from '../../../../../../behaviours/validation/validation-text.behaviour';
import { ServiceTranslation } from '../../../../../../entities/settings/services/service-translation';
import { Language } from '../../../../../../entities/shared/language';
import { ExpansionPanel } from '../../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { FormRow } from '../../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../../shared/ui/forms/form-row/form-row-label';
import { FilledTextField } from '../../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTextarea } from '../../../../../shared/ui/inputs/filled-textarea/filled-textarea';

/** Order of the languages in the form. */
export const SERVICE_LANGUAGES: readonly Language[] = [
  Language.It,
  Language.En,
  Language.Fr,
  Language.De,
  Language.Es,
  Language.Ar,
  Language.Zh,
];

/** Name and description of a service: in the user's language first, the others under "Altre lingue". */
@Component({
  selector: 'app-services-translations',
  imports: [
    ExpansionPanel,
    FilledTextField,
    FilledTextarea,
    FormField,
    FormRow,
    FormRowLabel,
    TranslatePipe,
  ],
  templateUrl: './services-translations.html',
  styleUrl: './services-translations.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesTranslations {
  /** The translations of the form of the panel, which validates and saves them. */
  readonly form = input.required<FieldTree<ServiceTranslation[]>>();
  /** Language of the main name and description: the user's; the others are translations. */
  readonly mainLanguage = input.required<Language>();

  protected readonly validationText = inject(ValidationTextBehaviour);

  /** [before the title, arrow]. */
  protected readonly languagesIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;

  protected readonly mainTranslation = computed(
    () =>
      [...this.form()].find(
        (translation) => translation.language().value() === this.mainLanguage(),
      ) ?? null,
  );
  protected readonly otherTranslations = computed(() =>
    [...this.form()].filter(
      (translation) => translation.language().value() !== this.mainLanguage(),
    ),
  );
  protected readonly areNamesOpen = signal(false);
  protected readonly areDescriptionsOpen = signal(false);
  /** The error of a name, or of a description, in a closed panel: it shows under the panel. */
  protected readonly closedNameError = computed(() =>
    this.areNamesOpen() ? null : this.firstError(({ serviceName }) => serviceName),
  );
  protected readonly closedDescriptionError = computed(() =>
    this.areDescriptionsOpen() ? null : this.firstError(({ description }) => description),
  );
  private readonly languageNames = computed(
    () => new Intl.DisplayNames([this.mainLanguage()], { type: 'language' }),
  );

  /** The name of a language in the main one, with a capital letter (e.g. "Inglese"). */
  protected languageName(language: Language | null): string {
    const name = language ? (this.languageNames().of(language) ?? language) : '';
    return name.charAt(0).toLocaleUpperCase(this.mainLanguage()) + name.slice(1);
  }

  /** "Inglese: …" for the first language with an error in that field. */
  private firstError(
    fieldOf: (translation: FieldTree<ServiceTranslation>) => FieldTree<string | null>,
  ): string | null {
    for (const translation of this.otherTranslations()) {
      const text = this.validationText.text(fieldOf(translation));
      if (text) {
        return `${this.languageName(translation.language().value())}: ${text}`;
      }
    }
    return null;
  }
}
