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
import { FormField, form } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import { FilledTextField } from '../../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledSelect } from '../../../../components/shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../components/shared/ui/selects/select/select';
import { ComboboxItem } from '../../../../entities/combobox/combobox-item';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { BathhouseGeneralInformation } from '../../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';

@Component({
  selector: 'app-bathhouse-configuration-1',
  imports: [FilledSelect, FilledTextField, FormField, TranslatePipe],
  templateUrl: './bathhouse-configuration-1.html',
  styleUrl: './bathhouse-configuration-1.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseConfiguration1 implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly cache = inject(BathhouseConfigurationCacheBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);

  private readonly information = signal(new BathhouseGeneralInformation());
  protected readonly informationForm = form(this.information);
  private readonly countries = signal<readonly ComboboxItem[]>([]);
  protected readonly countryOptions = computed<readonly SelectOption[]>(() =>
    this.countries().map(({ id, value }) => ({ value: id, label: value })),
  );
  protected readonly isLoading = signal(false);
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
            this.information.set({
              ...onlyFieldsOf(BathhouseGeneralInformation, information),
              country: information.country ?? countries.at(0)?.id ?? null,
            });
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

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}
