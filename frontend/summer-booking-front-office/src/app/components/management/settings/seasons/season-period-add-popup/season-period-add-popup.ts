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
  apiFieldError,
  apiFieldErrorsOrGeneral,
} from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { SeasonPeriodAddRequest } from '../../../../../entities/settings/seasons/season-period-add-request';
import { SeasonPeriodInterval } from '../../../../../entities/settings/seasons/season-period-interval';
import {
  MAX_SEASON_PERIOD_NAME_LENGTH,
  MAX_SEASON_PERIOD_RANGES,
  SeasonPeriodItem,
} from '../../../../../entities/settings/seasons/season-period-item';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import {
  isTextTaken,
  minItems,
  noOverlaps,
  requiredText,
} from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { PeriodCalendar } from '../../../../shared/ui/datepickers/period-calendar/period-calendar';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { periodOverlapError, rangesErrors, withoutRangesErrors } from '../season-ranges-errors';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { comparableJson } from '../../../../shared/forms/text-comparison';

@Component({
  selector: 'app-season-period-add-popup',
  imports: [Button, FilledTextField, FormField, FormRow, PeriodCalendar, TranslatePipe],
  templateUrl: './season-period-add-popup.html',
  styleUrl: './season-period-add-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class SeasonPeriodAddPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  /** The period goes to the next season instead of the current one. */
  readonly isNextSeason = input.required<boolean>();
  /** First and last day of the season of the tab: the days outside cannot be chosen. */
  readonly season = input<SeasonPeriodInterval | null>(null);
  /** The date intervals the season has already: the new ones cannot overlap them. */
  readonly seasonIntervals = input<readonly SeasonPeriodInterval[]>([]);
  /** The periods the season has already: the new one cannot have the name of one of them. */
  readonly periods = input<readonly SeasonPeriodItem[]>([]);
  readonly added = output<void>();
  /** Every error of the save, e.g. for the tab to read its periods again. */
  readonly failed = output<ApiProblem>();

  private readonly seasons = inject(SeasonService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  /** [previous month, next month, remove an interval]. */
  protected readonly calendarIcons = [
    '/assets/images/GeneralIcon_Previous.svg',
    '/assets/images/GeneralIcon_Next.svg',
    '/assets/images/GeneralIcon_Close.svg',
  ] as const;
  protected readonly isLoading = signal(false);
  private readonly draft = signal(new SeasonPeriodAddRequest());
  protected readonly draftForm = form(this.draft, (path) => {
    requiredText(path.name);
    metadata(path.name, MAX_LENGTH, () => MAX_SEASON_PERIOD_NAME_LENGTH);
    validate(path.name, ({ value }) =>
      isTextTaken(
        value(),
        this.periods().map(({ name }) => name),
      )
        ? { kind: 'seasons.period_name.taken' }
        : undefined,
    );
    minItems(path.ranges, 1);
    validate(path.ranges, ({ value }) =>
      value().length > MAX_SEASON_PERIOD_RANGES
        ? { kind: 'seasons.ranges.max', max: MAX_SEASON_PERIOD_RANGES }
        : undefined,
    );
    noOverlaps(path.ranges, () => this.seasonIntervals());
    readonly(path, () => this.isLoading());
  });
  protected readonly nameError = this.validationText.message(this.draftForm.name);
  protected readonly intervalsError = this.validationText.message(
    this.draftForm.ranges,
    'management.settings.seasons.add_popup.field_names.interval',
  );
  /** The days of the other periods of the season and of the intervals chosen already. */
  protected readonly takenIntervals = computed(() => [
    ...this.seasonIntervals(),
    ...this.draft().ranges,
  ]);
  /** The backend creates a period only in a season with both its days. */
  protected readonly isSeasonMissing = computed(() => !this.season());
  protected readonly canSave = computed(() => this.draftForm().valid() && !this.isSeasonMissing());
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
    return (
      this.open() && comparableJson(this.draft()) !== comparableJson(new SeasonPeriodAddRequest())
    );
  }

  protected save(): void {
    if (this.isLoading() || !this.auth.user()?.propertyId || !this.canSave()) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.seasons.addPeriod(this.isNextSeason(), this.draft()));
        this.isLoading.set(false);
        this.close();
        this.added.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.failed.emit(problem);
        if (problem.code === ApiErrorCode.PricingPeriodNameTaken) {
          return apiFieldError(this.draftForm.name, problem.code);
        }
        if (problem.code === ApiErrorCode.PricingPeriodOutsideSeason) {
          return apiFieldError(this.draftForm.ranges, problem.code);
        }
        const overlap = periodOverlapError(problem, this.draftForm.ranges);
        if (overlap) {
          return overlap;
        }
        const ranges = rangesErrors(problem, this.draftForm.ranges);
        if (!ranges.length) {
          return apiFieldErrorsOrGeneral(problem, this.draftForm, (general) =>
            this.error.set(general),
          );
        }
        const others = withoutRangesErrors(problem);
        return others.errors?.length
          ? [
              ...ranges,
              ...apiFieldErrorsOrGeneral(others, this.draftForm, (general) =>
                this.error.set(general),
              ),
            ]
          : ranges;
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
      width: 'min(52.5rem, 95vw)',
      maxWidth: 'none',
      maxHeight: '95vh',
      panelClass: 'season__period__add__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.draft.set(new SeasonPeriodAddRequest());
    this.draftForm().reset();
    this.error.set(null);
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
