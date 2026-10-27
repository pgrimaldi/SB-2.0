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
import { FormField, TreeValidationResult, form, readonly, submit } from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { SeasonPeriodUpdateRequest } from '../../../../../entities/settings/seasons/season-period-update-request';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { requiredText } from '../../../../shared/forms/validators';
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
  /** The period of a sub-section, with the name it has now. */
  readonly period = input<SeasonPeriodUpdateRequest | null>(null);
  readonly updated = output<void>();

  private readonly seasons = inject(SeasonService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly isLoading = signal(false);
  private readonly draft = signal(new SeasonPeriodUpdateRequest());
  protected readonly draftForm = form(this.draft, (path) => {
    requiredText(path.periodName);
    readonly(path, () => this.isLoading());
  });
  protected readonly periodNameError = this.validationText.message(this.draftForm.periodName);
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
    const idProperty = this.auth.user()?.propertyId;
    if (this.isLoading() || !idProperty || !this.canSave()) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.seasons.updatePeriod(idProperty, this.draft()));
        this.isLoading.set(false);
        this.close();
        this.updated.emit();
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.draftForm, (problem) =>
          this.error.set(problem),
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

  private original(): SeasonPeriodUpdateRequest {
    return { ...new SeasonPeriodUpdateRequest(), ...this.period() };
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
