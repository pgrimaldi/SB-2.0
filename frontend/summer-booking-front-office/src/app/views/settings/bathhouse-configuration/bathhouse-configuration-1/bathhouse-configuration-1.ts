import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, form, readonly } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, forkJoin, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { FilledTextField } from '../../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTimeField } from '../../../../components/shared/ui/inputs/filled-time-field/filled-time-field';
import { RichTextField } from '../../../../components/shared/ui/inputs/rich-text-field/rich-text-field';
import { FilledSelect } from '../../../../components/shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../components/shared/ui/selects/select/select';
import { ComboboxItem } from '../../../../entities/combobox/combobox-item';
import { CancellationPolicy } from '../../../../entities/enums/cancellation-policy';
import { PetPolicy } from '../../../../entities/enums/pet-policy';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { BathhouseGeneralInformation } from '../../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';

interface GeneralInformationTexts {
  cancellation_policies: Record<'free_until_48_hours' | 'free_until_24_hours', string>;
  pets_options: Record<'allowed' | 'not_allowed', string>;
}

@Component({
  selector: 'app-bathhouse-configuration-1',
  imports: [
    Button,
    FilledSelect,
    FilledTextField,
    FilledTimeField,
    FormField,
    RichTextField,
    TranslatePipe,
  ],
  templateUrl: './bathhouse-configuration-1.html',
  styleUrl: './bathhouse-configuration-1.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseConfiguration1 implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly cache = inject(BathhouseConfigurationCacheBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly translateService = inject(TranslateService);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  protected readonly isEditing = signal(false);
  private readonly information = signal(new BathhouseGeneralInformation());
  private loaded = new BathhouseGeneralInformation();
  protected readonly informationForm = form(this.information, (path) => {
    readonly(path, () => !this.isEditing());
  });
  private readonly countries = signal<readonly ComboboxItem[]>([]);
  protected readonly countryOptions = computed<readonly SelectOption[]>(() =>
    this.countries().map(({ id, value }) => ({ value: id, label: value })),
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
    ],
  );
  protected readonly petOptions = computed<readonly SelectOption<PetPolicy>[]>(() => [
    { value: PetPolicy.Allowed, label: this.texts()?.pets_options.allowed ?? '' },
    { value: PetPolicy.NotAllowed, label: this.texts()?.pets_options.not_allowed ?? '' },
  ]);
  protected readonly isLoading = signal(false);
  /** [bold, italic, underline, bulleted list, numbered list, link]. */
  protected readonly richTextIcons = [
    '/assets/images/BathhouseConfiguration_Bold.svg',
    '/assets/images/BathhouseConfiguration_Italic.svg',
    '/assets/images/BathhouseConfiguration_Underline.svg',
    '/assets/images/BathhouseConfiguration_BulletList.svg',
    '/assets/images/BathhouseConfiguration_OrderedList.svg',
    '/assets/images/BathhouseConfiguration_Link.svg',
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
      const idProperty = this.auth.user()?.propertyId;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() => {
        this.isLoading.set(true);
        this.error.set(null);
        return forkJoin({
          information: this.cache.generalInformation(idProperty),
          countries: this.cache.countries(),
        }).subscribe({
          next: ({ information, countries }) => {
            this.loaded = {
              ...onlyFieldsOf(BathhouseGeneralInformation, information),
              country: information.country ?? countries.at(0)?.id ?? null,
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

  protected edit(): void {
    this.isEditing.set(true);
  }

  protected cancel(): void {
    this.information.set(this.loaded);
    this.informationForm().reset();
    this.isEditing.set(false);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}
