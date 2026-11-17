import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  MAX_LENGTH,
  TreeValidationResult,
  form,
  metadata,
  readonly,
  submit,
  validate,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import {
  ApiValidationError,
  apiFieldErrorsOrGeneral,
} from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  MAX_SEASON_PERIOD_NAME_LENGTH,
  SeasonPeriodItem,
} from '../../../../../entities/settings/seasons/season-period-item';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { isTextTaken, requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';

@Component({
  selector: 'app-season-period-edit-popup',
  imports: [Button, FilledTextField, FormField, FormRow, TranslatePipe],
  templateUrl: './season-period-edit-popup.html',
  styleUrl: './season-period-edit-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class SeasonPeriodEditPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  /** The period is one of the next season instead of the current one. */
  readonly isNextSeason = input.required<boolean>();
  /** The period of a sub-section, with the name and the date intervals it has now. */
  readonly period = input<SeasonPeriodItem | null>(null);
  /** The periods of the season, this one too: two periods of a season cannot have the same name. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);
  readonly updated = output<void>();

  private readonly seasons = inject(SeasonService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly isLoading = signal(false);
  private readonly draft = signal(new SeasonPeriodItem());
  protected readonly draftForm = form(this.draft, (path) => {
    requiredText(path.name);
    metadata(path.name, MAX_LENGTH, () => MAX_SEASON_PERIOD_NAME_LENGTH);
    validate(path.name, ({ value }) =>
      isTextTaken(value(), this.otherNames()) ? { kind: 'seasons.period_name.taken' } : undefined,
    );
    readonly(path, () => this.isLoading());
  });
  protected readonly nameError = this.validationText.message(this.draftForm.name);
  protected readonly canSave = computed(() => this.draftForm().valid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    super();
    this.unsaved.watch(this);
    effect(() => {
      this.open();
      untracked(() => this.reset());
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && JSON.stringify(this.draft()) !== JSON.stringify(this.original());
  }

  protected save(): void {
    const key = this.draft().key;
    if (this.isLoading() || !this.auth.user()?.propertyId || !this.canSave() || key === null) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.seasons.updatePeriod(this.isNextSeason(), key, this.draft()));
        this.isLoading.set(false);
        this.close();
        this.updated.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (problem.code === 'pricing.period_name_taken') {
          const nameTaken: ApiValidationError = {
            kind: 'api',
            fieldTree: this.draftForm.name,
            code: problem.code,
          };
          return nameTaken;
        }
        if ((problem.errors ?? []).some(({ field }) => field !== 'name')) {
          this.error.set(problem);
          return [];
        }
        return apiFieldErrorsOrGeneral(problem, this.draftForm, (general) =>
          this.error.set(general),
        );
      } finally {
        this.isLoading.set(false);
      }
    });
  }

  protected async leave(): Promise<void> {
    if (!this.isLoading() && (await this.unsaved.confirmDiscard(this))) {
      this.close();
    }
  }

  protected override isCloseDisabled(): boolean {
    return this.isLoading();
  }

  protected override closeRequested(): void {
    void this.leave();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(32rem, 95vw)',
      maxWidth: 'none',
      maxHeight: '95vh',
      panelClass: 'season__period__edit__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  /** The names of the other periods of the season. */
  private otherNames(): (string | null)[] {
    const key = this.period()?.key;
    return this.periods()
      .filter((period) => period.key !== key)
      .map(({ name }) => name);
  }

  private original(): SeasonPeriodItem {
    return { ...new SeasonPeriodItem(), ...this.period() };
  }

  private reset(): void {
    this.draft.set(this.original());
    this.draftForm().reset();
    this.error.set(null);
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
