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
  TreeValidationResult,
  form,
  readonly,
  required,
  submit,
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
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { SeasonPeriodInterval } from '../../../../../entities/settings/seasons/season-period-interval';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { noOverlap } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { rangesErrors, withoutRangesErrors } from '../season-ranges-errors';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';

@Component({
  selector: 'app-season-interval-edit-popup',
  imports: [Button, FilledDateField, FormField, TranslatePipe],
  templateUrl: './season-interval-edit-popup.html',
  styleUrl: './season-interval-edit-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class SeasonIntervalEditPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  /** The period is one of the next season instead of the current one. */
  readonly isNextSeason = input.required<boolean>();
  /** The period of the interval, as the list gave it: it is saved with all its intervals. */
  readonly period = input<SeasonPeriodItem | null>(null);
  /** The date interval of a row of the periods, as the list gave it. */
  readonly interval = input<SeasonPeriodInterval | null>(null);
  /** First and last day of the season of the tab: the days outside cannot be chosen. */
  readonly season = input<SeasonPeriodInterval | null>(null);
  /** The date intervals the season has already, this one too: it cannot overlap the others. */
  readonly seasonIntervals = input<readonly SeasonPeriodInterval[]>([]);
  readonly updated = output<void>();
  readonly failed = output<ApiProblem>();

  private readonly seasons = inject(SeasonService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  /** [calendar, before the date]. */
  protected readonly dateIcon = ['/assets/images/GeneralIcon_CalendarFilled.svg'] as const;
  protected readonly isLoading = signal(false);
  private readonly draft = signal(new SeasonPeriodInterval());
  /** The intervals of the season but this one. */
  protected readonly otherIntervals = computed(() =>
    this.seasonIntervals().filter((range) => range !== this.interval()),
  );
  protected readonly draftForm = form(this.draft, (path) => {
    required(path.startsOn);
    required(path.endsOn);
    noOverlap(path, () => this.otherIntervals());
    readonly(path, () => this.isLoading());
  });
  protected readonly startsOnError = this.validationText.message(this.draftForm.startsOn);
  protected readonly endsOnError = this.validationText.message(this.draftForm.endsOn);
  protected readonly overlapError = this.validationText.message(this.draftForm);
  /** Data fine from Data inizio on, inside the season. */
  protected readonly endsOnMin = computed(() =>
    laterDay(this.season()?.startsOn ?? null, this.draft().startsOn),
  );
  /** Data fine stops the day before the next interval of the season. */
  protected readonly endsOnMax = computed(() =>
    lastFreeDay(this.draft().startsOn, this.otherIntervals(), this.season()?.endsOn ?? null),
  );
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
    const period = this.period();
    const interval = this.interval();
    const key = period?.key;
    if (
      this.isLoading() ||
      !this.auth.user()?.propertyId ||
      !period ||
      key === null ||
      key === undefined ||
      !interval ||
      !period.ranges.includes(interval) ||
      !this.canSave()
    ) {
      return;
    }
    const ranges = period.ranges.map((range) => (range === interval ? this.draft() : range));

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(
          this.seasons.updatePeriod(this.isNextSeason(), key, { ...period, ranges }),
        );
        this.isLoading.set(false);
        this.close();
        this.updated.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.failed.emit(problem);
        const ranges = rangesErrors(problem, this.draftForm);
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
      width: 'min(32rem, 95vw)',
      maxWidth: 'none',
      maxHeight: '95vh',
      panelClass: 'season__interval__edit__popup__panel',
      disableClose: true,
      autoFocus: false,
    };
  }

  private original(): SeasonPeriodInterval {
    return { ...new SeasonPeriodInterval(), ...this.interval() };
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

/** The later of two days at midnight UTC as ISO strings, compared by day; a missing one is no limit. */
function laterDay(first: string | null, second: string | null): string | null {
  if (!first || !second) {
    return first ?? second;
  }
  return first.slice(0, 10) > second.slice(0, 10) ? first : second;
}

/**
 * The day before the first of `periods` that starts after `day`, or `limit` when it comes first:
 * days at midnight UTC as ISO strings, compared by day; without `day`, `limit`.
 */
function lastFreeDay(
  day: string | null,
  periods: readonly SeasonPeriodInterval[],
  limit: string | null,
): string | null {
  if (!day) {
    return limit;
  }
  const next = periods.reduce<string | null>(
    (first, { startsOn }) =>
      startsOn &&
      startsOn.slice(0, 10) > day.slice(0, 10) &&
      (!first || startsOn.slice(0, 10) < first.slice(0, 10))
        ? startsOn
        : first,
    null,
  );
  if (!next) {
    return limit;
  }
  const dayBefore = new Date(Date.parse(next.slice(0, 10)) - 86_400_000).toISOString();
  return limit && limit.slice(0, 10) < dayBefore.slice(0, 10) ? limit : dayBefore;
}
