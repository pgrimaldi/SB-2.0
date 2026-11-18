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
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, TreeValidationResult, form, readonly, submit } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, firstValueFrom, forkJoin, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../behaviours/validation/validation-text.behaviour';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import {
  phoneNumberWithoutPrefix,
  requiredText,
  strictTimeRange,
} from '../../../../components/shared/forms/validators';
import { FilledTextField } from '../../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTimeField } from '../../../../components/shared/ui/inputs/filled-time-field/filled-time-field';
import { RichTextField } from '../../../../components/shared/ui/inputs/rich-text-field/rich-text-field';
import { FilledSelect } from '../../../../components/shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../components/shared/ui/selects/select/select';
import { CancellationPolicy } from '../../../../entities/enums/cancellation-policy';
import { PetPolicy } from '../../../../entities/enums/pet-policy';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import {
  BathhouseGeneralInformation,
  MAX_BATHHOUSE_PHONE_LENGTH,
} from '../../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { Country } from '../../../../entities/shared/country';
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
  ],
  templateUrl: './bathhouse-general-settings.html',
  styleUrl: './bathhouse-general-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseGeneralSettings implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly cache = inject(BathhouseConfigurationCacheBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  protected readonly isEditing = signal(false);
  protected readonly isSaving = signal(false);
  private readonly information = signal(new BathhouseGeneralInformation());
  private loaded = new BathhouseGeneralInformation();
  protected readonly informationForm = form(this.information, (path) => {
    requiredText(path.bathhouseName);
    phoneNumberWithoutPrefix(path.phone, MAX_BATHHOUSE_PHONE_LENGTH);
    phoneNumberWithoutPrefix(path.whatsapp, MAX_BATHHOUSE_PHONE_LENGTH);
    strictTimeRange(path.openingTime, path.closingTime);
    readonly(path, () => !this.isEditing() || this.isSaving());
  });
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.information()) as (keyof BathhouseGeneralInformation)[]).map((name) => [
      name,
      this.validationText.message(this.informationForm[name]),
    ]),
  ) as Readonly<Record<keyof BathhouseGeneralInformation, Signal<string | null>>>;
  private readonly countries = signal<readonly Country[]>([]);
  protected readonly countryOptions = computed<readonly SelectOption[]>(() =>
    this.countries().flatMap(({ code, name }) =>
      code ? [{ value: code, label: name ?? code }] : [],
    ),
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
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    this.unsaved.watch(this);
    effect((onCleanup) => {
      if (!this.auth.user()) {
        return;
      }
      const subscription = untracked(() => {
        this.isLoading.set(true);
        this.error.set(null);
        return forkJoin({
          information: this.cache.generalInformation(),
          countries: this.cache.countries(),
        }).subscribe({
          next: ({ information, countries }) => {
            this.loaded = {
              ...onlyFieldsOf(BathhouseGeneralInformation, information),
              country: information.country ?? countries.at(0)?.code ?? null,
            };
            this.information.set(this.loaded);
            this.informationForm().reset();
            this.countries.set(countries);
            this.isLoading.set(false);
          },
          error: (error: unknown) => {
            this.error.set(toApiProblem(error));
            this.isLoading.set(false);
          },
        });
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  hasUnsavedChanges(): boolean {
    return this.informationForm().dirty();
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
    this.isEditing.set(true);
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
    if (this.isSaving()) {
      return;
    }
    void submit(this.informationForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const sent = this.information();
        await firstValueFrom(this.catalog.saveBathhouseConfigurationGeneral(sent));
        this.loaded = sent;
        this.cache.keepGeneralInformation(sent);
        this.informationForm().reset();
        this.isEditing.set(false);
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.informationForm, (general) =>
          this.error.set(general),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
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
