import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  Signal,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  MAX_LENGTH,
  TreeValidationResult,
  form,
  metadata,
  readonly,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, firstValueFrom } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import { DropdownList } from '../../../../behaviours/forms/dropdown-list';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { comparableJson } from '../../../../components/shared/forms/text-comparison';
import { DATA_RELOAD } from '../../../../components/shared/ui/data/data-reload';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import {
  apiFieldError,
  apiFieldErrorsOrGeneral,
} from '../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../behaviours/validation/validation-text.behaviour';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import {
  emailAddress,
  maxRichTextLength,
  phoneNumberWithoutPrefix,
  requiredText,
  timeRange,
} from '../../../../components/shared/forms/validators';
import { FilledTextField } from '../../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTimeField } from '../../../../components/shared/ui/inputs/filled-time-field/filled-time-field';
import { RichTextField } from '../../../../components/shared/ui/inputs/rich-text-field/rich-text-field';
import { FilledSelect } from '../../../../components/shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../components/shared/ui/selects/select/select';
import { CancellationPolicy } from '../../../../entities/enums/cancellation-policy';
import { PetPolicy } from '../../../../entities/enums/pet-policy';
import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import {
  BathhouseGeneralInformation,
  MAX_BATHHOUSE_PHONE_LENGTH,
  MAX_BATHHOUSE_POSTAL_CODE_LENGTH,
  MAX_BATHHOUSE_RICH_TEXT_LENGTH,
  MAX_BATHHOUSE_TEXT_LENGTH,
} from '../../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { Country } from '../../../../entities/shared/country';
import { MAX_EMAIL_LENGTH } from '../../../../entities/shared/email-address';
import { VersionConflictPopup } from '../../../../components/management/version-conflict-popup/version-conflict-popup';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';

interface GeneralInformationTexts {
  cancellation_policies: Record<'free_until_48_hours' | 'free_until_24_hours' | 'none', string>;
  pets_options: Record<'allowed' | 'not_allowed' | 'on_request', string>;
}

/** The hours of notice of each choice of Cancellazione: the API has them, not the choice. */
const NOTICE_HOURS: Readonly<Record<CancellationPolicy, number | null>> = {
  [CancellationPolicy.FreeUntil48Hours]: 48,
  [CancellationPolicy.FreeUntil24Hours]: 24,
  [CancellationPolicy.None]: null,
};

@Component({
  selector: 'app-bathhouse-general-settings',
  imports: [
    Button,
    FilledSelect,
    FilledTextField,
    FilledTimeField,
    FormField,
    RichTextField,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './bathhouse-general-settings.html',
  styleUrl: './bathhouse-general-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseGeneralSettings implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly cache = inject(BathhouseConfigurationCacheBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  protected readonly isEditing = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly isSaving = signal(false);
  private readonly information = signal(new BathhouseGeneralInformation());
  private loaded = new BathhouseGeneralInformation();
  /** The version of the information read: Salva sends it back. */
  private version: string | null = null;
  protected readonly informationForm = form(this.information, (path) => {
    requiredText(path.bathhouseName);
    for (const text of [
      path.bathhouseName,
      path.address,
      path.city,
      path.province,
      path.region,
      path.website,
      path.facebook,
      path.instagram,
    ]) {
      metadata(text, MAX_LENGTH, () => MAX_BATHHOUSE_TEXT_LENGTH);
    }
    metadata(path.postalCode, MAX_LENGTH, () => MAX_BATHHOUSE_POSTAL_CODE_LENGTH);
    metadata(path.email, MAX_LENGTH, () => MAX_EMAIL_LENGTH);
    emailAddress(path.email);
    maxRichTextLength(path.description, MAX_BATHHOUSE_RICH_TEXT_LENGTH);
    maxRichTextLength(path.regulation, MAX_BATHHOUSE_RICH_TEXT_LENGTH);
    phoneNumberWithoutPrefix(path.phone, MAX_BATHHOUSE_PHONE_LENGTH);
    phoneNumberWithoutPrefix(path.whatsapp, MAX_BATHHOUSE_PHONE_LENGTH);
    timeRange(path.openingTime, path.closingTime);
    readonly(path, () => !this.isEditing() || this.isSaving());
  });
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.information()) as (keyof BathhouseGeneralInformation)[]).map((name) => [
      name,
      this.validationText.message(this.informationForm[name]),
    ]),
  ) as Readonly<Record<keyof BathhouseGeneralInformation, Signal<string | null>>>;
  protected readonly countries = new DropdownList(
    () => this.cache.countries(),
    this.errorText,
    () => this.chooseFirstCountry(),
  );
  protected readonly countryOptions = computed<readonly SelectOption[]>(() =>
    this.countries
      .items()
      .flatMap(({ code, name }) => (code ? [{ value: code, label: name ?? code }] : [])),
  );
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.bathhouse_configuration.general_information',
    ) as Observable<GeneralInformationTexts>,
    { initialValue: null },
  );
  protected readonly cancellationOptions = computed<readonly SelectOption<CancellationPolicy>[]>(
    () => [
      {
        value: CancellationPolicy.FreeUntil48Hours,
        label: this.texts()?.cancellation_policies.free_until_48_hours ?? '',
      },
      {
        value: CancellationPolicy.FreeUntil24Hours,
        label: this.texts()?.cancellation_policies.free_until_24_hours ?? '',
      },
      {
        value: CancellationPolicy.None,
        label: this.texts()?.cancellation_policies.none ?? '',
      },
    ],
  );
  /** The choice of Cancellazione, from the free cancellation and its hours of notice. */
  protected readonly cancellationPolicy = computed(() => policyOf(this.information()));
  protected readonly petOptions = computed<readonly SelectOption<PetPolicy>[]>(() => [
    { value: PetPolicy.Allowed, label: this.texts()?.pets_options.allowed ?? '' },
    { value: PetPolicy.NotAllowed, label: this.texts()?.pets_options.not_allowed ?? '' },
    { value: PetPolicy.OnRequest, label: this.texts()?.pets_options.on_request ?? '' },
  ]);
  protected readonly isLoading = signal(false);
  /** [bold, italic, underline, bulleted list, numbered list, link]. */
  protected readonly richTextIcons = [
    '/assets/images/GeneralIcon_Bold.svg',
    '/assets/images/GeneralIcon_Italic.svg',
    '/assets/images/GeneralIcon_Underline.svg',
    '/assets/images/GeneralIcon_BulletList.svg',
    '/assets/images/GeneralIcon_OrderedList.svg',
    '/assets/images/GeneralIcon_TextLink.svg',
  ] as const;
  /** [X]. */
  protected readonly popupIcons = ['/assets/images/GeneralIcon_Close.svg'] as const;
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = this.errorText.message(this.error);
  protected readonly isConflictShown = signal(false);
  private reloading?: Subscription;

  constructor() {
    this.unsaved.watch(this);
    effect((onCleanup) => {
      this.dataReload?.();
      if (!this.auth.user()) {
        return;
      }
      const subscription = untracked(() => {
        this.reloading?.unsubscribe();
        const information = this.fetch();
        this.countries.load();
        return information;
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  hasUnsavedChanges(): boolean {
    return comparableJson(this.information()) !== comparableJson(this.loaded);
  }

  /** The choice becomes the free cancellation and its hours of notice, as the API has them. */
  protected setCancellationPolicy(policy: CancellationPolicy | null): void {
    if (policy === null) {
      return;
    }
    const isFreeCancellation = this.informationForm.isFreeCancellation();
    isFreeCancellation.value.set(policy !== CancellationPolicy.None);
    this.informationForm.cancellationNoticeHours().value.set(NOTICE_HOURS[policy]);
    isFreeCancellation.markAsDirty();
    isFreeCancellation.markAsTouched();
  }

  protected edit(): void {
    if (this.isLoaded()) {
      this.isEditing.set(true);
    }
  }

  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancel();
    }
  }

  protected cancel(): void {
    this.information.set(this.loaded);
    this.informationForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  protected save(): void {
    if (this.isSaving() || !this.isLoaded()) {
      return;
    }
    void submit(this.informationForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const sent = this.information();
        const version = await firstValueFrom(
          this.catalog.saveBathhouseConfigurationGeneral(sent, this.version),
        );
        const saved = savedAs(sent);
        this.loaded = saved;
        this.version = version;
        this.information.set(saved);
        this.cache.keepGeneralInformation({ ...saved, version });
        this.informationForm().reset();
        this.isEditing.set(false);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictShown.set(true);
          this.reload();
          return undefined;
        }
        if (problem.code === ApiErrorCode.CatalogCountryNotFound) {
          return apiFieldError(this.informationForm.country, problem.code, problem.args);
        }
        return apiFieldErrorsOrGeneral(problem, this.informationForm, (general) =>
          this.error.set(general),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  /** Countries that arrive after the information: the first one, when none is saved or chosen. */
  private chooseFirstCountry(): void {
    if (this.isLoading() || this.loaded.country !== null || this.information().country !== null) {
      return;
    }
    this.loaded = withFirstCountry(this.loaded, this.countries.items());
    this.information.update((information) => ({ ...information, country: this.loaded.country }));
  }

  private reload(): void {
    this.reloading?.unsubscribe();
    this.cancel();
    this.cache.forgetGeneralInformation();
    this.reloading = this.fetch();
  }

  /** Modifica and Salva stay off until the information is read. */
  private fetch(): Subscription {
    this.isLoading.set(true);
    this.error.set(null);
    this.isLoaded.set(false);
    return this.cache.generalInformation().subscribe({
      next: (information) => {
        this.version = information.version;
        this.loaded = withFirstCountry(
          onlyFieldsOf(BathhouseGeneralInformation, information),
          this.countries.items(),
        );
        this.information.set(this.loaded);
        this.informationForm().reset();
        this.isLoaded.set(true);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(toApiProblem(error));
        this.isLoading.set(false);
      },
    });
  }

  ngOnDestroy(): void {
    this.reloading?.unsubscribe();
    this.countries.destroy();
    this.unsaved.unwatch(this);
  }
}

/** As the backend keeps the information: texts without spaces at the ends, an empty one null. */
function savedAs(information: BathhouseGeneralInformation): BathhouseGeneralInformation {
  return Object.fromEntries(
    Object.entries(information).map(([field, value]) => [
      field,
      typeof value === 'string' ? value.trim() || null : value,
    ]),
  ) as BathhouseGeneralInformation;
}

/** Without a saved country, the first one of the backend's list. */
function withFirstCountry(
  information: BathhouseGeneralInformation,
  countries: readonly Country[],
): BathhouseGeneralInformation {
  return { ...information, country: information.country ?? countries.at(0)?.code ?? null };
}

/** Hours of notice other than the choices, kept by the backend, show no choice. */
function policyOf({
  isFreeCancellation,
  cancellationNoticeHours,
}: BathhouseGeneralInformation): CancellationPolicy | null {
  if (!isFreeCancellation) {
    return CancellationPolicy.None;
  }
  return (
    Object.values(CancellationPolicy).find(
      (policy) =>
        policy !== CancellationPolicy.None && NOTICE_HOURS[policy] === cancellationNoticeHours,
    ) ?? null
  );
}
