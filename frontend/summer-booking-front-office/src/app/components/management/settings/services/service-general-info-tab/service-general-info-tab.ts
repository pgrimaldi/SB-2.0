import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { BarServiceCategory } from '../../../../../entities/enums/bar-service-category';
import { BarServiceIcon } from '../../../../../entities/enums/bar-service-icon';
import { BarServiceAddItemRequest } from '../../../../../entities/settings/services/bar-service-add-item-request';
import { Language } from '../../../../../entities/shared/language';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { InfoPopup } from '../../../../shared/ui/dialogs/info-popup/info-popup';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTextarea } from '../../../../shared/ui/inputs/filled-textarea/filled-textarea';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { FileDropZone } from '../../../../shared/ui/uploads/file-drop-zone/file-drop-zone';

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

interface BarServiceCategoryTexts {
  cafeteria: string;
  drinks: string;
  snacks_and_sandwiches: string;
  ice_creams: string;
  desserts: string;
}

const BAR_SERVICE_CATEGORY_KEYS: Readonly<
  Record<BarServiceCategory, keyof BarServiceCategoryTexts>
> = {
  [BarServiceCategory.Cafeteria]: 'cafeteria',
  [BarServiceCategory.Drinks]: 'drinks',
  [BarServiceCategory.SnacksAndSandwiches]: 'snacks_and_sandwiches',
  [BarServiceCategory.IceCreams]: 'ice_creams',
  [BarServiceCategory.Desserts]: 'desserts',
};

@Component({
  selector: 'app-service-general-info-tab',
  imports: [
    ExpansionPanel,
    FileDropZone,
    FilledSelect,
    FilledTextField,
    FilledTextarea,
    FormField,
    FormRow,
    FormRowLabel,
    IconButton,
    InfoPopup,
    SectionTitle,
    Toggle,
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
  readonly imagePreviewUrl = input<string | null>(null);
  readonly iconChosen = output<BarServiceIcon>();
  readonly imageChosen = output<File>();

  private readonly translateService = inject(TranslateService);
  protected readonly validationText = inject(ValidationTextBehaviour);

  /** Every icon a bar service can have, as shown in the grid. */
  protected readonly icons = Object.values(BarServiceIcon).map((icon) => ({
    icon,
    url: `/assets/images/Services_Bar${icon}.png`,
  }));
  protected readonly imagesIcon = ['/assets/images/GeneralIcon_Images.svg'] as const;
  protected readonly addImageIcon = ['/assets/images/GeneralIcon_AddImage.svg'] as const;
  /** [before the title, arrow]. */
  protected readonly languagesIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [X, before the title]. */
  protected readonly infoPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_InfoTitle.svg',
  ] as const;
  protected readonly showingHighlightedInfo = signal(false);
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

  protected chooseImage(files: readonly File[]): void {
    const [image] = files;
    if (image) {
      this.imageChosen.emit(image);
    }
  }
}
